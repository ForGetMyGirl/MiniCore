using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MiniCore.Model;


namespace MiniCore.HotUpdate
{
[UIWindow(typeof(TestWindowPresenter))]
public class TestWindowView : AUIBase
{

    public Button testBtn;
    public TMP_Text promptText;

    public Button rpcBtn;
    public Button normalMsgBtn;
    public Button connectBtn;

    public event Action OnTestBtnClickedEvent;
    public event Action OnRpcBtnClickedEvent;
    public event Action OnNormalMsgBtnClickedEvent;
    public event Action OnConnectBtnClickedEvent;

    void Awake()
    {
        testBtn.onClick.AddListener(OnTestBtnClicked);
        rpcBtn.onClick.AddListener(OnRpcBtnClicked);
        normalMsgBtn.onClick.AddListener(OnNormalMsgBtnClicked);
        connectBtn.onClick.AddListener(OnConnectBtnClicked);
    }

    private void OnNormalMsgBtnClicked()
    {
        OnNormalMsgBtnClickedEvent?.Invoke();
    }


    private void OnRpcBtnClicked()
    {
        OnRpcBtnClickedEvent?.Invoke();
    }


    private void OnTestBtnClicked()
    {
        OnTestBtnClickedEvent?.Invoke();
    }

    private void OnConnectBtnClicked()
    {
        OnConnectBtnClickedEvent?.Invoke();
    }

    public void UpdatePrompt(string prompt)
    {
        promptText.text = prompt;
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
