using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using MiniCore.Service;
using MiniCore.Threading;

namespace MiniCore.Unity
{
    /// <summary>
    /// 在原生平台以原子文件替换实现的逻辑键二进制存储后端。
    /// </summary>
    internal sealed class FileStorageBackend : IStorageBackend
    {
        #region Private 私有成员

        private readonly string rootPath; // 后端独占的实际文件目录。
        private readonly IMTaskExecutor ioExecutor; // 文件读写使用的执行器。

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 创建指定目录下的文件存储后端。
        /// </summary>
        /// <param name="rootPath">已经过边界校验的持久化目录。</param>
        internal FileStorageBackend(string rootPath)
        {
            this.rootPath = string.IsNullOrWhiteSpace(rootPath)
                ? throw new ArgumentException("文件存储目录不能为空。", nameof(rootPath))
                : rootPath;
            Directory.CreateDirectory(rootPath);
            ioExecutor = MTaskExecutors.TryGetThreadPool(out IMTaskExecutor threadPool)
                ? threadPool
                : MTaskExecutors.Unity;
        }

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 从后台执行器读取指定逻辑键。
        /// </summary>
        /// <param name="key">逻辑键。</param>
        /// <returns>键不存在时返回空。</returns>
        public async MTask<byte[]> ReadAsync(string key)
        {
            string path = GetPath(key);
            await MTask.SwitchTo(ioExecutor);
            return File.Exists(path) ? File.ReadAllBytes(path) : null;
        }

        /// <summary>
        /// 在后台执行器完整写入临时文件后原子替换正式文件。
        /// </summary>
        /// <param name="key">逻辑键。</param>
        /// <param name="bytes">需要保存的字节数组。</param>
        /// <returns>替换完成任务。</returns>
        public async MTask WriteAsync(string key, byte[] bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            string path = GetPath(key);
            string temporaryPath = path + ".tmp";
            await MTask.SwitchTo(ioExecutor);
            File.WriteAllBytes(temporaryPath, bytes);
            ReplaceAtomically(temporaryPath, path);
        }

        /// <summary>
        /// 在后台执行器删除指定逻辑键。
        /// </summary>
        /// <param name="key">逻辑键。</param>
        /// <returns>删除完成任务。</returns>
        public async MTask DeleteAsync(string key)
        {
            string path = GetPath(key);
            await MTask.SwitchTo(ioExecutor);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        /// <summary>
        /// 在后台执行器查询指定逻辑键是否存在。
        /// </summary>
        /// <param name="key">逻辑键。</param>
        /// <returns>正式文件存在时返回 true。</returns>
        public async MTask<bool> ExistsAsync(string key)
        {
            string path = GetPath(key);
            await MTask.SwitchTo(ioExecutor);
            return File.Exists(path);
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 将逻辑键映射为不泄露业务路径且不会越界的稳定文件名。
        /// </summary>
        /// <param name="key">逻辑键。</param>
        /// <returns>位于后端根目录内的完整路径。</returns>
        private string GetPath(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("存储逻辑键不能为空。", nameof(key));
            }

            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] digest;
            using (SHA256 sha256 = SHA256.Create())
            {
                digest = sha256.ComputeHash(keyBytes);
            }

            var name = new StringBuilder(digest.Length * 2 + 4);
            for (int index = 0; index < digest.Length; index++)
            {
                name.Append(digest[index].ToString("x2"));
            }

            name.Append(".bin");
            return Path.Combine(rootPath, name.ToString());
        }

        /// <summary>
        /// 优先使用平台原子替换能力提交已经完整写入的临时文件。
        /// </summary>
        /// <param name="temporaryPath">已完整写入的临时文件。</param>
        /// <param name="path">正式文件路径。</param>
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

        #endregion
    }
}
