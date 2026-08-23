using System.Windows.Input;

namespace MiniCore.Deploy.Desktop.ViewModels;

/// <summary>
/// 串行执行异步界面操作并阻止重复点击。
/// </summary>
public sealed class AsyncRelayCommand : ICommand
{
    #region Private 私有成员

    private readonly Func<Task> executeAsync; // 异步操作。
    private bool isRunning; // 当前是否正在执行。

    #endregion

    #region Public 公共成员

    /// <summary>
    /// 命令可用性变化事件。
    /// </summary>
    public event EventHandler? CanExecuteChanged;

    /// <summary>
    /// 创建异步命令。
    /// </summary>
    /// <param name="executeAsync">异步操作。</param>
    public AsyncRelayCommand(Func<Task> executeAsync)
    {
        this.executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
    }

    /// <summary>
    /// 在上一次操作完成前阻止重复执行。
    /// </summary>
    /// <param name="parameter">未使用的绑定参数。</param>
    /// <returns>当前空闲时返回 true。</returns>
    public bool CanExecute(object? parameter)
    {
        return !isRunning;
    }

    /// <summary>
    /// 启动异步操作并在结束后恢复按钮。
    /// </summary>
    /// <param name="parameter">未使用的绑定参数。</param>
    public async void Execute(object? parameter)
    {
        if (isRunning)
        {
            return;
        }

        isRunning = true;
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        try
        {
            await executeAsync().ConfigureAwait(true);
        }
        finally
        {
            isRunning = false;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    #endregion
}
