using UnityEngine;

namespace MiniCore.Demo.MiniBomber.Unity
{
    /// <summary>
    /// 单个玩家的插值显示、朝向和生死表现。
    /// </summary>
    public sealed class BomberPlayerView : MonoBehaviour
    {
        #region UnityProperty Unity序列化字段

        [SerializeField, Min(0.01f)] private float convergenceSpeed = 14f; // 小误差插值收敛速度。
        [SerializeField, Min(0.1f)] private float snapDistance = 0.75f; // 超过此格数时直接校正。
        [SerializeField, Min(0f)] private float animationDampTime = 0.08f; // 移动动画参数的平滑时间。
        [SerializeField] private Animator animator; // 角色待机和移动状态机。
        [SerializeField] private SkinnedMeshRenderer skinnedRenderer; // 角色蒙皮网格渲染器。
        [SerializeField] private GameObject visualRoot; // 生存时启用的角色表现根。
        [SerializeField] private GameObject protectionVisual; // 复活保护表现。

        #endregion

        #region Private 私有成员

        private static readonly int SpeedParameter = Animator.StringToHash("Speed"); // Animator 移动参数。
        private Vector3 targetPosition; // 最新权威目标位置。
        private int cellSizeMillimeters = 1000; // 当前地图格边长。
        private int lastAuthorityXMillimeters; // 上一次权威 X 坐标。
        private int lastAuthorityZMillimeters; // 上一次权威 Z 坐标。
        private float targetAnimationSpeed; // 当前期望动画速度参数。
        private bool hasAuthorityPosition; // 是否已经接收过权威坐标。
        private bool isAlive; // 最近一次权威快照中的存活状态。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 当前绑定玩家身份。
        /// </summary>
        public long PlayerId { get; private set; }

        /// <summary>
        /// 初始化玩家身份和地图单位。
        /// </summary>
        /// <param name="playerId">玩家身份。</param>
        /// <param name="cellSize">单格毫米数。</param>
        public void Initialize(long playerId, int cellSize)
        {
            PlayerId = playerId;
            cellSizeMillimeters = Mathf.Max(1, cellSize);
            targetPosition = transform.position;
            hasAuthorityPosition = false;
            isAlive = false;
            targetAnimationSpeed = 0f;
            ConfigureMobileVisuals();
            if (animator != null)
            {
                animator.SetFloat(SpeedParameter, 0f);
            }
        }

        /// <summary>
        /// 应用服务器权威位置和状态。
        /// </summary>
        /// <param name="xMillimeters">权威 X 毫米。</param>
        /// <param name="zMillimeters">权威 Z 毫米。</param>
        /// <param name="facingX">量化朝向 X。</param>
        /// <param name="facingZ">量化朝向 Z。</param>
        /// <param name="alive">是否存活。</param>
        /// <param name="protectedState">是否处于复活保护。</param>
        public void ApplyState(int xMillimeters, int zMillimeters, int facingX, int facingZ, bool alive, bool protectedState)
        {
            bool moved = hasAuthorityPosition &&
                         (lastAuthorityXMillimeters != xMillimeters || lastAuthorityZMillimeters != zMillimeters);
            targetAnimationSpeed = alive && moved ? 1f : 0f;
            lastAuthorityXMillimeters = xMillimeters;
            lastAuthorityZMillimeters = zMillimeters;
            hasAuthorityPosition = true;
            isAlive = alive;

            float unit = 1f / cellSizeMillimeters;
            targetPosition = new Vector3(xMillimeters * unit, transform.position.y, zMillimeters * unit);
            if ((targetPosition - transform.position).sqrMagnitude > snapDistance * snapDistance)
            {
                transform.position = targetPosition;
            }

            if (facingX != 0 || facingZ != 0)
            {
                transform.forward = new Vector3(facingX, 0f, facingZ).normalized;
            }

            if (visualRoot != null)
            {
                visualRoot.SetActive(alive);
            }

            if (protectionVisual != null)
            {
                protectionVisual.SetActive(alive && protectedState);
            }
        }

        /// <summary>
        /// 对本地玩家立即应用仅用于显示的移动预测；服务器快照仍会持续校正目标位置。
        /// </summary>
        /// <param name="move">归一化移动输入。</param>
        /// <param name="speedCellsPerSecond">每秒移动格数。</param>
        /// <param name="deltaTime">当前显示帧时长。</param>
        public void Predict(Vector2 move, float speedCellsPerSecond, float deltaTime)
        {
            if (!isAlive)
            {
                targetAnimationSpeed = 0f;
                return;
            }

            Vector2 normalized = Vector2.ClampMagnitude(move, 1f);
            targetAnimationSpeed = normalized.sqrMagnitude > 0.0001f ? 1f : 0f;
            Vector3 delta = new Vector3(normalized.x, 0f, normalized.y) * Mathf.Max(0f, speedCellsPerSecond) * Mathf.Max(0f, deltaTime);
            transform.position += delta;
            targetPosition += delta;
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 配置角色的蒙皮网格和动画裁剪策略，避免移动设备因动态包围盒不可见而停止渲染。
        /// </summary>
        private void ConfigureMobileVisuals()
        {
            if (skinnedRenderer != null)
            {
                skinnedRenderer.updateWhenOffscreen = true;
            }
            else
            {
                Debug.LogError($"MiniBomber 玩家 {PlayerId} 缺少 SkinnedMeshRenderer 引用。", this);
            }

            if (animator != null)
            {
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }
        }

        #endregion

        #region Unity 生命周期函数

        /// <summary>
        /// 平滑收敛到最新服务器权威目标位置。
        /// </summary>
        private void Update()
        {
            float factor = 1f - Mathf.Exp(-convergenceSpeed * Time.deltaTime);
            transform.position = Vector3.LerpUnclamped(transform.position, targetPosition, factor);
            if (animator != null)
            {
                animator.SetFloat(SpeedParameter, targetAnimationSpeed, animationDampTime, Time.deltaTime);
            }
        }

        #endregion
    }
}
