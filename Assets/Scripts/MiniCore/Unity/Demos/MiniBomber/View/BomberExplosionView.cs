using UnityEngine;

namespace MiniCore.Demo.MiniBomber.Unity
{
    /// <summary>
    /// 由服务器爆炸事件触发的短时客户端特效。
    /// </summary>
    public sealed class BomberExplosionView : MonoBehaviour
    {
        #region UnityProperty Unity序列化字段

        [SerializeField, Min(0.05f)] private float lifetime = 0.5f; // 特效自动销毁时间。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 在目标格播放爆炸并安排自动销毁。
        /// </summary>
        /// <param name="cellX">横向格坐标。</param>
        /// <param name="cellZ">纵向格坐标。</param>
        public void Play(int cellX, int cellZ)
        {
            transform.position = new Vector3(cellX + 0.5f, transform.position.y, cellZ + 0.5f);
            Destroy(gameObject, lifetime);
        }

        #endregion
    }
}
