using System;
using Cysharp.Threading.Tasks;
using MiniCore;
using MiniCore.Core;
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
        private Action clientDisconnectedHandler;

        protected override void OnBind()
        {
            net = Global.GetService<INetworkService>(this);
            EventCenter.AddListener<string>(HotEvent.KcpTestMessage, OnKcpTestMessage);
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
            EventCenter.RemoveListener<string>(HotEvent.KcpTestMessage, OnKcpTestMessage);

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

        private async UniTaskVoid StartServerAsync()
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

        private async UniTaskVoid ConnectClientAsync()
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
                        UniTask.Void(async () =>
                        {
                            await UniTask.SwitchToMainThread();
                            if (!clientConnected)
                            {
                                return;
                            }

                            clientConnected = false;
                            localJoined = false;
                            View.UpdatePrompt("Client disconnected.");
                        });
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

        private async UniTaskVoid DisconnectClientAsync()
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

        private async UniTaskVoid StopServerAsync()
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

            await UniTask.Delay(200);
            net.StopKcpServer();
            serverRunning = false;
            View.UpdatePrompt("Server stopped.");
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

        private async UniTaskVoid SendRpcAsync()
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
