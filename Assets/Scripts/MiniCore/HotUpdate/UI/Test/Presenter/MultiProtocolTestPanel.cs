using Cysharp.Threading.Tasks;
using MiniCore;
using MiniCore.Core;
using MiniCore.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

namespace MiniCore.HotUpdate
{
    public class MultiProtocolTestPanel : MonoBehaviour
    {
        private const float DesignWidth = 1920f;
        private const float DesignHeight = 1080f;
        private const string TcpClientSessionId = "tcp-client";
        private const string KcpClientSessionId = "kcp-client";
        private const string UdpClientSessionId = "udp-client";
        private const int MaxLogs = 300;

        private readonly List<string> logs = new List<string>();
        private readonly ConcurrentQueue<string> pendingLogs = new ConcurrentQueue<string>();

        private NetworkMessageComponent net;

        private string host = "127.0.0.1";
        private string message = "hello";
        private int tcpPort = 21001;
        private int kcpPort = 21002;
        private int udpPort = 21003;
        private uint kcpConv = 1001;

        private bool tcpServerRunning;
        private bool kcpServerRunning;
        private bool udpServerRunning;

        private bool tcpClientConnected;
        private bool kcpClientConnected;
        private bool udpClientConnected;

        private Vector2 logScroll;
        private GUIStyle titleStyle;
        private GUIStyle sectionStyle;
        private GUIStyle labelStyle;
        private GUIStyle logStyle;
        private GUIStyle buttonStyle;
        private GUIStyle textFieldStyle;

        private void Awake()
        {
            net = Global.Com.Get<NetworkMessageComponent>(this);
            if (net != null)
            {
                net.OnServerSessionCreated += HandleServerSessionCreated;
                net.OnServerSessionClosed += HandleServerSessionClosed;
            }

            EventCenter.AddListener<string>(HotEvent.KcpTestMessage, OnNetworkMessage);
            EventCenter.AddListener<string>(GameEvent.LogInfo, OnInfoMessage);
            EventCenter.AddListener<string>(GameEvent.LogWarning, OnWarningMessage);

            AddLog("Multi-protocol test panel ready.");
        }

        private void OnDestroy()
        {
            if (net != null)
            {
                net.OnServerSessionCreated -= HandleServerSessionCreated;
                net.OnServerSessionClosed -= HandleServerSessionClosed;
            }

            EventCenter.RemoveListener<string>(HotEvent.KcpTestMessage, OnNetworkMessage);
            EventCenter.RemoveListener<string>(GameEvent.LogInfo, OnInfoMessage);
            EventCenter.RemoveListener<string>(GameEvent.LogWarning, OnWarningMessage);
            Global.Com.ReleaseAll(this);
        }

        private void OnGUI()
        {
            EnsureStyles();

            float scale = Mathf.Min(Screen.width / DesignWidth, Screen.height / DesignHeight);
            float scaledWidth = DesignWidth * scale;
            float scaledHeight = DesignHeight * scale;
            float offsetX = (Screen.width - scaledWidth) * 0.5f;
            float offsetY = (Screen.height - scaledHeight) * 0.5f;

            var oldMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(new Vector3(offsetX, offsetY, 0f), Quaternion.identity, new Vector3(scale, scale, 1f));

            GUILayout.BeginArea(new Rect(16, 16, DesignWidth - 32, DesignHeight - 32), GUI.skin.box);
            GUILayout.Label("Multi Protocol Test (TCP/KCP/UDP)", titleStyle);

            DrawInputs();

            GUILayout.Space(16);
            GUILayout.BeginHorizontal();
            DrawServerBlock();
            GUILayout.Space(16);
            DrawClientBlock();
            GUILayout.EndHorizontal();

            GUILayout.Space(16);
            GUILayout.Label("Logs", sectionStyle);
            string[] logSnapshot = logs.ToArray();
            logScroll = GUILayout.BeginScrollView(logScroll, GUILayout.Height(500));
            for (int i = 0; i < logSnapshot.Length; i++)
            {
                GUILayout.Label(logSnapshot[i], logStyle);
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
            GUI.matrix = oldMatrix;
        }

        private void Update()
        {
            FlushPendingLogs();
        }

        private void DrawInputs()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Host", labelStyle, GUILayout.Width(80));
            host = GUILayout.TextField(host, textFieldStyle, GUILayout.Width(220), GUILayout.Height(40));
            GUILayout.Label("TCP Port", labelStyle, GUILayout.Width(110));
            tcpPort = ParseIntField(tcpPort, GUILayout.Width(110), GUILayout.Height(40));
            GUILayout.Label("KCP Port", labelStyle, GUILayout.Width(110));
            kcpPort = ParseIntField(kcpPort, GUILayout.Width(110), GUILayout.Height(40));
            GUILayout.Label("UDP Port", labelStyle, GUILayout.Width(110));
            udpPort = ParseIntField(udpPort, GUILayout.Width(110), GUILayout.Height(40));
            GUILayout.Label("KCP Conv", labelStyle, GUILayout.Width(110));
            kcpConv = (uint)Math.Max(1, ParseIntField((int)kcpConv, GUILayout.Width(140), GUILayout.Height(40)));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Message", labelStyle, GUILayout.Width(100));
            message = GUILayout.TextField(message, textFieldStyle, GUILayout.Width(980), GUILayout.Height(40));
            if (GUILayout.Button("Clear Logs", buttonStyle, GUILayout.Width(180), GUILayout.Height(46)))
            {
                ClearLogs();
            }
            GUILayout.EndHorizontal();
        }

