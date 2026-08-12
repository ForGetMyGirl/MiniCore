namespace MiniCore.Model
{

    /// <summary>
    /// Runtime 使用的日志输出端契约，由 Unity 或服务器宿主提供实现。
    /// </summary>
    public interface ILogSink
    {
        /// <summary>
        /// 输出普通信息。
        /// </summary>
        /// <param name="message">日志正文。</param>
        void Info(string message);

        /// <summary>
        /// 输出警告信息。
        /// </summary>
        /// <param name="message">日志正文。</param>
        void Warning(string message);

        /// <summary>
        /// 输出错误信息。
        /// </summary>
        /// <param name="message">日志正文。</param>
        void Error(string message);
    }
}
