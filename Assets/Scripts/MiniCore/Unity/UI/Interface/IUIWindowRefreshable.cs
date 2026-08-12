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
    /// 可在重复打开时原地接收新参数并刷新显示的窗口逻辑。
    /// </summary>
    public interface IUIWindowRefreshable
    {
        /// <summary>
        /// 使用新的强类型打开参数刷新当前活动窗口。
        /// </summary>
        /// <param name="arguments">新的窗口打开参数。</param>
        /// <returns>刷新完成任务。</returns>
        MTask RefreshAsync(object arguments);
    }
}
