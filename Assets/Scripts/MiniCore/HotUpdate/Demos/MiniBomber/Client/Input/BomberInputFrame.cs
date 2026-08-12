using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MiniCore.Demo.MiniBomber.Unity
{
    /// <summary>
    /// 平台无关的 MiniBomber 单次输入采样。
    /// </summary>
    public readonly struct BomberInputFrame
    {
        #region Public 公共成员

        /// <summary>
        /// 归一化移动方向。
        /// </summary>
        public Vector2 Move { get; }

        /// <summary>
        /// 本帧是否按下放置炸弹。
        /// </summary>
        public bool PlaceBomb { get; }

        /// <summary>
        /// 创建平台无关输入帧。
        /// </summary>
        /// <param name="move">归一化移动方向。</param>
        /// <param name="placeBomb">是否按下炸弹。</param>
        public BomberInputFrame(Vector2 move, bool placeBomb)
        {
            Move = Vector2.ClampMagnitude(move, 1f);
            PlaceBomb = placeBomb;
        }

        #endregion
    }
}
