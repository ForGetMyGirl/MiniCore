using System;
using MiniCore;
using MiniCore.Core;
using MiniCore.Model;
using UnityEngine;

namespace MiniCore.HotUpdate
{
    public class MultiProtocolSceneEnter : MonoBehaviour
    {
        public string packageName;
        private AssetsComponent assetsComponent;

        private void Awake()
        {
            Global.Com.GetOrAdd<TagsComponent>();
            var yooAssetResourceComponent = Global.Com.GetOrAdd<YooAssetResourceComponent>(new object[] { packageName });
            assetsComponent = Global.Com.GetOrAdd<AssetsComponent>();
            assetsComponent.RegisterResourcesComponent(yooAssetResourceComponent);
            Global.Com.GetOrAdd<UIFactoryComponent>();

            var net = Global.Com.GetOrAdd<NetworkMessageComponent>();
            net.SetSerializer(new NewtonsoftJsonSerializer());
            net.RpcTimeout = TimeSpan.FromSeconds(8);
            net.HeartbeatInterval = TimeSpan.FromSeconds(3);
            net.HeartbeatTimeout = TimeSpan.FromSeconds(12);

            Global.Com.GetOrAdd<TimerComponent>();
        }

        private void Start()
        {
            if (FindObjectOfType<MultiProtocolTestPanel>() == null)
            {
                var go = new GameObject("MultiProtocolTestPanel");
                DontDestroyOnLoad(go);
                go.AddComponent<MultiProtocolTestPanel>();
            }
        }
    }
}