        private void DrawServerBlock()
        {
            GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(920), GUILayout.Height(300));
            GUILayout.Label("Servers", sectionStyle);

            DrawServerRow(
                "TCP",
                tcpServerRunning,
                () => StartTcpServerAsync().Forget(),
                () => StopTcpServer());

            DrawServerRow(
                "KCP",
                kcpServerRunning,
                () => StartKcpServerAsync().Forget(),
                () => StopKcpServer());

            DrawServerRow(
                "UDP",
                udpServerRunning,
                () => StartUdpServerAsync().Forget(),
                () => StopUdpServer());

            GUILayout.EndVertical();
        }

        private void DrawClientBlock()
        {
            GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(920), GUILayout.Height(300));
            GUILayout.Label("Clients", sectionStyle);

            DrawClientRow(
                "TCP",
                tcpClientConnected,
                () => ConnectTcpAsync().Forget(),
                () => DisconnectClient(TcpClientSessionId, ref tcpClientConnected),
                () => SendNormalAsync(TcpClientSessionId, "TCP").Forget(),
                () => SendRpcAsync(TcpClientSessionId, "TCP").Forget());

            DrawClientRow(
                "KCP",
                kcpClientConnected,
                () => ConnectKcpAsync().Forget(),
                () => DisconnectClient(KcpClientSessionId, ref kcpClientConnected),
                () => SendNormalAsync(KcpClientSessionId, "KCP").Forget(),
                () => SendRpcAsync(KcpClientSessionId, "KCP").Forget());

            DrawClientRow(
                "UDP",
                udpClientConnected,
                () => ConnectUdpAsync().Forget(),
                () => DisconnectClient(UdpClientSessionId, ref udpClientConnected),
                () => SendNormalAsync(UdpClientSessionId, "UDP").Forget(),
                () => SendRpcAsync(UdpClientSessionId, "UDP").Forget());

