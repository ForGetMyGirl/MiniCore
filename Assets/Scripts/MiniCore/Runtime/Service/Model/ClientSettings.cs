using System;
using System.Collections.Generic;
using System.Threading;
using MiniCore.Threading;

namespace MiniCore.Service
{

    /// <summary>
    /// 客户端通用设备与偏好设置数据。
    /// </summary>
    [Serializable]
    public sealed class ClientSettings
    {
        /// <summary>
        /// 获取或设置设置数据版本。
        /// </summary>
        public int Version { get; set; } = 1;

        /// <summary>
        /// 获取或设置质量档名称。
        /// </summary>
        public string QualityLevel { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置目标帧率；零表示平台默认值。
        /// </summary>
        public int TargetFrameRate { get; set; } = 60;

        /// <summary>
        /// 获取或设置垂直同步数量。
        /// </summary>
        public int VSyncCount { get; set; }

        /// <summary>
        /// 获取或设置窗口宽度；零表示保留平台默认分辨率。
        /// </summary>
        public int ScreenWidth { get; set; }

        /// <summary>
        /// 获取或设置窗口高度；零表示保留平台默认分辨率。
        /// </summary>
        public int ScreenHeight { get; set; }

        /// <summary>
        /// 获取或设置是否使用全屏显示。
        /// </summary>
        public bool FullScreen { get; set; } = true;

        /// <summary>
        /// 获取或设置 BGM 音量。
        /// </summary>
        public float BgmVolume { get; set; } = 1f;

        /// <summary>
        /// 获取或设置音效音量。
        /// </summary>
        public float SfxVolume { get; set; } = 1f;

        /// <summary>
        /// 获取或设置 UI 音量。
        /// </summary>
        public float UiVolume { get; set; } = 1f;

        /// <summary>
        /// 获取或设置是否允许震动反馈。
        /// </summary>
        public bool VibrationEnabled { get; set; } = true;

        /// <summary>
        /// 获取或设置当前语言代码。
        /// </summary>
        public string Language { get; set; } = string.Empty;
    }
}
