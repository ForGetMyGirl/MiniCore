using System;
using System.IO;
using MiniCore.Model;
using Newtonsoft.Json;
using UnityEngine;

namespace MiniCore.Server
{
    /// <summary>
    /// 在 Dedicated Server AppService 装配前读取并校验部署配置。
    /// </summary>
    public static class DedicatedServerRuntimeBootstrap
    {
        #region Private 私有成员

        private const string RuntimeConfigFileName = "MiniCoreServerRuntime.json"; // DS Player 内固定配置文件名。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 获取当前 Dedicated Server 已加载的部署配置。
        /// </summary>
        public static MiniCoreServerRuntimeConfig Current { get; private set; }

        /// <summary>
        /// 从当前 Player 的 StreamingAssets 加载配置并发布 Role 上下文。
        /// </summary>
        public static void Prepare()
        {
            string path = Path.Combine(Application.streamingAssetsPath, RuntimeConfigFileName);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Dedicated Server 缺少运行配置。请使用 DS 构建目标注入配置，或将配置放入部署副本的 StreamingAssets。", path);
            }

            MiniCoreServerRuntimeConfig config = JsonConvert.DeserializeObject<MiniCoreServerRuntimeConfig>(File.ReadAllText(path));
            Validate(config);
            DedicatedServerRuntimeContext.Configure(config.ParseRoles());
            Current = config;
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 校验配置中参与监听、发现和实例寻址的必要字段。
        /// </summary>
        /// <param name="config">待校验配置。</param>
        private static void Validate(MiniCoreServerRuntimeConfig config)
        {
            if (config == null)
            {
                throw new InvalidDataException("Dedicated Server 运行配置不是有效 JSON 对象。");
            }

            if (string.IsNullOrWhiteSpace(config.InstanceId))
            {
                throw new InvalidDataException("Dedicated Server instanceId 不能为空。");
            }

            config.ParseRoles();
            config.ParsePersistenceMode();
            ValidatePort(config.Coordinator?.InnerPort ?? 0, "coordinator.innerPort");
            ValidatePort(config.Listeners?.InnerPort ?? 0, "listeners.innerPort");
            ValidatePort(config.Listeners?.OuterPort ?? 0, "listeners.outerPort");
            ValidatePort(config.Advertised?.InnerPort ?? 0, "advertised.innerPort");
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
