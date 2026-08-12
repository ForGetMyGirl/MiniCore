using System;
using System.Collections.Generic;
using MiniCore.Eventing;
using MiniCore.Threading;
using MiniCore.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MiniCore.UI
{

    /// <summary>
    /// 具有与 Presenter 相同生命周期但允许显式状态绑定的 ViewModel 基类。
    /// </summary>
    /// <typeparam name="TView">ViewModel 对应的 View 类型。</typeparam>
    public abstract class AUIWindowViewModel<TView> : AUIWindowPresenter<TView> where TView : AUIWindowView
    {
    }
}
