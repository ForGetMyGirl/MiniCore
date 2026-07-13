using System;
using MiniCore.Core;
using MiniCore.Model;
using UnityEngine;

namespace MiniCore.HotUpdate
{
    /// <summary>
    /// 多协议网络测试场景的全局基础设施初始化入口。
    /// </summary>
    public class MultiProtocolSceneEnter : MonoBehaviour
    {
        #region Public 公共成员

        /// <summary>
        /// 获取或设置当前场景使用的 YooAsset 资源包名称。
        /// </summary>
        public string packageName;

        #endregion

        #region Private 私有成员

        private AssetsComponent assetsComponent; // 场景使用的资源门面组件。

        /// <summary>
        /// 获取或创建多协议测试所需的常驻全局组件。
        /// </summary>
        private void Awake()
        {
            Global.Com.Pin<TagsComponent>();
            YooAssetResourceComponent yooAssetResourceComponent = Global.Com.Pin<YooAssetResourceComponent>(new YooAssetResourceComponentInitArgs(packageName));
            assetsComponent = Global.Com.Pin<AssetsComponent>();
            assetsComponent.RegisterResourcesComponent(yooAssetResourceComponent);
            Global.Com.Pin<UIFactoryComponent>();

            NetworkMessageComponent networkMessageComponent = Global.Com.Pin<NetworkMessageComponent>();
            networkMessageComponent.SetSerializer(new NewtonsoftJsonSerializer());
            networkMessageComponent.RpcTimeout = TimeSpan.FromSeconds(8);
            networkMessageComponent.HeartbeatInterval = TimeSpan.FromSeconds(3);
            networkMessageComponent.HeartbeatTimeout = TimeSpan.FromSeconds(12);

            Global.Com.Pin<TimerComponent>();
        }

        /// <summary>
        /// 创建多协议测试面板。
        /// </summary>
        private void Start()
        {
            if (FindObjectOfType<MultiProtocolTestPanel>() != null)
            {
                return;
            }

            GameObject panelObject = new GameObject("MultiProtocolTestPanel");
            DontDestroyOnLoad(panelObject);
            panelObject.AddComponent<MultiProtocolTestPanel>();
        }

        #endregion
    }
}
