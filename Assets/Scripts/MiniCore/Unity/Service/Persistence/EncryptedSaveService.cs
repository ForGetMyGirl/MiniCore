using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using MiniCore.Core;
using MiniCore.Model;
using MiniCore.Threading;
using MiniCore.Unity;

namespace MiniCore.Service
{
    /// <summary>
    /// 使用 AES-CBC 加密、HMAC-SHA256 防篡改并通过平台后端保存二进制数据的应用服务。
    /// 客户端内置密钥只能提高篡改和直接读取成本，不能替代服务端可信校验。
    /// </summary>
    [AppService(
        "保护存档",
        typeof(ISaveService),
        Description = "加密、校验并通过当前平台后端保存 Protobuf 等二进制数据。",
        InitArgsType = typeof(EncryptedSaveServiceInitArgs),
        RequiresServices = new[] { typeof(IStoragePathService) },
        RuntimeTargets = AppServiceRuntimeTargets.Client)]
    public sealed class EncryptedSaveService : AAppService, ISaveService
    {
        #region Private 私有成员

        private const byte CurrentFormatVersion = 2; // 当前保护文件格式版本，不兼容旧 JSON 存档。
        private const int MagicLength = 4; // 固定格式标识长度。
        private const int IvLength = 16; // AES-CBC 初始化向量长度。
        private const int TagLength = 32; // HMAC-SHA256 标签长度。
        private static readonly byte[] Magic = { (byte)'M', (byte)'C', (byte)'S', (byte)'B' }; // MiniCore Save Binary 格式标识。
        private byte[] masterKey; // 由开发者口令派生的内存主密钥。
        private IStorageBackend storageBackend; // 当前运行平台的逻辑键二进制后端。
        private ITelemetryService telemetry; // 可选遥测服务。

        #endregion

        #region Override 重写实现

        /// <summary>
        /// 无启动参数时明确拒绝创建，避免使用固定框架默认密钥。
        /// </summary>
        public override void Awake()
        {
            Initialize(string.Empty);
        }

        /// <summary>
        /// 使用开发者配置的稳定口令初始化二进制保护服务。
        /// </summary>
        /// <param name="args">包含稳定口令的启动参数。</param>
        public override void Awake(ComponentInitArgs args)
        {
            if (!(args is EncryptedSaveServiceInitArgs saveArgs))
            {
                throw new ArgumentException("保护存档服务必须使用 EncryptedSaveServiceInitArgs 初始化。", nameof(args));
            }

            Initialize(saveArgs.EncryptionKey);
        }

        /// <summary>
        /// 清除内存主密钥并释放服务引用。
        /// </summary>
        protected override void OnDispose()
        {
            if (masterKey != null)
            {
                Array.Clear(masterKey, 0, masterKey.Length);
                masterKey = null;
            }

            storageBackend = null;
            telemetry = null;
        }

        #endregion

        #region Interface 接口实现

        /// <summary>
        /// 加密并认证二进制数据后写入指定逻辑槽位。
        /// </summary>
        /// <param name="slotName">逻辑槽位名称。</param>
        /// <param name="data">Protobuf 等调用方已经编码完成的二进制数据。</param>
        /// <returns>平台写入事务完成任务。</returns>
        public async MTask SaveAsync(string slotName, byte[] data)
        {
            ValidateSlotName(slotName);
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            MTaskExternal.GetCancellationToken().ThrowIfCancellationRequested();
            byte[] protectedBytes = Protect(slotName, data);
            await storageBackend.WriteAsync(GetStorageKey(slotName), protectedBytes);
            telemetry?.Increment("save.write");
        }

