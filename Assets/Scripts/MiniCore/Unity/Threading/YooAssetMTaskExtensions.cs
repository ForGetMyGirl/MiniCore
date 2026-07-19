using System;
using System.Collections.Concurrent;
using MiniCore.Threading;
using YooAsset;

namespace MiniCore.Unity
{
    /// <summary>
    /// YooAsset 原生异步操作到 MTask 的零 TaskCompletionSource 适配。
    /// </summary>
    public static class YooAssetMTaskExtensions
    {
        #region Public 公共成员

        /// <summary>
        /// 将 YooAsset 通用异步操作转换为 MTask。
        /// </summary>
        /// <param name="operation">YooAsset 异步操作。</param>
        /// <returns>操作完成或当前 MTask 取消时结束的任务。</returns>
        public static MTask ToMTask(this AsyncOperationBase operation)
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            return operation.IsDone ? MTask.CompletedTask : YooOperationMTaskSource.Create(operation);
        }

        /// <summary>
        /// 将 YooAsset 资源句柄转换为 MTask。
        /// </summary>
        /// <param name="handle">资源加载句柄。</param>
        /// <returns>句柄完成或当前 MTask 取消时结束的任务。</returns>
        public static MTask ToMTask(this AssetHandle handle)
        {
            if (handle == null)
            {
                throw new ArgumentNullException(nameof(handle));
            }

            return handle.IsDone ? MTask.CompletedTask : YooAssetHandleMTaskSource.Create(handle);
        }

        /// <summary>
        /// 将 YooAsset 场景句柄转换为 MTask。
        /// </summary>
        /// <param name="handle">场景加载句柄。</param>
        /// <returns>句柄完成或当前 MTask 取消时结束的任务。</returns>
        public static MTask ToMTask(this SceneHandle handle)
        {
            if (handle == null)
            {
                throw new ArgumentNullException(nameof(handle));
            }

            return handle.IsDone ? MTask.CompletedTask : YooSceneHandleMTaskSource.Create(handle);
        }

