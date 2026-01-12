using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MiniCore.Model;

namespace MiniCore.HotUpdate
{
    [UIWindow(typeof(KcpTestWindowPresenter))]
    public class KcpTestWindowView : AUIBase
    {
        public Button startServerBtn;
        public Button stopServerBtn;
        public Button connectClientBtn;
        public Button disconnectClientBtn;
        public Button sendRpcBtn;
        public Button sendNormalBtn;
        public TMP_Text promptText;
        public TMP_InputField hostInput;
        public TMP_InputField portInput;
        public TMP_InputField convInput;
        public TMP_InputField messageInput;

        public event Action OnStartServerClicked;
        public event Action OnStopServerClicked;
        public event Action OnConnectClientClicked;
        public event Action OnDisconnectClientClicked;
        public event Action OnSendRpcClicked;
        public event Action OnSendNormalClicked;

        private void Awake()
        {
            startServerBtn.onClick.AddListener(() => OnStartServerClicked?.Invoke());
            stopServerBtn.onClick.AddListener(() => OnStopServerClicked?.Invoke());
            connectClientBtn.onClick.AddListener(() => OnConnectClientClicked?.Invoke());
            disconnectClientBtn.onClick.AddListener(() => OnDisconnectClientClicked?.Invoke());
            sendRpcBtn.onClick.AddListener(() => OnSendRpcClicked?.Invoke());
            sendNormalBtn.onClick.AddListener(() => OnSendNormalClicked?.Invoke());
        }

        public void UpdatePrompt(string prompt)
        {
            if (promptText != null)
            {
                promptText.text += $"{prompt}\n";
            }
        }

        public string GetHostOrDefault(string fallback)
        {
            if (hostInput == null)
            {
                return fallback;
            }
            string text = hostInput.text;
            return string.IsNullOrWhiteSpace(text) ? fallback : text.Trim();
        }

        public bool TryGetPort(int fallback, out int port)
        {
            port = fallback;
            if (portInput == null)
            {
                return true;
            }
            string text = portInput.text;
            if (string.IsNullOrWhiteSpace(text))
            {
                return true;
            }
            return int.TryParse(text.Trim(), out port);
        }

        public bool TryGetConv(uint fallback, out uint conv)
        {
            conv = fallback;
            if (convInput == null)
            {
                return true;
            }
            string text = convInput.text;
            if (string.IsNullOrWhiteSpace(text))
            {
                return true;
            }
            return uint.TryParse(text.Trim(), out conv);
        }

        public string GetMessageOrDefault(string fallback)
        {
            if (messageInput == null)
            {
                return fallback;
            }
            string text = messageInput.text;
            return string.IsNullOrWhiteSpace(text) ? fallback : text.Trim();
        }

        public override UniTask OpenAsync()
        {
            gameObject.SetActive(true);
            return UniTask.CompletedTask;
        }

        public override UniTask CloseAsync()
        {
            return UniTask.CompletedTask;
        }
    }
}
