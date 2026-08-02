using MiniCore.Threading;
using MiniCore.Unity;
using UnityEngine;

namespace MiniCore.UI
{
    /// <summary>
    /// 在加载超过阈值时显示且满足最短展示时间的全局输入遮罩。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class UILoadingOverlay : AMTaskBehaviour
    {
        #region UnityProperty Unity 引用属性

        [SerializeField] private CanvasGroup canvasGroup; // Loading 显示和输入阻断组件。

        #endregion

        #region Private 私有成员

        private int delayMilliseconds; // 延迟显示时间。
        private int minimumMilliseconds; // 最短显示时间。
        private int operationCount; // 当前未完成加载操作数。
        private int version; // 防止过期延迟任务改变状态。
        private float shownAt; // 最近一次实际显示时间。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 使用项目 Profile 初始化显示阈值和隐藏状态。
        /// </summary>
        /// <param name="profile">当前项目 UI Profile。</param>
        public void Initialize(UIProjectProfile profile)
        {
            delayMilliseconds = profile.LoadingDelayMilliseconds;
            minimumMilliseconds = profile.LoadingMinimumMilliseconds;
            EnsureCanvasGroup();
            ApplyVisible(false);
        }

        /// <summary>
        /// 登记一项可能显示 Loading 的加载操作。
        /// </summary>
        public void Begin()
        {
            operationCount++;
            if (operationCount != 1)
            {
                return;
            }

            int currentVersion = ++version;
            ShowDelayedAsync(currentVersion).Forget();
        }

        /// <summary>
        /// 结束一项加载操作并在全部完成后安全隐藏。
        /// </summary>
        public void End()
        {
            if (operationCount > 0)
            {
                operationCount--;
            }

            if (operationCount != 0)
            {
                return;
            }

            int currentVersion = ++version;
            HideDelayedAsync(currentVersion).Forget();
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// Unity 校验阶段补齐 CanvasGroup 引用。
        /// </summary>
        private void OnValidate()
        {
            EnsureCanvasGroup();
        }

        /// <summary>
        /// 等待配置延迟后仅为仍未结束的操作显示反馈。
        /// </summary>
        /// <param name="expectedVersion">发起操作时的版本。</param>
        /// <returns>延迟显示任务。</returns>
        private async MTask ShowDelayedAsync(int expectedVersion)
        {
            if (delayMilliseconds > 0)
            {
                await MTask.Delay(delayMilliseconds);
            }

            if (expectedVersion == version && operationCount > 0)
            {
                shownAt = Time.unscaledTime;
                ApplyVisible(true);
            }
        }

        /// <summary>
        /// 满足最短显示时长后隐藏当前 Loading。
        /// </summary>
        /// <param name="expectedVersion">操作全部结束时的版本。</param>
        /// <returns>延迟隐藏任务。</returns>
        private async MTask HideDelayedAsync(int expectedVersion)
        {
            EnsureCanvasGroup();
            if (canvasGroup.alpha <= 0f)
            {
                return;
            }

            int elapsed = Mathf.RoundToInt((Time.unscaledTime - shownAt) * 1000f);
            int remaining = Mathf.Max(0, minimumMilliseconds - elapsed);
            if (remaining > 0)
            {
                await MTask.Delay(remaining);
            }

            if (expectedVersion == version && operationCount == 0)
            {
                ApplyVisible(false);
            }
        }

        /// <summary>
        /// 应用透明度和输入阻断状态。
        /// </summary>
        /// <param name="visible">是否显示并阻断下层输入。</param>
        private void ApplyVisible(bool visible)
        {
            EnsureCanvasGroup();
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
            gameObject.SetActive(true);
        }

        /// <summary>
        /// 惰性获取当前节点 CanvasGroup。
        /// </summary>
        private void EnsureCanvasGroup()
        {
            canvasGroup ??= GetComponent<CanvasGroup>();
        }

        #endregion
    }
}
