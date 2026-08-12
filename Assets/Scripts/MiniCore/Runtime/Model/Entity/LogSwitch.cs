namespace MiniCore.Model
{
    /// <summary>
    /// 集中控制框架日志输出。
    /// 调用方在高频路径中应先检查开关，再创建日志时间、插值字符串或正文文本。
    /// </summary>
    public static class LogSwitch
    {
        #region Private 私有成员

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private static bool enableLog = true; // 编辑器和开发构建默认保留诊断日志。
#else
        private static bool enableLog; // 正式非开发构建默认关闭日志，避免高频路径产生输出开销。
#endif
        private static bool enablePayloadLog; // 是否输出协议正文文本。
        private static ILogSink sink; // 当前运行时日志输出端。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 是否允许输出普通、警告和错误日志。
        /// 正式非开发构建默认关闭；高频调用方应在构造日志字符串前读取该属性。
        /// </summary>
        public static bool EnableLog
        {
            get => enableLog;
            set => enableLog = value;
        }

        /// <summary>
        /// 是否允许输出协议正文日志。
        /// 仅在 <see cref="EnableLog"/> 同时开启时，网络层才会序列化正文为文本。
        /// </summary>
        public static bool EnablePayloadLog
        {
            get => enablePayloadLog;
            set => enablePayloadLog = value;
        }

        /// <summary>
        /// 注册当前进程的日志输出端；传入空值时仅保留事件广播。
        /// </summary>
        /// <param name="logSink">日志输出端。</param>
        public static void SetSink(ILogSink logSink)
        {
            sink = logSink;
        }

        /// <summary>
        /// 输出普通信息日志。
        /// 此方法保留内部开关检查，作为未在调用方预先判断时的安全兜底。
        /// </summary>
        /// <param name="message">需要输出的信息文本。</param>
        public static void Info(string message)
        {
            if (!enableLog)
            {
                return;
            }

            sink?.Info(message);
        }

        /// <summary>
        /// 输出警告日志。
        /// 此方法保留内部开关检查，异常路径无需由每个调用方重复处理。
        /// </summary>
        /// <param name="message">需要输出的警告文本。</param>
        public static void Warning(string message)
        {
            if (!enableLog)
            {
                return;
            }

            sink?.Warning(message);
        }

        /// <summary>
        /// 输出错误日志。
        /// 此方法保留内部开关检查，异常路径无需由每个调用方重复处理。
        /// </summary>
        /// <param name="message">需要输出的错误文本。</param>
        public static void Error(string message)
        {
            if (!enableLog)
            {
                return;
            }

            sink?.Error(message);
        }

        #endregion
    }
}
