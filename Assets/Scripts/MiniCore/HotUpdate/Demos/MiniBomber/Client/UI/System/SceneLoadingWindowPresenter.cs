using System;
using System.Text;
using MiniCore.Core;
using MiniCore.Model;
using MiniCore.Protocol.Generated;
using MiniCore.Service;
using MiniCore.Threading;
using MiniCore.UI;
using UnityEngine;

namespace MiniCore.Demo.MiniBomber
{

    /// <summary>
    /// 场景加载窗口 Presenter。
    /// </summary>
    public sealed class SceneLoadingWindowPresenter : AUIWindowPresenter<SceneLoadingWindowView>
    {
        #region Private 私有成员

        private ISceneService scenes; // 当前场景加载服务。

        #endregion

        #region Protected 受保护成员

        /// <summary>
        /// 显示当前场景服务进度。
        /// </summary>
        protected override void OnBind()
        {
            scenes = Global.GetService<ISceneService>(this);
            scenes.ProgressChanged += RenderProgress;
            Bindings.Add(() => scenes.ProgressChanged -= RenderProgress);
            RenderProgress(scenes.Progress);
            View.PromptText.text = "正在加载场景...";
        }

        /// <summary>
        /// 清空场景服务引用。
        /// </summary>
        protected override void OnDispose()
        {
            scenes = null;
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 将场景进度更新到进度条。
        /// </summary>
        /// <param name="progress">零到一的加载进度。</param>
        private void RenderProgress(float progress)
        {
            View.ProgressSlider.value = Mathf.Clamp01(progress);
        }

        #endregion
    }
}
