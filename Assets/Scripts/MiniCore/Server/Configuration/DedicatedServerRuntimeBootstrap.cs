using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using MiniCore.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace MiniCore.Server
{
    /// <summary>
    /// 在 Dedicated Server AppService 装配前读取并校验部署配置。
    /// </summary>
    public static class DedicatedServerRuntimeBootstrap
    {
        #region Private 私有成员

        private const string RuntimeConfigArgument = "--minicore-config"; // 显式外部实例配置参数。
        private const string RoleCatalogFileName = "ServerRoleCatalog.json"; // 随不可变制品发布的 Role 目录。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 获取当前 Dedicated Server 已加载的部署配置。
        /// </summary>
        public static MiniCoreServerRuntimeConfig Current { get; private set; }

        /// <summary>
        /// 获取随当前制品加载并校验的 Role Catalog。
        /// </summary>
        public static ServerRoleCatalog CurrentRoleCatalog { get; private set; }

        /// <summary>
        /// 从显式绝对路径加载实例配置，从制品加载 Role Catalog 并发布 Role 上下文。
        /// </summary>
        public static void Prepare()
        {
            string configPath = GetRequiredConfigPath();
            string catalogPath = Path.Combine(Application.streamingAssetsPath, RoleCatalogFileName);
            if (!File.Exists(catalogPath))
            {
                throw new FileNotFoundException("Dedicated Server 制品缺少 ServerRoleCatalog.json，请重新执行 MiniCore Deploy 构建。", catalogPath);
            }

            string configJson = File.ReadAllText(configPath);
            MiniCoreServerRuntimeConfig config = JsonConvert.DeserializeObject<MiniCoreServerRuntimeConfig>(configJson);
            ServerRoleCatalog catalog = JsonConvert.DeserializeObject<ServerRoleCatalog>(File.ReadAllText(catalogPath));
            Validate(config, catalog, configJson);
            DedicatedServerRuntimeContext.Configure(config.ParseRoles(catalog));
            Current = config;
            CurrentRoleCatalog = catalog;
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 校验配置中参与监听、发现和实例寻址的必要字段。
        /// </summary>
        /// <param name="config">待校验配置。</param>
        /// <param name="catalog">制品 Role Catalog。</param>
        /// <param name="configJson">未修改的外部配置 JSON。</param>
        private static void Validate(MiniCoreServerRuntimeConfig config, ServerRoleCatalog catalog, string configJson)
        {
            if (config == null)
            {
                throw new InvalidDataException("Dedicated Server 运行配置不是有效 JSON 对象。");
            }

            if (catalog == null)
            {
                throw new InvalidDataException("Dedicated Server Role Catalog 不是有效 JSON 对象。");
            }

            catalog.Validate();
            if (string.IsNullOrWhiteSpace(config.EnvironmentId)
                || string.IsNullOrWhiteSpace(config.InstanceId)
                || string.IsNullOrWhiteSpace(config.ReleaseVersion)
                || string.IsNullOrWhiteSpace(config.ControlProtocolVersion)
                || string.IsNullOrWhiteSpace(config.ConfigVersion)
                || string.IsNullOrWhiteSpace(config.ConfigSha256))
            {
                throw new InvalidDataException("Dedicated Server environmentId、instanceId、releaseVersion、controlProtocolVersion、configVersion 和 configSha256 均不能为空。");
            }

            VerifyConfigHash(config.ConfigSha256, configJson);
            config.ParseRoles(catalog);
            config.ParsePersistenceMode();
            ValidatePort(config.Coordinator?.InnerPort ?? 0, "coordinator.innerPort");
            ValidatePort(config.Listeners?.InnerPort ?? 0, "listeners.innerPort");
            if ((config.Listeners?.OuterPort ?? 0) > 0)
            {
                ValidatePort(config.Listeners.OuterPort, "listeners.outerPort");
            }

            ValidatePort(config.Advertised?.InnerPort ?? 0, "advertised.innerPort");
            ValidatePort(config.Management?.Port ?? 0, "management.port");
            if (!string.Equals(config.Management?.Host, "127.0.0.1", StringComparison.Ordinal)
                || string.IsNullOrWhiteSpace(config.Management.TokenFile)
                || !Path.IsPathRooted(config.Management.TokenFile)
                || string.IsNullOrWhiteSpace(config.LogPath)
                || !Path.IsPathRooted(config.LogPath))
            {
                throw new InvalidDataException("Dedicated Server 管理端必须只监听 127.0.0.1，Token 和日志必须使用绝对路径。");
            }
        }

        /// <summary>
        /// 读取 --minicore-config 后的绝对外部配置路径。
        /// </summary>
        /// <returns>存在的配置路径。</returns>
        private static string GetRequiredConfigPath()
        {
            string[] arguments = Environment.GetCommandLineArgs();
            for (int index = 0; index < arguments.Length - 1; index++)
            {
                if (!string.Equals(arguments[index], RuntimeConfigArgument, StringComparison.Ordinal))
                {
                    continue;
                }

                string path = arguments[index + 1];
                if (!Path.IsPathRooted(path))
                {
                    throw new InvalidDataException("--minicore-config 必须指定绝对路径，避免服务管理器工作目录影响配置选择。");
                }

                if (!File.Exists(path))
                {
                    throw new FileNotFoundException("Dedicated Server 外部实例配置不存在。", path);
                }

                return Path.GetFullPath(path);
            }

            throw new InvalidDataException("Dedicated Server 必须通过 --minicore-config <absolute-path> 指定外部实例配置。");
        }

        /// <summary>
        /// 移除 configSha256 字段后按紧凑 JSON 重新计算 SHA-256。
        /// </summary>
        /// <param name="expected">配置声明的哈希。</param>
        /// <param name="configJson">原始 JSON。</param>
        private static void VerifyConfigHash(string expected, string configJson)
        {
            JObject document = JObject.Parse(configJson);
            document.Remove("configSha256");
            string canonical = document.ToString(Formatting.None);
            byte[] bytes = Encoding.UTF8.GetBytes(canonical);
            string actual;
            using (SHA256 sha256 = SHA256.Create())
            {
                actual = ToLowerHex(sha256.ComputeHash(bytes));
            }

            if (!string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException($"Dedicated Server 配置 SHA-256 不匹配：期望 {expected}，实际 {actual}。");
            }
        }

        /// <summary>
        /// 将哈希字节转换为不分配格式器的十六进制小写文本。
        /// </summary>
        /// <param name="bytes">哈希字节。</param>
        /// <returns>十六进制文本。</returns>
        private static string ToLowerHex(byte[] bytes)
        {
            const string alphabet = "0123456789abcdef";
            var characters = new char[bytes.Length * 2];
            for (int index = 0; index < bytes.Length; index++)
            {
                byte value = bytes[index];
                characters[index * 2] = alphabet[value >> 4];
                characters[index * 2 + 1] = alphabet[value & 0x0F];
            }

            return new string(characters);
        }

        /// <summary>
        /// 校验一个 TCP 或 WebSocket 端口。
        /// </summary>
        /// <param name="port">待校验端口。</param>
        /// <param name="field">配置字段路径。</param>
        private static void ValidatePort(int port, string field)
        {
            if (port <= 0 || port > 65535)
            {
                throw new InvalidDataException($"Dedicated Server {field} 必须位于 1 到 65535 之间。");
            }
        }

        #endregion
    }
}
