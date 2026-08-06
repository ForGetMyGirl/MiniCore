using UnityEngine;

namespace MiniCore.Demo.MiniBomber.Unity
{
    /// <summary>
    /// 平滑跟随本地玩家并限制在地图边界内的俯视相机。
    /// </summary>
    public sealed class BomberCameraController : MonoBehaviour
    {
        #region UnityProperty Unity序列化字段

        [SerializeField] private Vector3 offset = new Vector3(0f, 10f, -6f); // 相机相对玩家偏移。
        [SerializeField, Min(0.1f)] private float followSpeed = 8f; // 平滑跟随速度。
        [SerializeField] private Vector2 minimumXZ = Vector2.zero; // 相机目标最小 XZ。
        [SerializeField] private Vector2 maximumXZ = new Vector2(17f, 13f); // 相机目标最大 XZ。

        #endregion

        #region Private 私有成员

        private Transform target; // 本地玩家表现节点。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 设置相机跟随的本地玩家表现。
        /// </summary>
        /// <param name="value">本地玩家节点。</param>
        public void SetTarget(Transform value)
        {
            target = value;
        }

        /// <summary>
        /// 按地图尺寸设置相机目标边界。
        /// </summary>
        /// <param name="width">地图宽度格数。</param>
        /// <param name="height">地图高度格数。</param>
        public void SetMapBounds(int width, int height)
        {
            minimumXZ = Vector2.zero;
            maximumXZ = new Vector2(Mathf.Max(0f, width), Mathf.Max(0f, height));
        }

        #endregion

        #region Unity 生命周期函数

        /// <summary>
        /// 在玩家表现插值完成后更新相机位置。
        /// </summary>
        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector3 targetPosition = target.position;
            targetPosition.x = Mathf.Clamp(targetPosition.x, minimumXZ.x, maximumXZ.x);
            targetPosition.z = Mathf.Clamp(targetPosition.z, minimumXZ.y, maximumXZ.y);
            Vector3 desired = targetPosition + offset;
            float factor = 1f - Mathf.Exp(-followSpeed * Time.deltaTime);
            transform.position = Vector3.LerpUnclamped(transform.position, desired, factor);
        }

        #endregion
    }
}
