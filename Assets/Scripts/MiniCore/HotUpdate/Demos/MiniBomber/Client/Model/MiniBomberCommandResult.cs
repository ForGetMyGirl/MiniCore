namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// MiniBomber 客户端业务命令的协议无关结果。
    /// </summary>
    public readonly struct MiniBomberCommandResult
    {
        #region Public 公共成员

        /// <summary>
        /// 获取业务错误码；零表示成功。
        /// </summary>
        public int Code { get; }

        /// <summary>
        /// 获取可展示的业务结果说明。
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// 判断业务命令是否成功。
        /// </summary>
        public bool IsSuccess => Code == 0;

        /// <summary>
        /// 创建协议无关的业务命令结果。
        /// </summary>
        /// <param name="code">业务错误码。</param>
        /// <param name="message">业务结果说明。</param>
        public MiniBomberCommandResult(int code, string message)
        {
            Code = code;
            Message = message ?? string.Empty;
        }

        #endregion
    }
}
