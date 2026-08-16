namespace MiniCore.UI
{

    /// <summary>
    /// 不暴露结果泛型的窗口结果通道。
    /// </summary>
    internal interface IUIWindowResultChannel
    {
        /// <summary>
        /// 尝试提交业务结果。
        /// </summary>
        /// <param name="value">Presenter 提交的结果。</param>
        void SetResult(object value);

        /// <summary>
        /// 窗口未提交结果就关闭时结束等待。
        /// </summary>
        void CloseWithoutResult();
    }
}
