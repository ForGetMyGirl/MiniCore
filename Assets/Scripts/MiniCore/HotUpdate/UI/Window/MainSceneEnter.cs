using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using MiniCore;
using MiniCore.Core;
using MiniCore.Model;

namespace MiniCore.HotUpdate
{
    public class MainSceneEnter : MonoBehaviour
    {
        public string packageName;
        public string kcpTestWindowPath;
        private AssetsComponent assetsComponent;


        private async void Awake()
        {
            Global.Com.Add<TagsComponent>();
            var yooAssetResourceComponent = Global.Com.Add<YooAssetResourceComponent>(new object[] { packageName });
            assetsComponent = Global.Com.Add<AssetsComponent>();
            assetsComponent.RegisterResourcesComponent(yooAssetResourceComponent);
            var uiFactoryComponent = Global.Com.Add<UIFactoryComponent>();
            Global.Com.Add<NetworkSessionComponent>();
            var netMsg = Global.Com.Add<NetworkMessageComponent>();
            netMsg.SetSerializer(new UnityJsonSerializer());

            if (!string.IsNullOrEmpty(kcpTestWindowPath))
            {
                await uiFactoryComponent.OpenAsync<KcpTestWindowView, KcpTestWindowPresenter>(kcpTestWindowPath, UICanvasLayer.Normal);
            }

        }

    }
}
