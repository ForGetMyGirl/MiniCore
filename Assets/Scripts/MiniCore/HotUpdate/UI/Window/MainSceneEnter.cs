using Cysharp.Threading.Tasks;
using UnityEngine;
using MiniCore;
using MiniCore.Core;
using MiniCore.Model;
using System;

namespace MiniCore.HotUpdate
{
    public class MainSceneEnter : MonoBehaviour
    {
        public string packageName;
        private AssetsComponent assetsComponent;


        private void Awake()
        {
            Global.Com.Add<TagsComponent>();
            var yooAssetResourceComponent = Global.Com.Add<YooAssetResourceComponent>(new object[] { packageName });
            assetsComponent = Global.Com.Add<AssetsComponent>();
            assetsComponent.RegisterResourcesComponent(yooAssetResourceComponent);
            Global.Com.Add<UIFactoryComponent>();
            Global.Com.Add<NetworkSessionComponent>();
            var netMsg = Global.Com.Add<NetworkMessageComponent>();
            netMsg.SetSerializer(new NewtonsoftJsonSerializer());
            netMsg.RpcTimeout = TimeSpan.FromSeconds(8);
            netMsg.HeartbeatInterval = TimeSpan.FromSeconds(3);
            netMsg.HeartbeatTimeout = TimeSpan.FromSeconds(12);
            Global.Com.Add<TimerComponent>();
        }

        private void Start()
        {
            OpenKcpTestWindowAsync().Forget();
        }

        private UniTask OpenKcpTestWindowAsync()
        {
            return Global.Com.Get<UIFactoryComponent>().OpenAsync<KcpTestWindowView, KcpTestWindowPresenter>(UIAssetPaths.KcpTestWindow, UICanvasLayer.Normal);
        }
    }
}
