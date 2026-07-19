using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MiniCore.Core;
using MiniCore.Model;
using Newtonsoft.Json;

namespace MiniCore.Service
{
    /// <summary>
    /// 加密存档服务的启动参数。
    /// </summary>
    public sealed class EncryptedSaveServiceInitArgs : ComponentInitArgs
    {
        #region Public 公共成员

        /// <summary>
        /// 获取或设置由开发者填写的稳定加密口令。
        /// 修改该值后，使用旧值写入的存档将无法读取；该值会明文保存在启动配置和生成代码中。
        /// </summary>
        public string EncryptionKey { get; set; } = string.Empty;

        #endregion
    }

    /// <summary>
    /// 使用 AES-CBC 与 HMAC-SHA256 保存版本化 JSON 数据的应用服务。
    /// 开发者填写的启动口令经 SHA-256 派生主密钥，再按槽位派生实际密钥。
    /// </summary>
    [AppService("加密存档", typeof(ISaveService), Description = "使用开发者配置的密钥加密并校验版本化本地存档。", RequiresServices = new[] { typeof(IStoragePathService) }, InitArgsType = typeof(EncryptedSaveServiceInitArgs))]
    public sealed class EncryptedSaveService : AAppService, ISaveService
    {
        #region Private 私有成员

        private const int CurrentFormatVersion = 1; // 当前加密文件格式版本。
        private byte[] masterKey; // 由开发者启动口令派生的 32 字节主密钥。
        private IStoragePathService storagePathService; // 本地持久化路径服务。
        private ITelemetryService telemetry; // 可选遥测服务。
        private string rootPath; // 所有逻辑存档槽位的根目录。

        #endregion

        #region Override 重写实现

        /// <summary>
        /// 该服务必须通过包含加密口令的启动参数初始化。
        /// </summary>
        public override void Awake()
        {
            Initialize(string.Empty);
        }

        /// <summary>
        /// 使用开发者配置的加密口令初始化存档服务。
        /// </summary>
        /// <param name="args">加密存档服务启动参数。</param>
        public override void Awake(ComponentInitArgs args)
        {
            if (!(args is EncryptedSaveServiceInitArgs saveArgs))
            {
                throw new ArgumentException("加密存档服务必须使用 EncryptedSaveServiceInitArgs 初始化。", nameof(args));
            }

            Initialize(saveArgs.EncryptionKey);
        }

        /// <summary>
        /// 释放当前服务持有的引用并清除内存中的主密钥。
        /// </summary>
        public override void Dispose()
        {
            Global.ReleaseAll(this);
            if (masterKey != null)
            {
                Array.Clear(masterKey, 0, masterKey.Length);
                masterKey = null;
            }

            storagePathService = null;
            telemetry = null;
            base.Dispose();
        }

        #endregion

        #region Interface 接口实现

        /// <summary>
        /// 将对象序列化、加密并以原子替换方式写入指定逻辑槽位。
        /// </summary>
        /// <typeparam name="T">待保存对象类型。</typeparam>
        /// <param name="slotName">逻辑存档槽位名称。</param>
        /// <param name="data">待保存对象。</param>
        /// <param name="token">取消令牌。</param>
        /// <returns>保存完成任务。</returns>
        public async Task SaveAsync<T>(string slotName, T data, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            string path = GetSlotPath(slotName);
            byte[] payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data));
            byte[] key = GetKey(slotName);
            byte[] encrypted = Encrypt(payload, key);
            string temporaryPath = path + ".tmp";

            await Task.Run(() =>
            {
                token.ThrowIfCancellationRequested();
                File.WriteAllBytes(temporaryPath, encrypted);
            }, token);
            token.ThrowIfCancellationRequested();

