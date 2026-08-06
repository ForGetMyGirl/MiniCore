using System;
using MiniCore.Service;
using MiniCore.Threading;

namespace MiniCore.UI
{
    /// <summary>
    /// UI 窗口使用的渲染空间。
    /// </summary>
    public enum UIRenderSpace
    {
        ScreenSpaceOverlay = 0,
        ScreenSpaceCamera = 1
    }

    /// <summary>
    /// ApplicationUIRoot 中的逻辑显示层。
    /// </summary>
    public enum UILayer
    {
        Background = 0,
        Hud = 1,
        Screen = 2,
        Window = 3,
        Popup = 4,
        Toast = 5,
        Guide = 6,
        Drag = 7,
        Transition = 8,
        System = 9,
        Debug = 10,
        Tooltip = 11
    }

    /// <summary>
    /// 窗口创建向导使用的业务模板。
    /// </summary>
    public enum UIWindowTemplate
    {
        Screen = 0,
        FloatingWindow = 1,
        ModalPopup = 2,
        Toast = 3,
        Hud = 4,
        Guide = 5,
        System = 6,
        Custom = 7
    }

    /// <summary>
    /// 同一窗口定义允许存在的实例形式。
    /// </summary>
    public enum UIInstancePolicy
    {
        Singleton,
        SingletonPerKey,
        Multiple,
        Queue,
        Replace
    }

    /// <summary>
    /// 重复打开同一逻辑窗口时的处理方式。
    /// </summary>
    public enum UIDuplicateOpenPolicy
    {
        Focus,
        Refresh,
        Ignore,
        Reject
    }

    /// <summary>
    /// 窗口关闭后的 View 处理方式。
    /// </summary>
    public enum UICachePolicy
    {
        DestroyOnClose,
        CacheOnClose,
        Resident
    }

    /// <summary>
    /// 窗口内容相对设备安全区域的适配方式。
    /// </summary>
    public enum UISafeAreaPolicy
    {
        Inherit,
        ConstrainContent,
        ConstrainWindow,
        Ignore,
        Custom
    }

    /// <summary>
    /// 窗口当前所处的生命周期状态。
    /// </summary>
    public enum UIWindowState
    {
        None,
        Loading,
        Staging,
        Opening,
        Active,
        Closing,
        Cached,
        Destroyed,
        Failed
    }

    /// <summary>
    /// 窗口动画当前执行的阶段。
    /// </summary>
    public enum UITransitionPhase
    {
        Enter,
        Exit
    }

    /// <summary>
    /// 动画被新生命周期操作打断时的收敛方式。
    /// </summary>
    public enum UITransitionInterruptMode
    {
        KeepCurrent,
        CompleteCurrent,
        RestoreOriginal
    }

    /// <summary>
    /// 稳定标识一个窗口定义的 128 位身份。
    /// </summary>
    [Serializable]
    public struct UIWindowId : IEquatable<UIWindowId>
    {
        #region Private 私有成员

        private ulong high; // 身份高 64 位。
        private ulong low; // 身份低 64 位。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 获取身份高 64 位。
        /// </summary>
        public ulong High => high;

        /// <summary>
        /// 获取身份低 64 位。
        /// </summary>
        public ulong Low => low;

        /// <summary>
        /// 判断当前身份是否尚未初始化。
        /// </summary>
        public bool IsEmpty => high == 0UL && low == 0UL;

        /// <summary>
        /// 使用高低 64 位创建窗口身份。
        /// </summary>
        /// <param name="highValue">身份高 64 位。</param>
        /// <param name="lowValue">身份低 64 位。</param>
        public UIWindowId(ulong highValue, ulong lowValue)
        {
            high = highValue;
            low = lowValue;
        }

        /// <summary>
        /// 将 Guid 转换为窗口身份。
        /// </summary>
        /// <param name="value">稳定 Guid。</param>
        /// <returns>对应的 128 位窗口身份。</returns>
        public static UIWindowId FromGuid(Guid value)
        {
            byte[] bytes = value.ToByteArray();
            return new UIWindowId(BitConverter.ToUInt64(bytes, 0), BitConverter.ToUInt64(bytes, 8));
        }

        /// <summary>
        /// 将窗口身份还原为 Guid。
        /// </summary>
        /// <returns>对应 Guid。</returns>
        public Guid ToGuid()
        {
            byte[] bytes = new byte[16];
            Array.Copy(BitConverter.GetBytes(high), 0, bytes, 0, 8);
            Array.Copy(BitConverter.GetBytes(low), 0, bytes, 8, 8);
            return new Guid(bytes);
        }

