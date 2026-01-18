using Cysharp.Threading.Tasks;
using System.Collections.Generic;
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
            var uiFactoryComponent = Global.Com.Add<UIFactoryComponent>();
            var sessionComponent = Global.Com.Add<NetworkSessionComponent>();
            var netMsg = Global.Com.Add<NetworkMessageComponent>();
            netMsg.SetSerializer(new UnityJsonSerializer());
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
