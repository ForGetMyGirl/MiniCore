using UnityEngine;

namespace MiniCore.Demo.MiniBomber.Unity
{
    /// <summary>
    /// 单个服务器权威炸弹的客户端表现。
    /// </summary>
    public sealed class BomberBombView : MonoBehaviour
    {
        #region Public 公共成员

        /// <summary>当前炸弹身份。</summary>
        public long BombId { get; private set; }

        /// <summary>
        /// 初始化炸弹身份和吸附格位置。
        /// </summary>
        /// <param name="bombId">炸弹身份。</param>
        /// <param name="cellX">横向格坐标。</param>
        /// <param name="cellZ">纵向格坐标。</param>
        public void Initialize(long bombId, int cellX, int cellZ)
        {
            BombId = bombId;
            transform.position = new Vector3(cellX + 0.5f, 0f, cellZ + 0.5f);
        }

        #endregion
    }
}
