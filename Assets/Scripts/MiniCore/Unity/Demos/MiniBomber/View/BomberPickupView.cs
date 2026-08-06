using UnityEngine;

namespace MiniCore.Demo.MiniBomber.Unity
{
    /// <summary>
    /// 炸弹数量和范围道具的客户端显示组件。
    /// </summary>
    public sealed class BomberPickupView : MonoBehaviour
    {
        #region Public 公共成员

        /// <summary>当前道具身份。</summary>
        public long PickupId { get; private set; }

        /// <summary>
        /// 初始化道具身份和格位置。
        /// </summary>
        /// <param name="pickupId">道具身份。</param>
        /// <param name="cellX">横向格坐标。</param>
        /// <param name="cellZ">纵向格坐标。</param>
        public void Initialize(long pickupId, int cellX, int cellZ)
        {
            PickupId = pickupId;
            transform.position = new Vector3(cellX + 0.5f, transform.position.y, cellZ + 0.5f);
        }

        #endregion
    }
}
