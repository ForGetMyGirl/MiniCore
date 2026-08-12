using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MiniCore.Demo.MiniBomber.Unity
{

    /// <summary>
    /// 从 Input System Action Asset 读取桌面键盘或 Android On-Screen Control 输入。
    /// </summary>
    public sealed class BomberInputComponent : MonoBehaviour
    {
        #region UnityProperty Unity序列化字段

        [SerializeField] private InputActionAsset actions; // MiniBomberGameplay.inputactions。
        [SerializeField] private string actionMapName = "Gameplay"; // Gameplay Action Map 名称。
        [SerializeField] private string moveActionName = "Move"; // 移动 Action 名称。
        [SerializeField] private string placeBombActionName = "PlaceBomb"; // 炸弹 Action 名称。
        [SerializeField] private string menuActionName = "Menu"; // 菜单 Action 名称。
        [SerializeField] private string debugActionName = "Debug"; // 网络诊断 Action 名称。
        [SerializeField] private GameObject mobileControlRoot; // Android 摇杆和按钮根节点。
        [SerializeField] private GameObject desktopHintRoot; // Windows 键位提示根节点。

        #endregion

        #region Private 私有成员

        private InputActionMap gameplayMap; // 当前启用的 Gameplay Action Map。
        private InputAction moveAction; // 移动输入。
        private InputAction placeBombAction; // 炸弹按钮输入。
        private InputAction menuAction; // 菜单输入。
        private InputAction debugAction; // 网络诊断输入。
        private bool placeBombPressed; // 等待下一次平台无关输入帧消费的炸弹按键边沿。

        /// <summary>
        /// 缓存 Input System 已确认的炸弹按下边沿，避免触屏事件与 MonoBehaviour Update 顺序造成漏采样。
        /// </summary>
        /// <param name="context">炸弹 Action 的执行上下文。</param>
        private void HandlePlaceBombPerformed(InputAction.CallbackContext context)
        {
            placeBombPressed = true;
        }

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 每个 Unity Update 产生输入采样时触发。
        /// </summary>
        public event Action<BomberInputFrame> FrameReady;

        /// <summary>
        /// 按下菜单或系统返回操作时触发。
        /// </summary>
        public event Action MenuPressed;

        /// <summary>
        /// 按下网络诊断开关时触发。
        /// </summary>
        public event Action DebugPressed;

        #endregion

        #region Unity 生命周期函数

        /// <summary>
        /// 解析 Action 并按当前平台启用对应操作提示。
        /// </summary>
        private void Awake()
        {
            if (actions == null)
            {
                throw new InvalidOperationException("BomberInputComponent 必须配置 MiniBomberGameplay.inputactions。");
            }

            gameplayMap = actions.FindActionMap(actionMapName, true);
            moveAction = gameplayMap.FindAction(moveActionName, true);
            placeBombAction = gameplayMap.FindAction(placeBombActionName, true);
            menuAction = gameplayMap.FindAction(menuActionName, true);
            debugAction = gameplayMap.FindAction(debugActionName, true);
            bool mobile = Application.platform == RuntimePlatform.Android;
            if (mobileControlRoot != null)
            {
                mobileControlRoot.SetActive(mobile);
            }

            if (desktopHintRoot != null)
            {
                desktopHintRoot.SetActive(!mobile);
            }
        }

        /// <summary>
        /// 启用 Gameplay Action Map。
        /// </summary>
        private void OnEnable()
        {
            if (placeBombAction != null)
            {
                placeBombAction.performed += HandlePlaceBombPerformed;
            }

            gameplayMap?.Enable();
        }

        /// <summary>
        /// 禁用 Gameplay Action Map，避免场景退出后继续接收输入。
        /// </summary>
        private void OnDisable()
        {
            if (placeBombAction != null)
            {
                placeBombAction.performed -= HandlePlaceBombPerformed;
            }

            gameplayMap?.Disable();
            placeBombPressed = false;
        }

        /// <summary>
        /// 每帧读取动作值并向 HotUpdate 表现组件发送统一输入帧。
        /// </summary>
        private void Update()
        {
            if (moveAction == null || placeBombAction == null)
            {
                return;
            }

            bool placeBomb = placeBombPressed;
            placeBombPressed = false;
            FrameReady?.Invoke(new BomberInputFrame(moveAction.ReadValue<Vector2>(), placeBomb));
            if (menuAction.WasPressedThisFrame())
            {
                MenuPressed?.Invoke();
            }

            if (debugAction.WasPressedThisFrame())
            {
                DebugPressed?.Invoke();
            }
        }

        /// <summary>
        /// 释放 Action 引用和事件订阅者。
        /// </summary>
        private void OnDestroy()
        {
            FrameReady = null;
            MenuPressed = null;
            DebugPressed = null;
            gameplayMap = null;
            moveAction = null;
            placeBombAction = null;
            menuAction = null;
            debugAction = null;
        }

        #endregion
    }
}
