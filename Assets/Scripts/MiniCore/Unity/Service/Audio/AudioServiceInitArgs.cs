using System;
using System.Collections.Generic;
using MiniCore.Core;
using MiniCore.Model;
using UnityEngine;
using UnityEngine.Audio;

namespace MiniCore.Service
{
    /// <summary>
    /// 音频服务的可选启动参数。
    /// </summary>
    public sealed class AudioServiceInitArgs : ComponentInitArgs
    {
        /// <summary>
        /// 获取或设置项目的 AudioMixer；未提供时使用 AudioSource 线性音量回退。
        /// </summary>
        public AudioMixer Mixer { get; set; }

        /// <summary>
        /// 获取或设置同时保留的音效 AudioSource 数量。
        /// </summary>
        public int PoolSize { get; set; } = 12;
    }
}
