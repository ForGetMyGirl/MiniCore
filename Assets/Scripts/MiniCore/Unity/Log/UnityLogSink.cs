using MiniCore.Model;
using UnityEngine;

namespace MiniCore.Unity
{
    /// <summary>
    /// 将 Runtime 日志输出到 Unity Console。
    /// </summary>
    public sealed class UnityLogSink : ILogSink
    {
        /// <summary>
        /// 输出普通信息日志。
        /// </summary>
        /// <param name="message">日志正文。</param>
        public void Info(string message)
        {
            Debug.Log(message);
        }

        /// <summary>
        /// 输出警告日志。
        /// </summary>
        /// <param name="message">日志正文。</param>
        public void Warning(string message)
        {
            Debug.LogWarning(message);
        }

        /// <summary>
        /// 输出错误日志。
        /// </summary>
        /// <param name="message">日志正文。</param>
        public void Error(string message)
        {
            Debug.LogError(message);
        }
    }
}
