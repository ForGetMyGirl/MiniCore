namespace MiniCore.UI
{
    /// <summary>
    /// 定义 WindowSession 唯一允许的生命周期状态迁移。
    /// </summary>
    public static class UIWindowStateMachine
    {
        #region Public 公共成员

        /// <summary>
        /// 判断窗口是否允许从当前状态进入目标状态。
        /// </summary>
        /// <param name="current">当前状态。</param>
        /// <param name="next">目标状态。</param>
        /// <returns>迁移符合固定生命周期时返回 true。</returns>
        public static bool CanTransition(UIWindowState current, UIWindowState next)
        {
            if (next == UIWindowState.Failed)
            {
                return current != UIWindowState.Cached && current != UIWindowState.Destroyed && current != UIWindowState.Failed;
            }

            switch (current)
            {
                case UIWindowState.None:
                    return next == UIWindowState.Loading;
                case UIWindowState.Loading:
                    return next == UIWindowState.Staging || next == UIWindowState.Closing;
                case UIWindowState.Staging:
                    return next == UIWindowState.Opening || next == UIWindowState.Closing;
                case UIWindowState.Opening:
                    return next == UIWindowState.Active || next == UIWindowState.Closing;
                case UIWindowState.Active:
                    return next == UIWindowState.Closing;
                case UIWindowState.Closing:
                    return next == UIWindowState.Cached || next == UIWindowState.Destroyed;
                default:
                    return false;
            }
        }

        #endregion
    }
}
