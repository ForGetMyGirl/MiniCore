using MiniCore.Threading;
using UnityEngine;

namespace MiniCore.UI
{

    /// <summary>
    /// 使用 Animator 状态播放窗口入场和退场动画。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public sealed partial class UIAnimatorTransition : MonoBehaviour, IUITransitionDriver
    {
        #region UnityProperty Unity 引用属性

        [SerializeField] private Animator animator; // 窗口 Animator。

        #endregion

        #region Private 私有成员

        [SerializeField] private string enterState = "Enter"; // 入场状态名。
        [SerializeField] private string exitState = "Exit"; // 退场状态名。
        [SerializeField] private string idleState = "Idle"; // 稳定显示状态名。
        [SerializeField] private string hiddenState = "Hidden"; // 隐藏状态名。
        [SerializeField, Min(0.05f)] private float timeoutSeconds = 5f; // 动画最大等待时长。
        private int animationVersion; // 打断和重入保护版本。

        #endregion

        #region Interface 接口实现

        /// <summary>
        /// 将 Animator 重置到隐藏状态。
        /// </summary>
        public void ResetToEnterState()
        {
            EnsureAnimator();
            animationVersion++;
            PlayImmediate(hiddenState);
        }

        /// <summary>
        /// 播放 Animator 入场状态。
        /// </summary>
        /// <returns>入场完成任务。</returns>
        public MTask PlayEnterAsync() => PlayStateAsync(enterState, idleState);

        /// <summary>
        /// 播放 Animator 退场状态。
        /// </summary>
        /// <returns>退场完成任务。</returns>
        public MTask PlayExitAsync() => PlayStateAsync(exitState, hiddenState);

        /// <summary>
        /// 中断 Animator 播放并按策略收敛。
        /// </summary>
        /// <param name="mode">中断策略。</param>
        public void Interrupt(UITransitionInterruptMode mode)
        {
            animationVersion++;
            if (mode == UITransitionInterruptMode.RestoreOriginal)
            {
                PlayImmediate(idleState);
            }
        }

        /// <summary>
        /// 立即应用指定阶段结束状态。
        /// </summary>
        /// <param name="phase">待完成阶段。</param>
        public void CompleteImmediately(UITransitionPhase phase)
        {
            animationVersion++;
            PlayImmediate(phase == UITransitionPhase.Enter ? idleState : hiddenState);
        }

        /// <summary>
        /// 恢复 Animator 到稳定显示状态。
        /// </summary>
        public void RestoreOriginalState()
        {
            animationVersion++;
            PlayImmediate(idleState);
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 在 Unity 校验阶段补齐 Animator 引用。
        /// </summary>
        private void OnValidate()
        {
            EnsureAnimator();
        }

        /// <summary>
        /// 播放一个 Animator 状态并在结束或超时时进入稳定状态。
        /// </summary>
        /// <param name="stateName">目标动画状态。</param>
        /// <param name="completedState">播放结束后的稳定状态。</param>
        /// <returns>播放完成任务。</returns>
        private async MTask PlayStateAsync(string stateName, string completedState)
        {
            EnsureAnimator();
            if (string.IsNullOrWhiteSpace(stateName))
            {
                PlayImmediate(completedState);
                return;
            }

            int version = ++animationVersion;
            animator.Play(stateName, 0, 0f);
            float elapsed = 0f;
            await MTask.Yield();
            while (version == animationVersion && elapsed < timeoutSeconds)
            {
                MTask.ThrowIfCancellationRequested();
                AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
                if (state.IsName(stateName) && state.normalizedTime >= 1f && !animator.IsInTransition(0))
                {
                    break;
                }

                elapsed += Time.unscaledDeltaTime;
                await MTask.Yield();
            }

            if (version == animationVersion)
            {
                PlayImmediate(completedState);
            }
        }

        /// <summary>
        /// 在当前帧立即采样一个 Animator 状态。
        /// </summary>
        /// <param name="stateName">目标状态名。</param>
        private void PlayImmediate(string stateName)
        {
            EnsureAnimator();
            if (!string.IsNullOrWhiteSpace(stateName))
            {
                animator.Play(stateName, 0, 1f);
                animator.Update(0f);
            }
        }

        /// <summary>
        /// 惰性获取窗口 Animator。
        /// </summary>
        private void EnsureAnimator()
        {
            animator ??= GetComponent<Animator>();
        }

        #endregion
    }
}
