using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using MiniCore;
using MiniCore.Core;
using MiniCore.Model;


namespace MiniCore.HotUpdate
{
    public class TestWindowPresenter : APresenter<TestWindowView>
    {
        protected override void OnBind()
        {
            View.OnTestBtnClickedEvent += OnTestBtnClicked;
            View.OnRpcBtnClickedEvent += OnRpc;
            View.OnNormalMsgBtnClickedEvent += OnNormalMsg;
            View.OnConnectBtnClickedEvent += OnConnect;
        }

        private string host = "127.0.0.1";
        private int port = 7777;
        private bool connected;

        private void OnConnect()
        {
            ConnectAsync().Forget();
        }

        private async UniTaskVoid ConnectAsync()
        {
            if (connected)
            {
                View.UpdatePrompt("已连接服务器");
                return;
            }
            try
            {
                var net = Global.Com.Get<NetworkMessageComponent>();
                await net.InitializeDefaultSessionAsync(host, port);
                connected = true;
                View.UpdatePrompt($"已连接 {host}:{port}");
            }
            catch (Exception ex)
            {
                View.UpdatePrompt($"连接失败: {ex.Message}");
                EventCenter.Broadcast(GameEvent.LogError, ex);
            }
        }

        private void OnNormalMsg()
        {
            SendNormalMsg().Forget();
        }

        private async UniTaskVoid SendNormalMsg()
        {
            try
            {
                var net = Global.Com.Get<NetworkMessageComponent>();
                string content = $"Client normal msg at {DateTime.Now:O}";
                await net.SendAsync(new DemoNormalMessage { Content = content });
                EventCenter.Broadcast(GameEvent.LogInfo, $"[Client] Sent normal msg: {content}");
                View.UpdatePrompt(content);
            }
            catch (Exception ex)
            {
                EventCenter.Broadcast(GameEvent.LogError, $"[Client] Send normal msg failed: {ex.Message}");
                View.UpdatePrompt($"Normal send failed: {ex.Message}");
            }
        }

        private void OnRpc()
        {
            SendRpc().Forget();
        }

        private async UniTaskVoid SendRpc()
        {
            try
            {
                var net = Global.Com.Get<NetworkMessageComponent>();
                var req = new DemoRpcRequest { Payload = $"Hello RPC {DateTime.Now:O}" };
                DemoRpcResponse resp = await net.CallAsync<DemoRpcRequest, DemoRpcResponse>(req);
                string msg = $"RPC resp code:{resp.ErrorCode} msg:{resp.Message} echo:{resp.Echo}";
                EventCenter.Broadcast(GameEvent.LogInfo, $"[Client] {msg}");
                View.UpdatePrompt(msg);
            }
            catch (Exception ex)
            {
                EventCenter.Broadcast(GameEvent.LogError, $"[Client] RPC failed: {ex.Message}");
                View.UpdatePrompt($"RPC failed: {ex.Message}");
            }
        }

        private void OnTestBtnClicked()
        {
            EventCenter.Broadcast(GameEvent.LogInfo, "MVP模式按下了测试按钮");
        }

        public void UpdateData(string testStr)
        {
            View.UpdatePrompt(testStr);
        }
    }

}