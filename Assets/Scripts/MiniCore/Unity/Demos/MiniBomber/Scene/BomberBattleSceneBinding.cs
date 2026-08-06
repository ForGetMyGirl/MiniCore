using System;
using UnityEngine;

namespace MiniCore.Demo.MiniBomber.Unity
{
    /// <summary>
    /// BattleScene 中唯一的世界表现引用入口，不承载权威玩法逻辑。
    /// </summary>
    public sealed class BomberBattleSceneBinding : MonoBehaviour
    {
        #region UnityProperty Unity序列化字段

        [SerializeField] private BomberMapView mapView; // 地图表现。
        [SerializeField] private BomberInputComponent input; // 平台输入源。
        [SerializeField] private BomberCameraController cameraController; // 本地玩家相机。
        [SerializeField] private Transform playerRoot; // 玩家实例根节点。
        [SerializeField] private Transform bombRoot; // 炸弹实例根节点。
        [SerializeField] private Transform effectRoot; // 爆炸和道具实例根节点。
        [SerializeField] private BomberPlayerView playerPrefab; // 玩家表现 Prefab。
        [SerializeField] private BomberBombView bombPrefab; // 炸弹表现 Prefab。
        [SerializeField] private BomberExplosionView explosionPrefab; // 爆炸格表现 Prefab。
        [SerializeField] private BomberPickupView pickupPrefab; // 道具表现 Prefab。

        #endregion

        #region Public 公共成员

        /// <summary>BattleScene Binding 启用事件。</summary>
        public static event Action<BomberBattleSceneBinding> Available;

        /// <summary>地图表现。</summary>
        public BomberMapView MapView => mapView;

        /// <summary>平台输入源。</summary>
        public BomberInputComponent Input => input;

        /// <summary>本地玩家相机。</summary>
        public BomberCameraController CameraController => cameraController;

        /// <summary>
        /// 创建玩家表现实例。
        /// </summary>
        /// <returns>新玩家表现。</returns>
        public BomberPlayerView CreatePlayer()
        {
            return Instantiate(Require(playerPrefab, nameof(playerPrefab)), Require(playerRoot, nameof(playerRoot)));
        }

        /// <summary>
        /// 创建炸弹表现实例。
        /// </summary>
        /// <returns>新炸弹表现。</returns>
        public BomberBombView CreateBomb()
        {
            return Instantiate(Require(bombPrefab, nameof(bombPrefab)), Require(bombRoot, nameof(bombRoot)));
        }

        /// <summary>
        /// 创建爆炸格表现实例。
        /// </summary>
        /// <returns>新爆炸表现。</returns>
        public BomberExplosionView CreateExplosion()
        {
            return Instantiate(Require(explosionPrefab, nameof(explosionPrefab)), Require(effectRoot, nameof(effectRoot)));
        }

        /// <summary>
        /// 创建道具表现实例。
        /// </summary>
        /// <returns>新道具表现。</returns>
        public BomberPickupView CreatePickup()
        {
            return Instantiate(Require(pickupPrefab, nameof(pickupPrefab)), Require(effectRoot, nameof(effectRoot)));
        }

        /// <summary>
        /// 销毁指定世界表现对象。
        /// </summary>
        /// <param name="instance">待销毁实例。</param>
        public void DestroyView(Component instance)
        {
            if (instance != null)
            {
                Destroy(instance.gameObject);
            }
        }

        #endregion

        #region Unity 生命周期函数

        /// <summary>
        /// 通知 HotUpdate 表现组件 BattleScene 已经可以绑定。
        /// </summary>
        private void OnEnable()
        {
            Available?.Invoke(this);
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 验证必须配置的 Unity 引用。
        /// </summary>
        /// <typeparam name="T">Unity 对象类型。</typeparam>
        /// <param name="value">待验证对象。</param>
        /// <param name="fieldName">字段名称。</param>
        /// <returns>非空对象。</returns>
        private static T Require<T>(T value, string fieldName) where T : UnityEngine.Object
        {
            return value != null ? value : throw new InvalidOperationException($"BomberBattleSceneBinding 未配置 {fieldName}。");
        }

        #endregion
    }
}
