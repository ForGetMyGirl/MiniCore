using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MiniCore.Deploy.Desktop.ViewModels;

/// <summary>
/// 为桌面配置和执行状态提供最小属性变更通知。
/// </summary>
public abstract class ObservableObject : INotifyPropertyChanged
{
    #region Public 公共成员

    /// <summary>
    /// 属性值变化事件。
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    #endregion

    #region Protected 受保护成员

    /// <summary>
    /// 更新字段并在值变化时通知界面。
    /// </summary>
    /// <typeparam name="T">字段类型。</typeparam>
    /// <param name="field">字段引用。</param>
    /// <param name="value">新值。</param>
    /// <param name="propertyName">属性名。</param>
    /// <returns>值发生变化时返回 true。</returns>
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }

    /// <summary>
    /// 主动通知一个计算属性已经变化。
    /// </summary>
    /// <param name="propertyName">属性名。</param>
    protected void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion
}
