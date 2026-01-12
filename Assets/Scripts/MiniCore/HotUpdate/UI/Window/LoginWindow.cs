using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MiniCore.Model;


namespace MiniCore.HotUpdate
{
public class LoginWindow : AUIBase
{
    public Button quitBtn;
    public TMP_Text titleText;

    private void Awake()
    {
        titleText.text = "Hot update success";
        quitBtn.onClick.AddListener(OnQuitBtnClick);
    }

    private void OnQuitBtnClick()
    {
        Application.Quit();
    }
}

}