        /// <summary>
        /// 读取、认证并解密指定逻辑槽位的二进制数据。
        /// </summary>
        /// <param name="slotName">逻辑槽位名称。</param>
        /// <returns>槽位不存在时返回空，否则返回独立明文字节数组。</returns>
        public async MTask<byte[]> LoadAsync(string slotName)
        {
            ValidateSlotName(slotName);
            MTaskExternal.GetCancellationToken().ThrowIfCancellationRequested();
            byte[] protectedBytes = await storageBackend.ReadAsync(GetStorageKey(slotName));
            if (protectedBytes == null)
            {
                return null;
            }

            byte[] data = Unprotect(slotName, protectedBytes);
            telemetry?.Increment("save.read");
            return data;
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 派生内存主密钥并选择浏览器注册后端或原生文件后端。
        /// </summary>
        /// <param name="encryptionKey">开发者配置的稳定口令。</param>
        private void Initialize(string encryptionKey)
        {
            if (string.IsNullOrWhiteSpace(encryptionKey))
            {
                throw new InvalidOperationException("启用保护存档前必须配置稳定口令。");
            }

            if (!StorageBackendRegistry.TryCreate(out storageBackend))
            {
                if (!Global.TryGetService(this, out IStoragePathService pathService))
                {
                    throw new InvalidOperationException(
                        "未注册当前平台的存储后端，也未配置可用于原生文件回退的 IStoragePathService。");
                }

                storageBackend = new FileStorageBackend(pathService.GetDirectory("Storage"));
            }

            byte[] source = Encoding.UTF8.GetBytes(encryptionKey);
            try
            {
                using (SHA256 sha256 = SHA256.Create())
                {
                    masterKey = sha256.ComputeHash(source);
                }
            }
            finally
            {
                Array.Clear(source, 0, source.Length);
            }

            Global.TryGetService(this, out telemetry);
        }

        /// <summary>
        /// 使用独立加密密钥和认证密钥生成 Encrypt-then-MAC 格式。
        /// </summary>
        /// <param name="slotName">密钥派生所绑定的逻辑槽位。</param>
        /// <param name="data">明文二进制数据。</param>
        /// <returns>格式头、IV、密文和认证标签组成的保护数据。</returns>
        private byte[] Protect(string slotName, byte[] data)
        {
            byte[] encryptionKey = DeriveKey("enc", slotName);
            byte[] authenticationKey = DeriveKey("mac", slotName);
            try
            {
                using (Aes aes = Aes.Create())
                {
                    aes.Key = encryptionKey;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                    aes.GenerateIV();
                    byte[] cipher;
                    using (ICryptoTransform encryptor = aes.CreateEncryptor())
                    {
                        cipher = encryptor.TransformFinalBlock(data, 0, data.Length);
                    }

                    int tagOffset = checked(MagicLength + 1 + IvLength + cipher.Length);
                    byte[] result = new byte[tagOffset + TagLength];
                    Buffer.BlockCopy(Magic, 0, result, 0, MagicLength);
                    result[MagicLength] = CurrentFormatVersion;
                    Buffer.BlockCopy(aes.IV, 0, result, MagicLength + 1, IvLength);
                    Buffer.BlockCopy(cipher, 0, result, MagicLength + 1 + IvLength, cipher.Length);
                    using (HMACSHA256 hmac = new HMACSHA256(authenticationKey))
                    {
                        byte[] tag = hmac.ComputeHash(result, 0, tagOffset);
                        Buffer.BlockCopy(tag, 0, result, tagOffset, tag.Length);
                        Array.Clear(tag, 0, tag.Length);
                    }

                    Array.Clear(cipher, 0, cipher.Length);
                    return result;
                }
            }
            finally
            {
                Array.Clear(encryptionKey, 0, encryptionKey.Length);
                Array.Clear(authenticationKey, 0, authenticationKey.Length);
            }
        }

        /// <summary>
        /// 先以固定时间比较认证标签，再解密通过校验的密文。
        /// </summary>
        /// <param name="slotName">密钥派生所绑定的逻辑槽位。</param>
        /// <param name="input">完整保护数据。</param>
        /// <returns>通过认证后的明文字节。</returns>
        private byte[] Unprotect(string slotName, byte[] input)
        {
            int minimumLength = MagicLength + 1 + IvLength + 16 + TagLength;
            if (input == null || input.Length < minimumLength || !HasValidHeader(input))
            {
                throw new InvalidDataException("存档格式无效或版本不受支持。");
            }

            byte[] encryptionKey = DeriveKey("enc", slotName);
            byte[] authenticationKey = DeriveKey("mac", slotName);
            try
            {
                int tagOffset = input.Length - TagLength;
                byte[] expected;
                using (HMACSHA256 hmac = new HMACSHA256(authenticationKey))
                {
                    expected = hmac.ComputeHash(input, 0, tagOffset);
                }

                bool authenticated = FixedTimeEquals(expected, input, tagOffset);
                Array.Clear(expected, 0, expected.Length);
                if (!authenticated)
                {
                    throw new InvalidDataException("存档完整性校验失败，数据可能已被修改。");
                }

                byte[] iv = new byte[IvLength];
                Buffer.BlockCopy(input, MagicLength + 1, iv, 0, IvLength);
                int cipherOffset = MagicLength + 1 + IvLength;
                int cipherLength = tagOffset - cipherOffset;
                using (Aes aes = Aes.Create())
                {
                    aes.Key = encryptionKey;
                    aes.IV = iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                    using (ICryptoTransform decryptor = aes.CreateDecryptor())
                    {
                        return decryptor.TransformFinalBlock(input, cipherOffset, cipherLength);
                    }
                }
            }
            finally
            {
                Array.Clear(encryptionKey, 0, encryptionKey.Length);
                Array.Clear(authenticationKey, 0, authenticationKey.Length);
            }
        }

        /// <summary>
        /// 按用途和槽位从内存主密钥派生独立子密钥。
        /// </summary>
        /// <param name="purpose">稳定用途标签。</param>
        /// <param name="slotName">逻辑槽位。</param>
        /// <returns>32 字节子密钥。</returns>
        private byte[] DeriveKey(string purpose, string slotName)
        {
            byte[] context = Encoding.UTF8.GetBytes(purpose + "\0" + slotName);
            try
            {
                using (HMACSHA256 hmac = new HMACSHA256(masterKey))
                {
                    return hmac.ComputeHash(context);
                }
            }
            finally
            {
                Array.Clear(context, 0, context.Length);
            }
        }

        /// <summary>
        /// 校验固定格式标识和版本。
        /// </summary>
        /// <param name="input">完整保护数据。</param>
        /// <returns>格式头有效时返回 true。</returns>
        private static bool HasValidHeader(byte[] input)
        {
            int difference = input[MagicLength] ^ CurrentFormatVersion;
            for (int index = 0; index < MagicLength; index++)
            {
                difference |= input[index] ^ Magic[index];
            }

            return difference == 0;
        }

        /// <summary>
        /// 使用固定遍历长度比较认证标签。
        /// </summary>
        /// <param name="expected">本地计算标签。</param>
        /// <param name="input">包含实际标签的完整数据。</param>
        /// <param name="offset">实际标签起始位置。</param>
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

        /// <summary>
        /// 校验业务槽位名称不包含路径语义。
        /// </summary>
        /// <param name="slotName">待校验槽位。</param>
        private static void ValidateSlotName(string slotName)
        {
            if (string.IsNullOrWhiteSpace(slotName)
                || slotName.Contains("..")
                || slotName.IndexOf('/') >= 0
                || slotName.IndexOf('\\') >= 0)
            {
                throw new ArgumentException("存档槽位名称无效。", nameof(slotName));
            }
        }

        /// <summary>
        /// 为平台后端生成不包含实际路径的稳定逻辑键。
        /// </summary>
        /// <param name="slotName">已校验的槽位名称。</param>
        /// <returns>平台无关逻辑键。</returns>
        private static string GetStorageKey(string slotName)
        {
            return "save:" + slotName;
        }

        #endregion
    }
}