        /// <summary>
        /// 判断两个窗口身份是否相同。
        /// </summary>
        /// <param name="other">待比较身份。</param>
        /// <returns>高低位均相同时返回 true。</returns>
        public bool Equals(UIWindowId other)
        {
            return high == other.high && low == other.low;
        }

        /// <summary>
        /// 判断目标对象是否为同一窗口身份。
        /// </summary>
        /// <param name="obj">待比较对象。</param>
        /// <returns>对象表示相同身份时返回 true。</returns>
        public override bool Equals(object obj)
        {
            return obj is UIWindowId other && Equals(other);
        }

        /// <summary>
        /// 获取窗口身份哈希值。
        /// </summary>
        /// <returns>组合后的哈希值。</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return (high.GetHashCode() * 397) ^ low.GetHashCode();
            }
        }

        /// <summary>
        /// 输出用于日志和诊断的稳定身份文本。
        /// </summary>
        /// <returns>Guid 格式文本。</returns>
        public override string ToString()
        {
            return ToGuid().ToString("N");
        }

        /// <summary>
        /// 判断两个窗口身份是否相同。
        /// </summary>
        public static bool operator ==(UIWindowId left, UIWindowId right) => left.Equals(right);

        /// <summary>
        /// 判断两个窗口身份是否不同。
        /// </summary>
        public static bool operator !=(UIWindowId left, UIWindowId right) => !left.Equals(right);

        #endregion
    }

    /// <summary>
    /// 表示 SingletonPerKey 或 Multiple 窗口的业务实例键。
    /// </summary>
    [Serializable]
    public struct UIWindowInstanceKey : IEquatable<UIWindowInstanceKey>
    {
        #region Private 私有成员

        private long numericValue; // 数字业务键。
        private string textValue; // 文本业务键。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 获取空实例键。
        /// </summary>
        public static UIWindowInstanceKey Empty => default;

        /// <summary>
        /// 判断当前键是否为空。
        /// </summary>
        public bool IsEmpty => numericValue == 0L && string.IsNullOrEmpty(textValue);

        /// <summary>
        /// 使用数字创建实例键。
        /// </summary>
        /// <param name="value">非零业务数字。</param>
        public UIWindowInstanceKey(long value)
        {
            numericValue = value;
            textValue = null;
        }

        /// <summary>
        /// 使用文本创建实例键。
        /// </summary>
        /// <param name="value">非空稳定业务文本。</param>
        public UIWindowInstanceKey(string value)
        {
            numericValue = 0L;
            textValue = value;
        }

        /// <summary>
        /// 判断两个实例键是否相同。
        /// </summary>
        /// <param name="other">待比较实例键。</param>
        /// <returns>数字和文本均相同时返回 true。</returns>
        public bool Equals(UIWindowInstanceKey other)
        {
            return numericValue == other.numericValue && string.Equals(textValue, other.textValue, StringComparison.Ordinal);
        }

        /// <summary>
        /// 判断目标对象是否表示同一实例键。
        /// </summary>
        /// <param name="obj">待比较对象。</param>
        /// <returns>对象表示相同实例键时返回 true。</returns>
        public override bool Equals(object obj)
        {
            return obj is UIWindowInstanceKey other && Equals(other);
        }

        /// <summary>
        /// 获取实例键哈希值。
        /// </summary>
        /// <returns>稳定哈希值。</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return (numericValue.GetHashCode() * 397) ^ (textValue != null ? StringComparer.Ordinal.GetHashCode(textValue) : 0);
            }
        }

        /// <summary>
        /// 输出实例键诊断文本。
        /// </summary>
        /// <returns>数字或文本内容。</returns>
        public override string ToString()
        {
            return textValue ?? numericValue.ToString();
        }

        /// <summary>
        /// 判断两个实例键是否相同。
        /// </summary>
        public static bool operator ==(UIWindowInstanceKey left, UIWindowInstanceKey right) => left.Equals(right);

        /// <summary>
        /// 判断两个实例键是否不同。
        /// </summary>
        public static bool operator !=(UIWindowInstanceKey left, UIWindowInstanceKey right) => !left.Equals(right);

        #endregion
    }

    /// <summary>
    /// 唯一标识某个活动或缓存窗口实例。
    /// </summary>
    [Serializable]
    public struct UIWindowInstanceId : IEquatable<UIWindowInstanceId>
    {
        #region Public 公共成员

        /// <summary>
        /// 获取窗口定义身份。
        /// </summary>
        public UIWindowId WindowId { get; }

        /// <summary>
        /// 获取业务实例键。
        /// </summary>
        public UIWindowInstanceKey InstanceKey { get; }

        /// <summary>
        /// 获取本次实例代次。
        /// </summary>
        public uint Generation { get; }

        /// <summary>
        /// 创建窗口实例身份。
        /// </summary>
        /// <param name="windowId">窗口定义身份。</param>
        /// <param name="instanceKey">业务实例键。</param>
        /// <param name="generation">实例代次。</param>
        public UIWindowInstanceId(UIWindowId windowId, UIWindowInstanceKey instanceKey, uint generation)
        {
            WindowId = windowId;
            InstanceKey = instanceKey;
            Generation = generation;
        }

        /// <summary>
        /// 判断两个窗口实例身份是否相同。
        /// </summary>
        /// <param name="other">待比较实例身份。</param>
        /// <returns>定义、业务键和代次均相同时返回 true。</returns>
        public bool Equals(UIWindowInstanceId other)
        {
            return WindowId.Equals(other.WindowId) && InstanceKey.Equals(other.InstanceKey) && Generation == other.Generation;
        }

        /// <summary>
        /// 判断目标对象是否表示同一窗口实例。
        /// </summary>
        /// <param name="obj">待比较对象。</param>
        /// <returns>对象表示同一实例时返回 true。</returns>
        public override bool Equals(object obj)
        {
            return obj is UIWindowInstanceId other && Equals(other);
        }

        /// <summary>
        /// 获取实例身份哈希值。
        /// </summary>
        /// <returns>组合哈希值。</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = WindowId.GetHashCode();
                hash = (hash * 397) ^ InstanceKey.GetHashCode();
                return (hash * 397) ^ (int)Generation;
            }
        }

        /// <summary>
        /// 判断两个实例身份是否相同。
        /// </summary>
        public static bool operator ==(UIWindowInstanceId left, UIWindowInstanceId right) => left.Equals(right);

        /// <summary>
        /// 判断两个实例身份是否不同。
        /// </summary>
        public static bool operator !=(UIWindowInstanceId left, UIWindowInstanceId right) => !left.Equals(right);

        #endregion
    }

    /// <summary>
    /// 业务持有的不可变窗口操作句柄，不暴露具体 Unity View。
    /// 使用引用类型可避免复杂值类型跨热更新异步泛型边界时依赖 HybridCLR adjustor thunk。
    /// </summary>
    public sealed class UIWindowHandle : IEquatable<UIWindowHandle>
    {
        #region Public 公共成员

        /// <summary>
        /// 获取窗口实例身份。
        /// </summary>
        public UIWindowInstanceId InstanceId { get; }

        /// <summary>
        /// 判断句柄是否包含有效窗口定义。
        /// </summary>
        public bool IsValid => !InstanceId.WindowId.IsEmpty;

        /// <summary>
        /// 创建窗口句柄。
        /// </summary>
        /// <param name="instanceId">窗口实例身份。</param>
        public UIWindowHandle(UIWindowInstanceId instanceId)
        {
            InstanceId = instanceId;
        }

        /// <summary>
        /// 判断两个句柄是否指向同一代窗口实例。
        /// </summary>
        /// <param name="other">待比较句柄。</param>
        /// <returns>实例身份相同时返回 true。</returns>
        public bool Equals(UIWindowHandle other)
        {
            return !ReferenceEquals(other, null) && InstanceId.Equals(other.InstanceId);
        }

        /// <summary>
        /// 判断目标对象是否为相同句柄。
        /// </summary>
        /// <param name="obj">待比较对象。</param>
        /// <returns>对象为相同句柄时返回 true。</returns>
        public override bool Equals(object obj) => Equals(obj as UIWindowHandle);

        /// <summary>
        /// 获取句柄哈希值。
        /// </summary>
        /// <returns>实例身份哈希值。</returns>
        public override int GetHashCode() => InstanceId.GetHashCode();

        /// <summary>
        /// 判断两个窗口句柄是否指向同一代实例。
        /// </summary>
        /// <param name="left">左侧句柄。</param>
        /// <param name="right">右侧句柄。</param>
        /// <returns>实例身份相同时返回 true。</returns>
        public static bool operator ==(UIWindowHandle left, UIWindowHandle right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            return !ReferenceEquals(left, null) && !ReferenceEquals(right, null) && left.Equals(right);
        }

        /// <summary>
        /// 判断两个窗口句柄是否指向不同实例。
        /// </summary>
        /// <param name="left">左侧句柄。</param>
        /// <param name="right">右侧句柄。</param>
        /// <returns>实例身份不同时返回 true。</returns>
        public static bool operator !=(UIWindowHandle left, UIWindowHandle right) => !(left == right);

        #endregion
    }

    /// <summary>
    /// 标记由编辑器生成的强类型窗口路由。
    /// </summary>
    public interface IUIWindowRoute
    {
    }

    /// <summary>
    /// 标记只允许传给指定窗口路由的打开参数。
    /// </summary>
    /// <typeparam name="TRoute">目标窗口路由。</typeparam>
    public interface IUIWindowArgs<TRoute> where TRoute : IUIWindowRoute
    {
    }

    /// <summary>
    /// 为 SingletonPerKey 窗口提供稳定业务实例键。
    /// </summary>
    public interface IUIWindowKeyProvider
    {
        /// <summary>
        /// 获取当前打开参数对应的业务实例键。
        /// </summary>
        UIWindowInstanceKey InstanceKey { get; }
    }

    /// <summary>
    /// 提供强类型窗口打开、导航、预加载、关闭与聚焦能力。
    /// </summary>
    public interface IUIService : IAppService
    {
        /// <summary>
        /// 按编辑器生成的稳定路由名称打开窗口，供数据驱动流程使用。
        /// </summary>
        /// <param name="routeName">窗口 Authoring 中的稳定 RouteName。</param>
        /// <returns>活动窗口句柄。</returns>
        MTask<UIWindowHandle> OpenAsync(string routeName);

        /// <summary>
        /// 打开不带业务参数的窗口。
        /// </summary>
        /// <typeparam name="TRoute">生成的窗口路由。</typeparam>
        /// <returns>活动窗口句柄。</returns>
        MTask<UIWindowHandle> OpenAsync<TRoute>() where TRoute : IUIWindowRoute;

        /// <summary>
        /// 使用强类型参数打开窗口。
        /// </summary>
        /// <typeparam name="TRoute">生成的窗口路由。</typeparam>
        /// <param name="args">只允许用于该路由的参数。</param>
        /// <returns>活动窗口句柄。</returns>
        MTask<UIWindowHandle> OpenAsync<TRoute>(IUIWindowArgs<TRoute> args) where TRoute : IUIWindowRoute;

        /// <summary>
        /// 将目标全屏窗口导航到其导航组顶部。
        /// </summary>
        /// <typeparam name="TRoute">生成的窗口路由。</typeparam>
        /// <returns>导航完成任务。</returns>
        MTask NavigateAsync<TRoute>() where TRoute : IUIWindowRoute;

        /// <summary>
        /// 按稳定路由名称切换 Screen 导航组顶部窗口。
        /// </summary>
        /// <param name="routeName">窗口 Authoring 中的稳定 RouteName。</param>
        /// <returns>导航完成任务。</returns>
        MTask NavigateAsync(string routeName);

        /// <summary>
        /// 关闭指定导航组当前的 Screen 窗口，使应用进入没有全屏窗口的流程状态。
        /// </summary>
        /// <param name="navigationGroup">窗口 Authoring 中的导航组名称。</param>
        /// <returns>当前 Screen 关闭完成任务；导航组为空时立即完成。</returns>
        MTask CloseNavigationAsync(string navigationGroup);

        /// <summary>
        /// 预加载目标窗口资源和可配置数量的 View。
        /// </summary>
        /// <typeparam name="TRoute">生成的窗口路由。</typeparam>
        /// <param name="count">希望准备的缓存实例数。</param>
        /// <returns>预加载完成任务。</returns>
        MTask PrefetchAsync<TRoute>(int count = 1) where TRoute : IUIWindowRoute;

        /// <summary>
        /// 关闭句柄指向的当前代窗口实例。
        /// </summary>
        /// <param name="handle">待关闭句柄。</param>
        /// <returns>关闭完成任务。</returns>
        MTask CloseAsync(UIWindowHandle handle);

        /// <summary>
        /// 将句柄对应窗口移动到所在层最前方并恢复输入焦点。
        /// </summary>
        /// <param name="handle">目标窗口句柄。</param>
        /// <returns>句柄仍然有效并成功聚焦时返回 true。</returns>
        bool Focus(UIWindowHandle handle);

        /// <summary>
        /// 打开会返回业务结果的窗口并等待其关闭结果。
        /// </summary>
        /// <typeparam name="TRoute">生成的窗口路由。</typeparam>
        /// <typeparam name="TArgs">强类型打开参数。</typeparam>
        /// <typeparam name="TResult">关闭结果类型。</typeparam>
        /// <param name="args">窗口打开参数。</param>
        /// <returns>窗口提交的关闭结果。</returns>
        MTask<TResult> ShowAsync<TRoute, TArgs, TResult>(TArgs args)
            where TRoute : IUIWindowRoute
            where TArgs : IUIWindowArgs<TRoute>;
    }
}
