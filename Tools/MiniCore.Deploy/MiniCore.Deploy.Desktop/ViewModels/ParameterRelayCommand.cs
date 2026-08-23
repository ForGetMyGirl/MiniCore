using System.Windows.Input;

namespace MiniCore.Deploy.Desktop.ViewModels;

/// <summary>
/// 将带一个界面参数的同步操作包装为命令。
/// </summary>
/// <typeparam name="T">命令参数类型。</typeparam>
public sealed class ParameterRelayCommand<T> : ICommand
    where T : class
{
    #region Private 私有成员

    private readonly Action<T> execute; // 实际操作。
    private readonly Func<T, bool>? canExecute; // 可选可用性判断。

    #endregion

    #region Public 公共成员

    /// <summary>
    /// 命令可用性变化事件。
    /// </summary>
    public event EventHandler? CanExecuteChanged;

    /// <summary>
    /// 创建带参数的同步命令。
    /// </summary>
    /// <param name="execute">实际操作。</param>
    /// <param name="canExecute">可选可用性判断。</param>
    public ParameterRelayCommand(Action<T> execute, Func<T, bool>? canExecute = null)
    {
        this.execute = execute ?? throw new ArgumentNullException(nameof(execute));
        this.canExecute = canExecute;
    }

    /// <summary>
    /// 判断指定参数当前是否允许执行。
    /// </summary>
    /// <param name="parameter">命令参数。</param>
    /// <returns>参数有效且操作允许时返回 true。</returns>
    public bool CanExecute(object? parameter)
    {
        return parameter is T value && (canExecute?.Invoke(value) ?? true);
    }

    /// <summary>
    /// 使用指定参数执行同步操作。
    /// </summary>
    /// <param name="parameter">命令参数。</param>
    public void Execute(object? parameter)
    {
        if (parameter is T value)
        {
            execute(value);
        }
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