            GUILayout.EndVertical();
        }

        private void DrawServerRow(string protocol, bool running, Action start, Action stop)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{protocol} Server: {(running ? "Running" : "Stopped")}", labelStyle, GUILayout.Width(360), GUILayout.Height(44));
            if (GUILayout.Button($"Start {protocol}", buttonStyle, GUILayout.Width(180), GUILayout.Height(44)))
            {
                start?.Invoke();
            }
            if (GUILayout.Button($"Stop {protocol}", buttonStyle, GUILayout.Width(180), GUILayout.Height(44)))
            {
                stop?.Invoke();
            }
            GUILayout.EndHorizontal();
        }

        private void DrawClientRow(string protocol, bool connected, Action connect, Action disconnect, Action sendNormal, Action sendRpc)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{protocol} Client: {(connected ? "Connected" : "Disconnected")}", labelStyle, GUILayout.Width(320), GUILayout.Height(44));
            if (GUILayout.Button($"Connect {protocol}", buttonStyle, GUILayout.Width(150), GUILayout.Height(44)))
            {
                connect?.Invoke();
            }
            if (GUILayout.Button($"Disconnect {protocol}", buttonStyle, GUILayout.Width(180), GUILayout.Height(44)))
            {
                disconnect?.Invoke();
            }
            if (GUILayout.Button($"Send {protocol}", buttonStyle, GUILayout.Width(120), GUILayout.Height(44)))
            {
                sendNormal?.Invoke();
            }
            if (GUILayout.Button($"RPC {protocol}", buttonStyle, GUILayout.Width(120), GUILayout.Height(44)))
            {
                sendRpc?.Invoke();
            }
            GUILayout.EndHorizontal();
        }

        private int ParseIntField(int current, params GUILayoutOption[] options)
        {
            string text = GUILayout.TextField(current.ToString(), textFieldStyle, options);
            if (int.TryParse(text, out int parsed))
            {
                return parsed;
            }

            return current;
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 36,
                fontStyle = FontStyle.Bold
            };

            sectionStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 28,
                fontStyle = FontStyle.Bold
            };

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24
            };

            logStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20
            };

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 22
            };

            textFieldStyle = new GUIStyle(GUI.skin.textField)
            {
                fontSize = 22
            };
        }

        private async UniTaskVoid StartTcpServerAsync()
        {
            if (net == null || tcpServerRunning)
            {
                return;
            }

            try
            {
                await net.StartTcpServerAsync("0.0.0.0", tcpPort);
                tcpServerRunning = true;
                AddLog($"TCP server started on {tcpPort}");
            }
            catch (Exception ex)
            {
                AddLog($"Start TCP server failed: {ex.Message}");
            }
        }

        private async UniTaskVoid StartKcpServerAsync()
        {
            if (net == null || kcpServerRunning)
            {
                return;
            }

            try
            {
                await net.StartKcpServerAsync("0.0.0.0", kcpPort, new KcpServerConfig { Interval = 10, SessionTimeoutMs = 30000 });
                kcpServerRunning = true;
                AddLog($"KCP server started on {kcpPort}");
            }
            catch (Exception ex)
            {
                AddLog($"Start KCP server failed: {ex.Message}");
            }
        }

        private async UniTaskVoid StartUdpServerAsync()
        {
            if (net == null || udpServerRunning)
            {
                return;
            }

            try
            {
                await net.StartUdpServerAsync("0.0.0.0", udpPort, new UdpServerConfig());
                udpServerRunning = true;
                AddLog($"UDP server started on {udpPort}");
            }
            catch (Exception ex)
            {
                AddLog($"Start UDP server failed: {ex.Message}");
            }
        }

        private void StopTcpServer()
        {
            if (!tcpServerRunning || net == null)
            {
                return;
            }

            net.StopTcpServer();
            tcpServerRunning = false;
            AddLog("TCP server stopped");
        }

        private void StopKcpServer()
        {
            if (!kcpServerRunning || net == null)
            {
                return;
            }

            net.StopKcpServer();
            kcpServerRunning = false;
            AddLog("KCP server stopped");
        }

        private void StopUdpServer()
        {
            if (!udpServerRunning || net == null)
            {
                return;
            }

            net.StopUdpServer();
            udpServerRunning = false;
            AddLog("UDP server stopped");
        }

        private async UniTaskVoid ConnectTcpAsync()
        {
            if (net == null)
            {
                return;
            }

            AddLog($"TCP client connecting to {host}:{tcpPort}...");
            try
            {
                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(8));
                bool ok = await net.ConnectTcpSessionAsync(TcpClientSessionId, host, tcpPort, TimeSpan.FromSeconds(5), cts.Token);
                tcpClientConnected = ok;
                AddLog(ok ? "TCP client connected" : "TCP client connect failed");
            }
            catch (Exception ex)
            {
                AddLog($"TCP connect exception: {ex.Message}");
            }
        }

        private async UniTaskVoid ConnectKcpAsync()
        {
            if (net == null)
            {
                return;
            }

            AddLog($"KCP client connecting to {host}:{kcpPort} conv:{kcpConv}...");
            try
            {
                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(8));
                bool ok = await net.ConnectKcpSessionAsync(KcpClientSessionId, host, kcpPort, kcpConv, TimeSpan.FromSeconds(5), null, cts.Token);
                kcpClientConnected = ok;
                AddLog(ok ? "KCP client connected" : "KCP client connect failed");
            }
            catch (Exception ex)
            {
                AddLog($"KCP connect exception: {ex.Message}");
            }
        }

        private async UniTaskVoid ConnectUdpAsync()
        {
            if (net == null)
            {
                return;
            }

            AddLog($"UDP client connecting to {host}:{udpPort}...");
            try
            {
                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(8));
                bool ok = await net.ConnectUdpSessionAsync(UdpClientSessionId, host, udpPort, TimeSpan.FromSeconds(5), cts.Token);
                udpClientConnected = ok;
                AddLog(ok ? "UDP client connected" : "UDP client connect failed (probe timeout)");
            }
            catch (Exception ex)
            {
                AddLog($"UDP connect exception: {ex.Message}");
            }
        }

        private void DisconnectClient(string sessionId, ref bool connected)
        {
            if (!connected || net == null)
            {
                return;
            }

            net.DisconnectSession(sessionId);
            connected = false;
            AddLog($"{sessionId} disconnected");
        }

        private async UniTaskVoid SendNormalAsync(string sessionId, string protocol)
        {
            if (net == null)
            {
                return;
            }

            if (!TryEnsureConnected(sessionId, protocol, out _))
            {
                return;
            }

            try
            {
                await net.SendAsync(sessionId, new DemoNormalMessage { Content = $"[{protocol}] {message}" });
                AddLog($"{protocol} normal message sent");
            }
            catch (Exception ex)
            {
                AddLog($"{protocol} send failed: {ex.Message}");
            }
        }

        private async UniTaskVoid SendRpcAsync(string sessionId, string protocol)
        {
            if (net == null)
            {
                return;
            }

            if (!TryEnsureConnected(sessionId, protocol, out _))
            {
                return;
            }

            try
            {
                var req = new DemoRpcRequest { Payload = $"[{protocol}] {message}" };
                var resp = await net.CallAsync<DemoRpcRequest, DemoRpcResponse>(sessionId, req);
                AddLog($"{protocol} rpc response code:{resp.ErrorCode} msg:{resp.Message} echo:{resp.Echo}");
            }
            catch (Exception ex)
            {
                AddLog($"{protocol} rpc failed: {ex.Message}");
            }
        }

        private void HandleServerSessionCreated(NetworkSession session)
        {
            AddLog($"Server session created: {session.SessionId}");
        }

        private void HandleServerSessionClosed(string sessionId)
        {
            AddLog($"Server session closed: {sessionId}");
        }

        private void OnNetworkMessage(string messageText)
        {
            AddLog($"[Server Msg] {messageText}");
        }

        private void OnInfoMessage(string messageText)
        {
            if (string.IsNullOrEmpty(messageText))
            {
                return;
            }
            AddLog($"[Info] {messageText}");
        }

        private void OnWarningMessage(string messageText)
        {
            if (string.IsNullOrEmpty(messageText))
            {
                return;
            }

            AddLog($"[Warn] {messageText}");
        }

        private void AddLog(string text)
        {
            string line = $"[{DateTime.Now:HH:mm:ss.fff}] {text}";
            pendingLogs.Enqueue(line);
        }

        private void FlushPendingLogs()
        {
            while (pendingLogs.TryDequeue(out string line))
            {
                logs.Add(line);
                if (logs.Count > MaxLogs)
                {
                    logs.RemoveAt(0);
                }
            }
        }

        private void ClearLogs()
        {
            logs.Clear();
            while (pendingLogs.TryDequeue(out _))
            {
            }
        }

        private bool TryEnsureConnected(string sessionId, string protocol, out NetworkSession session)
        {
            session = net.GetSession(sessionId);
            if (session == null || !session.IsConnected)
            {
                AddLog($"{protocol} 未连接，无法发送消息。请先 Connect。");
                return false;
            }

            return true;
        }
    }
}
