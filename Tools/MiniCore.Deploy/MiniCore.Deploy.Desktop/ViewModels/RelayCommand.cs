using System.Windows.Input;

namespace MiniCore.Deploy.Desktop.ViewModels;

/// <summary>
/// 将同步界面操作包装为不依赖第三方 MVVM 库的命令。
/// </summary>
public sealed class RelayCommand : ICommand
{
    #region Private 私有成员

    private readonly Action execute; // 实际操作。
    private readonly Func<bool>? canExecute; // 可选可用性判断。

    #endregion

    #region Public 公共成员

    /// <summary>
    /// 命令可用性变化事件。
    /// </summary>
    public event EventHandler? CanExecuteChanged;

    /// <summary>
    /// 创建同步命令。
    /// </summary>
    /// <param name="execute">实际操作。</param>
    /// <param name="canExecute">可选可用性判断。</param>
    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        this.execute = execute ?? throw new ArgumentNullException(nameof(execute));
        this.canExecute = canExecute;
    }

    /// <summary>
    /// 判断命令当前是否可以执行。
    /// </summary>
    /// <param name="parameter">未使用的绑定参数。</param>
    /// <returns>允许执行时返回 true。</returns>
    public bool CanExecute(object? parameter)
    {
        return canExecute?.Invoke() ?? true;
    }

    /// <summary>
    /// 执行命令。
    /// </summary>
    /// <param name="parameter">未使用的绑定参数。</param>
    public void Execute(object? parameter)
    {
        execute();
    }

    /// <summary>
    /// 通知界面重新读取命令可用状态。
    /// </summary>
    public void NotifyCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    #endregion
}