            ReplaceAtomically(temporaryPath, path);
            telemetry?.Increment("save.write");
        }

        /// <summary>
        /// 读取、校验、解密并反序列化指定逻辑槽位。
        /// </summary>
        /// <typeparam name="T">目标对象类型。</typeparam>
        /// <param name="slotName">逻辑存档槽位名称。</param>
        /// <param name="token">取消令牌。</param>
        /// <returns>槽位不存在时返回空；否则返回存档对象。</returns>
        public async Task<T> LoadAsync<T>(string slotName, CancellationToken token = default) where T : class
        {
            string path = GetSlotPath(slotName);
            if (!File.Exists(path))
            {
                return null;
            }

            byte[] encrypted = await Task.Run(() =>
            {
                token.ThrowIfCancellationRequested();
                return File.ReadAllBytes(path);
            }, token);
            byte[] payload = Decrypt(encrypted, GetKey(slotName));
            telemetry?.Increment("save.read");
            return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(payload));
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 验证槽位名称并生成其实际文件路径。
        /// </summary>
        /// <param name="slotName">逻辑槽位名称。</param>
        /// <returns>安全的实际文件路径。</returns>
        private string GetSlotPath(string slotName)
        {
            if (string.IsNullOrWhiteSpace(slotName) || slotName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || slotName.Contains(".."))
            {
                throw new ArgumentException("存档槽位名称无效。", nameof(slotName));
            }

            return Path.Combine(rootPath, slotName + ".mcs");
        }

        /// <summary>
        /// 为逻辑槽位派生 AES 与 HMAC 使用的 32 字节密钥。
        /// </summary>
        /// <param name="slotName">逻辑槽位名称。</param>
        /// <returns>长度为 32 的密钥。</returns>
        private byte[] GetKey(string slotName)
        {
            if (masterKey == null || masterKey.Length != 32)
            {
                throw new InvalidOperationException("加密存档主密钥尚未初始化。");
            }

            byte[] slotBytes = Encoding.UTF8.GetBytes(slotName);
            using HMACSHA256 hmac = new HMACSHA256(masterKey);
            return hmac.ComputeHash(slotBytes);
        }

        /// <summary>
        /// 验证开发者口令、派生主密钥并取得存档目录。
        /// </summary>
        /// <param name="encryptionKey">启动配置中填写的稳定加密口令。</param>
        private void Initialize(string encryptionKey)
        {
            if (string.IsNullOrWhiteSpace(encryptionKey))
            {
                throw new InvalidOperationException("启用加密存档前，必须在启动配置的 EncryptionKey 中填写稳定的加密口令。");
            }

            byte[] source = Encoding.UTF8.GetBytes(encryptionKey);
            using SHA256 sha256 = SHA256.Create();
            masterKey = sha256.ComputeHash(source);
            storagePathService = Global.GetService<IStoragePathService>(this);
            Global.TryGetService(this, out telemetry);
            rootPath = storagePathService.GetDirectory("Saves");
        }

        /// <summary>
        /// 加密正文并在末尾附加 HMAC 完整性标签。
        /// </summary>
        /// <param name="payload">明文数据。</param>
        /// <param name="key">32 字节主密钥。</param>
        /// <returns>版本、IV、密文与认证标签组成的文件内容。</returns>
        private static byte[] Encrypt(byte[] payload, byte[] key)
        {
            using Aes aes = Aes.Create();
            aes.Key = key;
            aes.GenerateIV();
            using ICryptoTransform encryptor = aes.CreateEncryptor();
            byte[] cipher = encryptor.TransformFinalBlock(payload, 0, payload.Length);
            byte[] result = new byte[1 + aes.IV.Length + cipher.Length + 32];
            result[0] = CurrentFormatVersion;
            Buffer.BlockCopy(aes.IV, 0, result, 1, aes.IV.Length);
            Buffer.BlockCopy(cipher, 0, result, 1 + aes.IV.Length, cipher.Length);
            using HMACSHA256 hmac = new HMACSHA256(key);
            byte[] tag = hmac.ComputeHash(result, 0, result.Length - 32);
            Buffer.BlockCopy(tag, 0, result, result.Length - 32, tag.Length);
            return result;
        }

        /// <summary>
        /// 将完整临时文件替换为正式文件，优先使用文件系统原子替换能力。
        /// </summary>
        /// <param name="temporaryPath">已完整写入的临时文件。</param>
        /// <param name="path">目标正式文件。</param>
        private static void ReplaceAtomically(string temporaryPath, string path)
        {
            if (!File.Exists(path))
            {
                File.Move(temporaryPath, path);
                return;
            }

            string backupPath = path + ".bak";
            try
            {
                File.Replace(temporaryPath, path, backupPath, true);
                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                }
            }
            catch (PlatformNotSupportedException)
            {
                File.Delete(path);
                File.Move(temporaryPath, path);
            }
        }

        /// <summary>
        /// 校验完整性标签后解密文件正文。
        /// </summary>
        /// <param name="input">加密文件内容。</param>
        /// <param name="key">32 字节主密钥。</param>
        /// <returns>解密后的明文数据。</returns>
        private static byte[] Decrypt(byte[] input, byte[] key)
        {
            if (input == null || input.Length < 1 + 16 + 32 || input[0] != CurrentFormatVersion)
            {
                throw new InvalidDataException("存档格式无效或版本不受支持。");
            }

            int tagOffset = input.Length - 32;
            using HMACSHA256 hmac = new HMACSHA256(key);
            byte[] expected = hmac.ComputeHash(input, 0, tagOffset);
            if (!FixedTimeEquals(expected, input, tagOffset))
            {
                throw new InvalidDataException("存档完整性校验失败。");
            }

            byte[] iv = new byte[16];
            Buffer.BlockCopy(input, 1, iv, 0, iv.Length);
            int cipherLength = tagOffset - 1 - iv.Length;
            using Aes aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            using ICryptoTransform decryptor = aes.CreateDecryptor();
            return decryptor.TransformFinalBlock(input, 17, cipherLength);
        }

        /// <summary>
        /// 使用固定遍历长度比较认证标签，避免将首个不匹配位置暴露给调用方。
        /// </summary>
        /// <param name="expected">根据密文计算出的标签。</param>
        /// <param name="input">包含实际标签的文件内容。</param>
        /// <param name="offset">实际标签在文件内容中的起始位置。</param>
        /// <returns>标签完全一致时返回 true。</returns>
        private static bool FixedTimeEquals(byte[] expected, byte[] input, int offset)
        {
            int difference = 0;
            for (int index = 0; index < expected.Length; index++)
            {
                difference |= expected[index] ^ input[offset + index];
            }

            return difference == 0;
        }

        #endregion
    }
}
