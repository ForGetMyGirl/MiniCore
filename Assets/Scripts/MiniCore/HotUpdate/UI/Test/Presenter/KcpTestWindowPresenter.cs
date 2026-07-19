using System;
using MiniCore.Threading;
using MiniCore;
using MiniCore.Core;
using MiniCore.Eventing;
using MiniCore.Model;
using MiniCore.Protocol.Generated;
using MiniCore.Service;

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
        private bool localJoined;
        private INetworkService net;
        private IApplicationEventBus eventBus;
        private Action clientDisconnectedHandler;
        private EventSubscription messageSubscription;

        protected override void OnBind()
        {
            net = Global.GetService<INetworkService>(this);
            eventBus = Global.GetOrAddModule<IApplicationEventBus>(this);
            messageSubscription = eventBus.Subscribe<DemoMessageReceivedEvent>(OnDemoMessageReceived);
            View.OnStartServerClicked += StartServer;
            View.OnStopServerClicked += () => StopServerAsync().Forget();
            View.OnConnectClientClicked += ConnectClient;
            View.OnDisconnectClientClicked += () => DisconnectClientAsync().Forget();
            View.OnSendRpcClicked += SendRpc;
            View.OnSendNormalClicked += SendNormal;

            if (net != null)
            {
                net.OnServerSessionCreated += HandleServerSessionCreated;
                net.OnServerSessionClosed += HandleServerSessionClosed;
            }
        }

        public override void UnbindView()
        {
            messageSubscription.Dispose();
            eventBus = null;

            if (net != null)
            {
                net.OnServerSessionCreated -= HandleServerSessionCreated;
                net.OnServerSessionClosed -= HandleServerSessionClosed;
            }

            var session = net?.GetSession(ClientSessionId);
            if (session?.Transport != null && clientDisconnectedHandler != null)
            {
                session.Transport.OnDisconnected -= clientDisconnectedHandler;
            }

            Global.ReleaseAll(this);
            base.UnbindView();
        }

        private void StartServer()
        {
            StartServerAsync().Forget();
        }

        /// <summary>
        /// 将强类型示例网络事件转发为窗口既有的文本展示逻辑。
        /// </summary>
        /// <param name="@event">包含格式化网络文本的业务事件。</param>
        private void OnDemoMessageReceived(DemoMessageReceivedEvent @event)
        {
            OnKcpTestMessage(@event.Message);
        }

        private async MTask StartServerAsync()
        {
            if (net == null)
            {
                View.UpdatePrompt("NetworkService missing.");
                return;
            }

            if (serverRunning)
            {
                View.UpdatePrompt("Server already running.");
                return;
            }

            if (!View.TryGetPort(port, out int listenPort))
            {
                View.UpdatePrompt("Invalid port.");
                return;
            }

            port = listenPort;
            await net.StartKcpServerAsync("0.0.0.0", listenPort, new KcpServerConfig
            {
                Interval = 10,
                SessionTimeoutMs = 30000
            });

            serverRunning = true;
            View.UpdatePrompt($"Server started on port {listenPort}.");
        }

        private void ConnectClient()
        {
            ConnectClientAsync().Forget();
        }

        private async MTask ConnectClientAsync()
        {
            if (net == null)
            {
                View.UpdatePrompt("NetworkService missing.");
                return;
            }

            if (clientConnected)
            {
                var session = net.GetSession(ClientSessionId);
                if (session != null && session.IsConnected)
                {
                    bool alive = await net.ProbeSessionAsync(ClientSessionId, TimeSpan.FromMilliseconds(500));
                    if (alive)
                    {
                        View.UpdatePrompt("Client already connected.");
                        return;
                    }
                }

                net.DisconnectSession(ClientSessionId);
                clientConnected = false;
            }

            var existingSession = net.GetSession(ClientSessionId);
            if (clientConnected && (existingSession == null || !existingSession.IsConnected))
            {
                clientConnected = false;
            }

            if (clientConnected)
            {
                View.UpdatePrompt("Client already connected.");
                return;
            }

            try
            {
                View.UpdatePrompt("Client connecting...");
                if (!View.TryGetPort(port, out int connectPort))
                {
                    View.UpdatePrompt("Invalid port.");
                    return;
                }

                if (!View.TryGetConv(DefaultConv, out uint conv))
                {
                    View.UpdatePrompt("Invalid conv.");
                    return;
                }

                host = View.GetHostOrDefault(host);
                port = connectPort;
                View.UpdatePrompt($"Connect args host:{host} port:{connectPort} conv:{conv}");

                bool ok = await net.ConnectKcpSessionAsync(ClientSessionId, host, connectPort, conv);
                if (!ok)
                {
                    clientConnected = false;
                    View.UpdatePrompt("Client connect failed (probe timeout).");
                    return;
                }

                clientConnected = true;
                var session = net.GetSession(ClientSessionId);
                if (session?.Transport != null)
                {
                    if (clientDisconnectedHandler != null)
                    {
                        session.Transport.OnDisconnected -= clientDisconnectedHandler;
                    }

                    clientDisconnectedHandler = () =>
                    {
                        HandleClientDisconnectedAsync().Forget();
                    };

                    session.Transport.OnDisconnected += clientDisconnectedHandler;
                }

                View.UpdatePrompt($"Client connected to {host}:{connectPort} conv:{conv}");
            }
            catch (Exception ex)
            {
                View.UpdatePrompt($"Client connect failed: {ex.Message}");
                LogSwitch.Error(ex.ToString());
            }
        }

        private async MTask DisconnectClientAsync()
        {
            if (!clientConnected)
            {
                View.UpdatePrompt("Client not connected.");
                return;
            }

            try
            {
                await net.SendAsync(ClientSessionId, new DisconnectNotice
                {
                    IsServerShutdown = false,
                    Reason = "ClientDisconnect"
                });
            }
            catch (Exception ex)
            {
                LogSwitch.Warning($"Client disconnect notice failed: {ex.Message}");
            }
            finally
            {
                net.DisconnectSession(ClientSessionId);
                clientConnected = false;
                localJoined = false;
                View.UpdatePrompt("Client disconnected by user.");
            }
        }

        private async MTask StopServerAsync()
        {
            if (net == null)
            {
                View.UpdatePrompt("NetworkService missing.");
                return;
            }

            if (!serverRunning)
            {
                View.UpdatePrompt("Server is not running.");
                return;
            }

            try
            {
                var serverSessions = net.GetServerSessionsSnapshot();
                for (int i = 0; i < serverSessions.Count; i++)
                {
                    var session = serverSessions[i];
                    await net.SendAsync(session.SessionId, new DisconnectNotice
                    {
                        IsServerShutdown = true,
                        Reason = "ServerStopping"
                    });
                }
            }
            catch (Exception ex)
            {
                LogSwitch.Warning($"Server stop notice failed: {ex.Message}");
            }

            await MTask.Delay(200);
            net.StopKcpServer();
            serverRunning = false;
            View.UpdatePrompt("Server stopped.");
        }

        /// <summary>
        /// 将传输层断开通知切回 Unity 主线程并更新窗口状态。
        /// </summary>
        /// <returns>断开状态更新任务。</returns>
        private async MTask HandleClientDisconnectedAsync()
        {
            await MTask.SwitchTo(MTaskExecutors.Unity);
            if (!clientConnected)
            {
                return;
            }

            clientConnected = false;
            localJoined = false;
            View.UpdatePrompt("Client disconnected.");
        }

        private void SendNormal()
        {
            SendNormalAsync().Forget();
        }

        private void SendRpc()
        {
            SendRpcAsync().Forget();
        }

        private async MTask SendNormalAsync()
        {
            var currentSession = net?.GetSession(ClientSessionId);
            if (clientConnected && (currentSession == null || !currentSession.IsConnected))
            {
                clientConnected = false;
            }

            if (clientConnected)
            {
                var session = net.GetSession(ClientSessionId);
                if (session == null || !session.IsConnected)
                {
                    clientConnected = false;
                    View.UpdatePrompt("Session disconnected. Reconnect first.");
                    return;
                }
            }

            if (!clientConnected)
            {
                View.UpdatePrompt("Client not connected.");
                return;
            }

            try
            {
                string content = View.GetMessageOrDefault($"KCP test message {DateTime.Now:O}");
                await net.SendAsync(ClientSessionId, new DemoNormalMessage { Content = content });
                View.UpdatePrompt($"Client sent: {content}");
            }
            catch (Exception ex)
            {
                LogSwitch.Error($"Client send failed: {ex.Message}");
                View.UpdatePrompt($"Client send failed: {ex.Message}");
            }
        }

        private async MTask SendRpcAsync()
        {
            if (clientConnected)
            {
                var session = net.GetSession(ClientSessionId);
                if (session == null || !session.IsConnected)
                {
                    clientConnected = false;
                    View.UpdatePrompt("Session disconnected. Reconnect first.");
                    return;
                }
            }

            if (!clientConnected)
            {
                View.UpdatePrompt("Client not connected.");
                return;
            }

            try
            {
                string payload = View.GetMessageOrDefault($"RPC test {DateTime.Now:O}");
                var req = new DemoRpcRequest { Payload = payload };
                DemoRpcResponse resp = await net.CallAsync<DemoRpcRequest, DemoRpcResponse>(ClientSessionId, req);
                string msg = $"RPC response code:{resp.Code} msg:{resp.Msg} echo:{resp.Echo}";
                View.UpdatePrompt(msg);
            }
            catch (Exception ex)
            {
                View.UpdatePrompt($"RPC failed: {ex.Message}");
                LogSwitch.Error(ex.ToString());
            }
        }

        private void HandleServerSessionCreated(NetworkSession session)
        {
            View.UpdatePrompt($"Server session created: {session.SessionId}");
        }

        private void HandleServerSessionClosed(string sessionId)
        {
            View.UpdatePrompt($"Server session closed: {sessionId}");
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
