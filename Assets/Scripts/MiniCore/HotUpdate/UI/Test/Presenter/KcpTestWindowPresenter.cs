using System;
using Cysharp.Threading.Tasks;
using MiniCore.Core;
using MiniCore.Model;

namespace MiniCore.HotUpdate
{
    public class KcpTestWindowPresenter : APresenter<KcpTestWindowView>
    {
        private const string ClientSessionId = "kcp-client";
        private const uint DefaultConv = 1001;

        private string host = "127.0.0.1";
        private int port = 20002;

        private bool clientConnected;
        private bool serverRunning;
        private bool serverHandlersBound;
        private NetworkSessionComponent sessionComponent;

        protected override void OnBind()
        {
            sessionComponent = Global.Com.Get<NetworkSessionComponent>();
            EventCenter.AddListener<string>(HotEvent.KcpTestMessage, OnKcpTestMessage);
            View.OnStartServerClicked += StartServer;
            View.OnStopServerClicked += StopServer;
            View.OnConnectClientClicked += ConnectClient;
            View.OnDisconnectClientClicked += DisconnectClient;
            View.OnSendRpcClicked += SendRpc;
            View.OnSendNormalClicked += SendNormal;
        }

        public override void UnbindView()
        {
            EventCenter.RemoveListener<string>(HotEvent.KcpTestMessage, OnKcpTestMessage);
            base.UnbindView();
        }

        private void StartServer()
        {
            StartServerAsync().Forget();
        }

        private async UniTaskVoid StartServerAsync()
        {
            if (serverRunning)
            {
                View.UpdatePrompt("服务器已在运行。");
                return;
            }

            if (!View.TryGetPort(port, out int listenPort))
            {
                View.UpdatePrompt("端口输入无效。");
                return;
            }

            if (!serverHandlersBound)
            {
                sessionComponent.OnServerSessionCreated += HandleServerSessionCreated;
                sessionComponent.OnServerSessionClosed += HandleServerSessionClosed;
                serverHandlersBound = true;
            }

            port = listenPort;
            await sessionComponent.StartKcpServerAsync("0.0.0.0", listenPort, new KcpServerConfig
            {
                Interval = 10,
                SessionTimeoutMs = 30000
            });
            serverRunning = true;
            View.UpdatePrompt($"服务器已启动，端口:{listenPort}");
        }

        private void StopServer()
        {
            sessionComponent.StopKcpServer();
            serverRunning = false;
            View.UpdatePrompt("服务器已停止。");
        }

        private void ConnectClient()
        {
            ConnectClientAsync().Forget();
        }

        private void DisconnectClient()
        {
            if (!clientConnected)
            {
                View.UpdatePrompt("客户端未连接，无法断开。");
                return;
            }

            var session = sessionComponent.GetSession(ClientSessionId);
            session?.Transport?.Disconnect();
            sessionComponent.RemoveSession(ClientSessionId);
            clientConnected = false;
            View.UpdatePrompt("客户端已主动断开连接。");
        }

        private async UniTaskVoid ConnectClientAsync()
        {
            if (clientConnected)
            {
                View.UpdatePrompt("客户端已连接。");
                return;
            }

            try
            {
                View.UpdatePrompt("客户端正在连接...");
                if (!View.TryGetPort(port, out int connectPort))
                {
                    View.UpdatePrompt("端口输入无效。");
                    return;
                }
                if (!View.TryGetConv(DefaultConv, out uint conv))
                {
                    View.UpdatePrompt("Conv 输入无效。");
                    return;
                }
                host = View.GetHostOrDefault(host);
                port = connectPort;
                View.UpdatePrompt($"连接参数 host:{host} port:{connectPort} conv:{conv}");

                var net = Global.Com.Get<NetworkMessageComponent>();
                await net.InitializeKcpSessionAsync(ClientSessionId, host, connectPort, conv);
                clientConnected = true;
                View.UpdatePrompt($"客户端已连接 {host}:{connectPort} conv:{conv}");
            }
            catch (Exception ex)
            {
                View.UpdatePrompt($"客户端连接失败：{ex.Message}");
                EventCenter.Broadcast(GameEvent.LogError, ex);
            }
        }

        private void SendNormal()
        {
            SendNormalAsync().Forget();
        }

        private void SendRpc()
        {
            SendRpcAsync().Forget();
        }

        private async UniTaskVoid SendNormalAsync()
        {
            if (!clientConnected)
            {
                View.UpdatePrompt("客户端未连接。");
                return;
            }

            try
            {
                var net = Global.Com.Get<NetworkMessageComponent>();
                string content = View.GetMessageOrDefault($"KCP 测试消息 {DateTime.Now:O}");
                await net.SendAsync(ClientSessionId, new DemoNormalMessage { Content = content });
                View.UpdatePrompt($"客户端已发送：{content}");
            }
            catch (Exception ex)
            {
                EventCenter.Broadcast(GameEvent.LogError, $"客户端发送失败: {ex.Message}");
                View.UpdatePrompt($"客户端发送失败：{ex.Message}");
            }
        }

        private async UniTaskVoid SendRpcAsync()
        {
            if (!clientConnected)
            {
                View.UpdatePrompt("客户端未连接。");
                return;
            }

            try
            {
                var net = Global.Com.Get<NetworkMessageComponent>();
                string payload = View.GetMessageOrDefault($"RPC 测试 {DateTime.Now:O}");
                var req = new DemoRpcRequest { Payload = payload };
                DemoRpcResponse resp = await net.CallAsync<DemoRpcRequest, DemoRpcResponse>(ClientSessionId, req);
                string msg = $"RPC 响应 code:{resp.ErrorCode} msg:{resp.Message} echo:{resp.Echo}";
                View.UpdatePrompt(msg);
            }
            catch (Exception ex)
            {
                View.UpdatePrompt($"RPC 请求失败：{ex.Message}");
                EventCenter.Broadcast(GameEvent.LogError, ex);
            }
        }

        private void HandleServerSessionCreated(NetworkSession session)
        {
            View.UpdatePrompt($"服务端会话已创建：{session.SessionId}");
            var net = Global.Com.Get<NetworkMessageComponent>();
            net.BindServerSessionReceiver(session.SessionId);
        }

        private void HandleServerSessionClosed(string sessionId)
        {
            View.UpdatePrompt($"服务端会话已关闭：{sessionId}");
        }

        private void OnKcpTestMessage(string message)
        {
            if (View != null)
            {
                View.UpdatePrompt(message);
            }
        }
    }
}