        #endregion
    }

    /// <summary>
    /// YooAsset MTask 适配源的公共单等待者实现。
    /// </summary>
    internal abstract class YooMTaskSourceBase : IMTaskSource
    {
        #region Private 私有成员

        private readonly object gate = new object(); // 保护完成状态和唯一续体。
        private readonly Action cancelAction; // 当前 MTask 节点取消回调。
        private Action continuation; // 唯一等待者续体。
        private IMTaskExecutor executor; // 等待者捕获的执行器。
        private MTaskCancellationRegistration cancellationRegistration; // 当前 MTask 节点的取消回调注册。
        private Exception exception; // 取消原因。
        private MTaskStatus status; // 当前适配状态。
        private bool registered; // 是否已注册等待者。
        private bool consumed; // 是否已消费。
        private short version; // 对象池复用版本。

        #endregion

        #region Protected 受保护成员

        /// <summary>
        /// 创建适配源并缓存取消回调。
        /// </summary>
        protected YooMTaskSourceBase()
        {
            cancelAction = Cancel;
        }

        /// <summary>
        /// 获取当前对象池版本。
        /// </summary>
        protected short Version => version;

        /// <summary>
        /// 初始化一个新的 YooAsset 等待周期。
        /// </summary>
        protected void Initialize()
        {
            unchecked
            {
                version++;
                if (version == 0)
                {
                    version = 1;
                }
            }

            continuation = null;
            executor = null;
            exception = null;
            status = MTaskStatus.Pending;
            registered = false;
            consumed = false;
            cancellationRegistration = MTaskRuntime.RegisterCancellation(cancelAction);
        }

        /// <summary>
        /// 由 YooAsset 完成事件成功结束等待。
        /// </summary>
        protected void Complete()
        {
            CompleteCore(MTaskStatus.Succeeded, null);
        }

        /// <summary>
        /// 解除具体 YooAsset 操作的事件订阅。
        /// </summary>
        protected abstract void Unsubscribe();

        /// <summary>
        /// 将具体适配源清理并归还对象池。
        /// </summary>
        protected abstract void ReturnToPool();

        #endregion

        #region Interface 接口实现

        /// <summary>
        /// 获取适配任务状态。
        /// </summary>
        /// <param name="token">对象池版本。</param>
        /// <returns>当前任务状态。</returns>
        public MTaskStatus GetStatus(short token)
        {
            ValidateToken(token);
            lock (gate)
            {
                return status;
            }
        }

        /// <summary>
        /// 注册 YooAsset 完成续体。
        /// </summary>
        /// <param name="value">完成续体。</param>
        /// <param name="token">对象池版本。</param>
        public void OnCompleted(Action value, short token)
        {
            ValidateToken(token);
            bool schedule;
            lock (gate)
            {
                if (registered || consumed)
                {
                    throw new InvalidOperationException("YooAsset MTask 只能等待一次。");
                }

                registered = true;
                continuation = value ?? throw new ArgumentNullException(nameof(value));
                executor = MTaskRuntime.CurrentExecutor ?? MTaskExecutors.Unity;
                schedule = status != MTaskStatus.Pending;
            }

            if (schedule)
            {
                Schedule();
            }
        }

        /// <summary>
        /// 消费 YooAsset 等待结果并归还对象池。
        /// </summary>
        /// <param name="token">对象池版本。</param>
        public void GetResult(short token)
        {
            ValidateToken(token);
            Exception value;
            lock (gate)
            {
                if (status == MTaskStatus.Pending || consumed)
                {
                    throw new InvalidOperationException("YooAsset MTask 尚未完成或已经被消费。");
                }

                consumed = true;
                value = exception;
            }

            cancellationRegistration.Dispose();
            continuation = null;
            executor = null;
            exception = null;
            ReturnToPool();
            if (value != null)
            {
                throw value;
            }
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 响应当前结构化任务节点取消。
        /// </summary>
        private void Cancel()
        {
            Unsubscribe();
            CompleteCore(MTaskStatus.Canceled, MTaskRuntime.GetCancellationException());
        }

        /// <summary>
        /// 原子地完成适配源并派发等待者。
        /// </summary>
        /// <param name="targetStatus">最终状态。</param>
        /// <param name="value">取消异常。</param>
        private void CompleteCore(MTaskStatus targetStatus, Exception value)
        {
            bool schedule;
            lock (gate)
            {
                if (status != MTaskStatus.Pending)
                {
                    return;
                }

                status = targetStatus;
                exception = value;
                schedule = registered;
            }

            cancellationRegistration.Dispose();
            if (schedule)
            {
                Schedule();
            }
        }

        /// <summary>
        /// 将完成续体派发到等待者捕获的执行器。
        /// </summary>
        private void Schedule()
        {
            Action callback;
            IMTaskExecutor target;
            lock (gate)
            {
                callback = continuation;
                target = executor ?? MTaskExecutors.Unity;
            }

            if (target.IsCurrentThread)
            {
                callback?.Invoke();
            }
            else if (callback != null)
            {
                target.Post(callback);
            }
        }

        /// <summary>
        /// 校验对象池版本。
        /// </summary>
        /// <param name="token">调用方版本。</param>
        private void ValidateToken(short token)
        {
            if (token != version)
            {
                throw new InvalidOperationException("YooAsset MTask 句柄已经失效。");
            }
        }

        #endregion
    }

    /// <summary>
    /// YooAsset 通用异步操作的池化适配源。
    /// </summary>
    internal sealed class YooOperationMTaskSource : YooMTaskSourceBase
    {
        #region Private 私有成员

        private static readonly ConcurrentStack<YooOperationMTaskSource> Pool = new ConcurrentStack<YooOperationMTaskSource>(); // 通用操作适配池。
        private readonly Action<AsyncOperationBase> completedAction; // YooAsset 完成事件回调。
        private AsyncOperationBase operation; // 当前等待的通用操作。

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 创建适配源并缓存事件委托。
        /// </summary>
        private YooOperationMTaskSource()
        {
            completedAction = OnCompleted;
        }

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 创建通用 YooAsset 操作等待任务。
        /// </summary>
        /// <param name="value">待等待操作。</param>
        /// <returns>对应的 MTask。</returns>
        internal static MTask Create(AsyncOperationBase value)
        {
            if (!Pool.TryPop(out YooOperationMTaskSource source))
            {
                source = new YooOperationMTaskSource();
            }

            source.operation = value;
            source.Initialize();
            value.Completed += source.completedAction;
            return new MTask(source, source.Version);
        }

        #endregion

        #region Protected 受保护成员

        /// <summary>
        /// 解除通用操作完成事件。
        /// </summary>
        protected override void Unsubscribe()
        {
            if (operation != null)
            {
                operation.Completed -= completedAction;
            }
        }

        /// <summary>
        /// 清理通用操作引用并归还池。
        /// </summary>
        protected override void ReturnToPool()
        {
            operation = null;
            Pool.Push(this);
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 响应 YooAsset 通用操作完成。
        /// </summary>
        /// <param name="value">已完成操作。</param>
        private void OnCompleted(AsyncOperationBase value)
        {
            Unsubscribe();
            Complete();
        }

        #endregion
    }

    /// <summary>
    /// YooAsset 资源句柄的池化适配源。
    /// </summary>
    internal sealed class YooAssetHandleMTaskSource : YooMTaskSourceBase
    {
        #region Private 私有成员

        private static readonly ConcurrentStack<YooAssetHandleMTaskSource> Pool = new ConcurrentStack<YooAssetHandleMTaskSource>(); // 资源句柄适配池。
        private readonly Action<AssetHandle> completedAction; // 资源句柄完成回调。
        private AssetHandle handle; // 当前等待的资源句柄。

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 创建资源句柄适配源并缓存事件委托。
        /// </summary>
        private YooAssetHandleMTaskSource()
        {
            completedAction = OnCompleted;
        }

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 创建资源句柄等待任务。
        /// </summary>
        /// <param name="value">待等待句柄。</param>
        /// <returns>对应的 MTask。</returns>
        internal static MTask Create(AssetHandle value)
        {
            if (!Pool.TryPop(out YooAssetHandleMTaskSource source))
            {
                source = new YooAssetHandleMTaskSource();
            }

            source.handle = value;
            source.Initialize();
            value.Completed += source.completedAction;
            return new MTask(source, source.Version);
        }

        #endregion

        #region Protected 受保护成员

        /// <summary>
        /// 解除资源句柄完成事件。
        /// </summary>
        protected override void Unsubscribe()
        {
            if (handle != null)
            {
                handle.Completed -= completedAction;
            }
        }

        /// <summary>
        /// 清理资源句柄引用并归还池。
        /// </summary>
        protected override void ReturnToPool()
        {
            handle = null;
            Pool.Push(this);
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 响应资源句柄完成。
        /// </summary>
        /// <param name="value">已完成句柄。</param>
        private void OnCompleted(AssetHandle value)
        {
            Unsubscribe();
            Complete();
        }

        #endregion
    }

    /// <summary>
    /// YooAsset 场景句柄的池化适配源。
    /// </summary>
    internal sealed class YooSceneHandleMTaskSource : YooMTaskSourceBase
    {
        #region Private 私有成员

        private static readonly ConcurrentStack<YooSceneHandleMTaskSource> Pool = new ConcurrentStack<YooSceneHandleMTaskSource>(); // 场景句柄适配池。
        private readonly Action<SceneHandle> completedAction; // 场景句柄完成回调。
        private SceneHandle handle; // 当前等待的场景句柄。

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 创建场景句柄适配源并缓存事件委托。
        /// </summary>
        private YooSceneHandleMTaskSource()
        {
            completedAction = OnCompleted;
        }

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 创建场景句柄等待任务。
        /// </summary>
        /// <param name="value">待等待句柄。</param>
        /// <returns>对应的 MTask。</returns>
        internal static MTask Create(SceneHandle value)
        {
            if (!Pool.TryPop(out YooSceneHandleMTaskSource source))
            {
                source = new YooSceneHandleMTaskSource();
            }

            source.handle = value;
            source.Initialize();
            value.Completed += source.completedAction;
            return new MTask(source, source.Version);
        }

        #endregion

        #region Protected 受保护成员

        /// <summary>
        /// 解除场景句柄完成事件。
        /// </summary>
        protected override void Unsubscribe()
        {
            if (handle != null)
            {
                handle.Completed -= completedAction;
            }
        }

        /// <summary>
        /// 清理场景句柄引用并归还池。
        /// </summary>
        protected override void ReturnToPool()
        {
            handle = null;
            Pool.Push(this);
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 响应场景句柄完成。
        /// </summary>
        /// <param name="value">已完成句柄。</param>
        private void OnCompleted(SceneHandle value)
        {
            Unsubscribe();
            Complete();
        }

        #endregion
    }

}
