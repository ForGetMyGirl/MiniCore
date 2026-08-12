using MiniCore.Threading;
using UnityEngine;

namespace MiniCore.UI
{

    /// <summary>
    /// 内置轻量窗口动画驱动，支持透明度、缩放和位移组合。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
    public sealed partial class UIPresetTransition : MonoBehaviour, IUITransitionDriver
    {
        #region UnityProperty Unity 引用属性

        [SerializeField] private RectTransform target; // 动画目标 RectTransform。
        [SerializeField] private CanvasGroup canvasGroup; // 动画目标透明度组件。

        #endregion

        #region Private 私有成员

        [SerializeField, Min(0f)] private float enterDuration = 0.2f; // 入场时长。
        [SerializeField, Min(0f)] private float exitDuration = 0.15f; // 退场时长。
        [SerializeField] private AnimationCurve enterCurve = null; // 入场采样曲线。
        [SerializeField] private AnimationCurve exitCurve = null; // 退场采样曲线。
        [SerializeField] private bool animateAlpha = true; // 是否动画透明度。
        [SerializeField] private bool animateScale = true; // 是否动画缩放。
        [SerializeField] private bool animatePosition; // 是否动画位置。
        [SerializeField] private Vector3 enterScale = new Vector3(0.92f, 0.92f, 1f); // 入场初始缩放。
        [SerializeField] private Vector2 enterOffset = Vector2.zero; // 入场初始位置偏移。
        [SerializeField] private Vector3 exitScale = new Vector3(0.96f, 0.96f, 1f); // 退场结束缩放。
        [SerializeField] private Vector2 exitOffset = Vector2.zero; // 退场结束位置偏移。
        [SerializeField] private bool useUnscaledTime = true; // 是否忽略游戏时间缩放。
        private Vector2 originalPosition; // 首次播放前位置。
        private Vector3 originalScale; // 首次播放前缩放。
        private float originalAlpha; // 首次播放前透明度。
        private int animationVersion; // 打断和重入保护版本。
        private bool captured; // 是否已经保存原始状态。

        #endregion

        #region Interface 接口实现

        /// <summary>
        /// 捕获原始状态并应用入场起点。
        /// </summary>
        public void ResetToEnterState()
        {
            EnsureReferences();
            CaptureOriginalState();
            animationVersion++;
            ApplyState(0f, UITransitionPhase.Enter);
        }

        /// <summary>
        /// 播放预设入场动画。
        /// </summary>
        /// <returns>入场完成任务。</returns>
        public MTask PlayEnterAsync()
        {
            return PlayAsync(UITransitionPhase.Enter, enterDuration, enterCurve);
        }

        /// <summary>
        /// 播放预设退场动画。
        /// </summary>
        /// <returns>退场完成任务。</returns>
        public MTask PlayExitAsync()
        {
            return PlayAsync(UITransitionPhase.Exit, exitDuration, exitCurve);
        }

        /// <summary>
        /// 中断当前动画并按策略收敛状态。
        /// </summary>
        /// <param name="mode">打断后的状态策略。</param>
        public void Interrupt(UITransitionInterruptMode mode)
        {
            animationVersion++;
            if (mode == UITransitionInterruptMode.RestoreOriginal)
            {
                RestoreOriginalState();
            }
            else if (mode == UITransitionInterruptMode.CompleteCurrent)
            {
                ApplyState(1f, UITransitionPhase.Enter);
            }
        }

        /// <summary>
        /// 立即应用指定动画阶段结束状态。
        /// </summary>
        /// <param name="phase">待完成阶段。</param>
        public void CompleteImmediately(UITransitionPhase phase)
        {
            animationVersion++;
            ApplyState(1f, phase);
        }

        /// <summary>
        /// 恢复动画首次播放前的原始状态。
        /// </summary>
        public void RestoreOriginalState()
        {
            if (!captured)
            {
                return;
            }

            EnsureReferences();
            target.anchoredPosition = originalPosition;
            target.localScale = originalScale;
            canvasGroup.alpha = originalAlpha;
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 在 Unity 校验阶段补齐组件引用和默认曲线。
        /// </summary>
        private void OnValidate()
        {
            EnsureReferences();
            enterCurve ??= AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            exitCurve ??= AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        }

        /// <summary>
        /// 播放指定阶段并响应任务取消和动画打断。
        /// </summary>
        /// <param name="phase">目标阶段。</param>
        /// <param name="duration">播放时长。</param>
        /// <param name="curve">采样曲线。</param>
        /// <returns>播放完成任务。</returns>
        private async MTask PlayAsync(UITransitionPhase phase, float duration, AnimationCurve curve)
        {
            EnsureReferences();
            CaptureOriginalState();
            int version = ++animationVersion;
            if (duration <= 0f)
            {
                ApplyState(1f, phase);
                return;
            }

            float elapsed = 0f;
            while (elapsed < duration && version == animationVersion)
            {
                MTask.ThrowIfCancellationRequested();
                elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                float normalized = Mathf.Clamp01(elapsed / duration);
                ApplyState(curve == null ? normalized : curve.Evaluate(normalized), phase);
                await MTask.Yield();
            }

            if (version == animationVersion)
            {
                ApplyState(1f, phase);
            }
        }

        /// <summary>
        /// 首次播放时保存窗口原始显示状态。
        /// </summary>
        private void CaptureOriginalState()
        {
            if (captured)
            {
                return;
            }

            originalPosition = target.anchoredPosition;
            originalScale = target.localScale;
            originalAlpha = canvasGroup.alpha;
            captured = true;
        }

        /// <summary>
        /// 应用指定动画阶段的插值状态。
        /// </summary>
        /// <param name="value">经过曲线采样的零到一进度。</param>
        /// <param name="phase">目标动画阶段。</param>
        private void ApplyState(float value, UITransitionPhase phase)
        {
            EnsureReferences();
            if (phase == UITransitionPhase.Enter)
            {
                if (animateAlpha)
                {
                    canvasGroup.alpha = Mathf.Lerp(0f, originalAlpha, value);
                }

                if (animateScale)
                {
                    target.localScale = Vector3.LerpUnclamped(enterScale, originalScale, value);
                }

                if (animatePosition)
                {
                    target.anchoredPosition = Vector2.LerpUnclamped(originalPosition + enterOffset, originalPosition, value);
                }
            }
            else
            {
                if (animateAlpha)
                {
                    canvasGroup.alpha = Mathf.Lerp(originalAlpha, 0f, value);
                }

                if (animateScale)
                {
                    target.localScale = Vector3.LerpUnclamped(originalScale, exitScale, value);
                }

                if (animatePosition)
                {
                    target.anchoredPosition = Vector2.LerpUnclamped(originalPosition, originalPosition + exitOffset, value);
                }
            }
        }

        /// <summary>
        /// 惰性获取动画目标组件。
        /// </summary>
        private void EnsureReferences()
        {
            target ??= transform as RectTransform;
            canvasGroup ??= GetComponent<CanvasGroup>();
        }

        #endregion
    }
}
