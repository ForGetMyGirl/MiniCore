using System.Collections.Generic;
using System.IO;
using System.Reflection;
using MiniCore.Demo.MiniBomber;
using MiniCore.Demo.MiniBomber.Unity;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MiniCore.Tests.Editor.Demos.MiniBomber
{
    /// <summary>
    /// MiniBomber UI 输入、角色动画和动态资源构建配置的资产回归测试。
    /// </summary>
    public sealed class MiniBomberPresentationAssetTests
    {
        #region Private 私有成员

        private const string BattleHudPath = "Assets/AssetRes/Demos/MiniBomber/UI/BattleHudWindow.prefab"; // 战斗 HUD 资产路径。
        private const string BattleScenePath = "Assets/Scenes/Demos/MiniBomber/BattleScene.unity"; // 战斗场景资产路径。
        private const string BombPrefabPath = "Assets/AssetRes/Demos/MiniBomber/Prefabs/Bomb.prefab"; // 炸弹表现资产路径。
        private const string PlayerAvatarPath = "Assets/AssetRes/Demos/MiniBomber/Prefabs/PlayerAvatar.prefab"; // 玩家表现资产路径。
        private const string GameplayActionsPath = "Assets/AssetRes/Demos/MiniBomber/Config/MiniBomberGameplay.inputactions"; // 战斗输入资产路径。
        private const string LinkerConfigPath = "Assets/Linker/MiniCore.link.xml"; // 动态资源类型保留配置路径。
        private const string NativeTypePreserverPath = "Assets/Scripts/Project/Bootstrap/UnityEngineTypePreserver.cs"; // Unity 原生模块显式保护入口路径。
        private const string BootstrapPath = "Assets/Scripts/Project/Bootstrap/UpdateMainWindow.cs"; // 热更新 Bootstrap 启动流程路径。
        private const string BomberInputPath = "Assets/Scripts/MiniCore/HotUpdate/Demos/MiniBomber/Client/Input/BomberInputComponent.cs"; // 移动端输入采样实现路径。
        private const string ClientStartupPath = "Assets/Scripts/MiniCore/HotUpdate/Demos/MiniBomber/Entry/MiniBomberClientStartupComponent.cs"; // 客户端启动入口路径。
        private const string BattlePresentationPath = "Assets/Scripts/MiniCore/HotUpdate/Demos/MiniBomber/Client/Presentation/MiniBomberBattlePresentationComponent.cs"; // 战斗世界表现桥接路径。
        private const string WindowPresenterPath = "Assets/Scripts/MiniCore/HotUpdate/Demos/MiniBomber/Client/UI/Battle/BattleHudWindowPresenter.cs"; // 战斗 HUD 性能信息实现路径。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 验证 Android 炸弹按钮通过 OnScreenButton 向 PlaceBomb Action 注入输入。
        /// </summary>
        [Test]
        public void BattleHudMobileControls_RouteBombButtonThroughInputSystem()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BattleHudPath);
            Assert.That(prefab, Is.Not.Null, $"未找到战斗 HUD：{BattleHudPath}");

            BattleHudWindowView view = prefab.GetComponent<BattleHudWindowView>();
            Assert.That(view, Is.Not.Null, "BattleHudWindow 根节点缺少 BattleHudWindowView。");
            Assert.That(view.MobileControlRoot, Is.Not.Null, "BattleHudWindowView 未绑定 MobileControlRoot。");
            SerializedObject serializedView = new SerializedObject(view);
            SerializedProperty performanceText = serializedView.FindProperty("PerformanceText");
            Assert.That(performanceText, Is.Not.Null, "BattleHudWindowView 缺少 PerformanceText 序列化字段。");
            Assert.That(performanceText.objectReferenceValue, Is.Not.Null, "BattleHudWindowView 未绑定 performanceText。");
            Assert.That(performanceText.objectReferenceValue.name, Is.EqualTo("performanceText"));

            Transform moveJoystick = FindDescendant(view.MobileControlRoot.transform, "MoveJoystick");
            Assert.That(moveJoystick, Is.Not.Null, "MobileControlRoot 下缺少 MoveJoystick。");
            Transform joystickHandle = FindDescendant(moveJoystick, "Handle");
            Assert.That(joystickHandle, Is.Not.Null, "MoveJoystick 下缺少 Handle。");
            Component onScreenStick = FindComponent(joystickHandle, "UnityEngine.InputSystem.OnScreen.OnScreenStick");
            Assert.That(onScreenStick, Is.Not.Null, "MoveJoystick/Handle 缺少 Input System 的 OnScreenStick。");
            SerializedObject serializedStick = new SerializedObject(onScreenStick);
            SerializedProperty movementRange = serializedStick.FindProperty("m_MovementRange");
            Assert.That(movementRange, Is.Not.Null, "OnScreenStick 缺少 m_MovementRange 序列化字段。");
            Assert.That(movementRange.floatValue, Is.EqualTo(100f));

            Transform bombButton = FindDescendant(view.MobileControlRoot.transform, "BombButton");
            Assert.That(bombButton, Is.Not.Null, "MobileControlRoot 下缺少 BombButton。");
            Component onScreenButton = FindComponent(bombButton, "UnityEngine.InputSystem.OnScreen.OnScreenButton");
            Assert.That(onScreenButton, Is.Not.Null, "BombButton 缺少 Input System 的 OnScreenButton。");

            SerializedObject serializedButton = new SerializedObject(onScreenButton);
            SerializedProperty controlPath = serializedButton.FindProperty("m_ControlPath");
            Assert.That(controlPath, Is.Not.Null, "OnScreenButton 缺少 m_ControlPath 序列化字段。");
            Assert.That(controlPath.stringValue, Is.EqualTo("<Gamepad>/buttonSouth"));

            string actionJson = File.ReadAllText(Path.GetFullPath(GameplayActionsPath));
            StringAssert.Contains("\"name\": \"PlaceBomb\"", actionJson);
            StringAssert.Contains("\"path\": \"<Gamepad>/buttonSouth\"", actionJson);
        }

        /// <summary>
        /// 验证 PlayerAvatar 使用专用 Animator Controller，且表现脚本引用同一个 Animator。
        /// </summary>
        [Test]
        public void PlayerAvatar_UsesDedicatedAnimatorController()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerAvatarPath);
            Assert.That(prefab, Is.Not.Null, $"未找到玩家 Prefab：{PlayerAvatarPath}");

            BomberPlayerView view = prefab.GetComponent<BomberPlayerView>();
            Animator animator = prefab.GetComponentInChildren<Animator>(true);
            Assert.That(view, Is.Not.Null, "PlayerAvatar 根节点缺少 BomberPlayerView。");
            Assert.That(animator, Is.Not.Null, "PlayerAvatar 缺少 Animator。");
            Assert.That(animator.runtimeAnimatorController, Is.Not.Null, "PlayerAvatar 的 Animator 未配置 Controller。");
            Assert.That(AssetDatabase.GetAssetPath(animator.runtimeAnimatorController), Is.EqualTo("Assets/Anim/BomberMan.controller"));

            FieldInfo animatorField = typeof(BomberPlayerView).GetField("animator", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(animatorField, Is.Not.Null);
            Assert.That(animatorField.GetValue(view), Is.SameAs(animator), "BomberPlayerView 未引用 PlayerAvatar 的 Animator。");

            SkinnedMeshRenderer renderer = prefab.GetComponentInChildren<SkinnedMeshRenderer>(true);
            Assert.That(renderer, Is.Not.Null, "PlayerAvatar 缺少 SkinnedMeshRenderer。");
            Assert.That(renderer.sharedMesh, Is.Not.Null, "PlayerAvatar 的 SkinnedMeshRenderer 未配置 Mesh。");
            Assert.That(renderer.sharedMaterial, Is.Not.Null, "PlayerAvatar 的 SkinnedMeshRenderer 未配置 Material。");
        }

        /// <summary>
        /// 验证 BattleScene 的地面对象具有可用网格、材质和着色器。
        /// </summary>
        [Test]
        public void BattleScene_GroundHasRenderableMesh()
        {
            Scene scene = EditorSceneManager.OpenScene(BattleScenePath, OpenSceneMode.Additive);
            try
            {
                GameObject ground = FindSceneObject(scene, "Environment/Ground/Cube");
                Assert.That(ground, Is.Not.Null, "BattleScene 缺少 Environment/Ground/Cube。");

                MeshFilter meshFilter = ground.GetComponent<MeshFilter>();
                MeshRenderer meshRenderer = ground.GetComponent<MeshRenderer>();
                Assert.That(meshFilter, Is.Not.Null);
                Assert.That(meshFilter.sharedMesh, Is.Not.Null, "地面 MeshFilter 未配置网格。");
                Assert.That(meshRenderer, Is.Not.Null);
                Assert.That(meshRenderer.enabled, Is.True);
                Assert.That(meshRenderer.sharedMaterial, Is.Not.Null, "地面 MeshRenderer 未配置材质。");
                Assert.That(meshRenderer.sharedMaterial.shader, Is.Not.Null, "地面材质未配置有效 Shader。");
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        /// <summary>
        /// 验证炸弹忽略 Prefab 编辑位置并始终吸附到目标格的地面中心。
        /// </summary>
        [Test]
        public void BombView_InitializesAtGroundCellCenter()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BombPrefabPath);
            Assert.That(prefab, Is.Not.Null, $"未找到炸弹 Prefab：{BombPrefabPath}");
            Assert.That(prefab.transform.localPosition, Is.EqualTo(Vector3.zero), "炸弹 Prefab 根节点必须保持原点。");

            GameObject instance = Object.Instantiate(prefab);
            try
            {
                instance.transform.position = new Vector3(20f, 8f, 30f);
                BomberBombView view = instance.GetComponent<BomberBombView>();
                Assert.That(view, Is.Not.Null);

                view.Initialize(7L, 2, 3);

                Assert.That(instance.transform.position, Is.EqualTo(new Vector3(2.5f, 0f, 3.5f)));
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        /// <summary>
        /// 验证可靠方块摧毁事件可以立即隐藏目标木箱，不需要等待后续快照。
        /// </summary>
        [Test]
        public void MapView_HidesBreakableImmediately()
        {
            Scene scene = EditorSceneManager.OpenScene(BattleScenePath, OpenSceneMode.Additive);
            try
            {
                BomberMapView mapView = FindSceneComponent<BomberMapView>(scene);
                Assert.That(mapView, Is.Not.Null, "BattleScene 缺少 BomberMapView。");
                mapView.Build();

                int cellX = -1;
                int cellZ = -1;
                for (int z = 0; z < mapView.Definition.Height && cellX < 0; z++)
                {
                    for (int x = 0; x < mapView.Definition.Width; x++)
                    {
                        if (mapView.Definition.GetCell(x, z) == MiniBomberCellType.Breakable)
                        {
                            cellX = x;
                            cellZ = z;
                            break;
                        }
                    }
                }

                Assert.That(cellX, Is.GreaterThanOrEqualTo(0), "测试地图没有可破坏木箱。");
                FieldInfo blocksField = typeof(BomberMapView).GetField("breakableBlocks", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(blocksField, Is.Not.Null);
                var blocks = blocksField.GetValue(mapView) as Dictionary<int, GameObject>;
                Assert.That(blocks, Is.Not.Null);
                int cellIndex = (cellZ * mapView.Definition.Width) + cellX;
                Assert.That(blocks.TryGetValue(cellIndex, out GameObject block), Is.True);
                Assert.That(block.activeSelf, Is.True);

                mapView.HideBreakable(cellX, cellZ);

                Assert.That(block.activeSelf, Is.False);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        /// <summary>
        /// 验证移动端炸弹按键采用回调缓存边沿，客户端启动时保持屏幕常亮，并即时处理木箱摧毁事件。
        /// </summary>
        [Test]
        public void MobileBattleFlow_BuffersBombPressAndKeepsScreenAwake()
        {
            string inputSource = File.ReadAllText(Path.GetFullPath(BomberInputPath));
            string startupSource = File.ReadAllText(Path.GetFullPath(ClientStartupPath));
            string presentationSource = File.ReadAllText(Path.GetFullPath(BattlePresentationPath));
            string presenterSource = File.ReadAllText(Path.GetFullPath(WindowPresenterPath));
            StringAssert.Contains("placeBombAction.performed += HandlePlaceBombPerformed", inputSource);
            StringAssert.Contains("bool placeBomb = placeBombPressed;", inputSource);
            StringAssert.DoesNotContain("placeBombAction.WasPressedThisFrame()", inputSource);
            StringAssert.Contains("Screen.sleepTimeout = SleepTimeout.NeverSleep;", startupSource);
            StringAssert.Contains("MiniBomberEventBlockDestroyed", presentationSource);
            StringAssert.Contains("MapView?.HideBreakable", presentationSource);
            StringAssert.Contains("MTask.Delay(500)", presenterSource);
            StringAssert.Contains("TryGetTransportRttMs", presenterSource);
            StringAssert.Contains("FPS: {framesPerSecond:F2}  RTT:", presenterSource);
        }

        /// <summary>
        /// 验证 Android Player 保留 YooAsset 动态模型、动画、特效和触屏控件所需类型。
        /// </summary>
        [Test]
        public void AndroidLinkerConfig_PreservesDynamicWorldTypes()
        {
            string linkerConfig = File.ReadAllText(Path.GetFullPath(LinkerConfigPath));
            StringAssert.Contains("UnityEngine.Animator", linkerConfig);
            StringAssert.Contains("UnityEngine.Avatar", linkerConfig);
            StringAssert.Contains("UnityEngine.SkinnedMeshRenderer", linkerConfig);
            StringAssert.Contains("UnityEngine.ParticleSystem", linkerConfig);
            StringAssert.Contains("UnityEngine.InputSystem.OnScreen.OnScreenButton", linkerConfig);
            StringAssert.Contains("UnityEngine.InputSystem.OnScreen.OnScreenStick", linkerConfig);
        }

        /// <summary>
        /// 验证 AOT Bootstrap 在下载完成后显式调用公开保护入口，并静态引用动态角色和粒子所需类型。
        /// </summary>
        [Test]
        public void AndroidNativeTypePreserver_AnchorsDynamicEngineTypes()
        {
            string preserverSource = File.ReadAllText(Path.GetFullPath(NativeTypePreserverPath));
            string bootstrapSource = File.ReadAllText(Path.GetFullPath(BootstrapPath));
            StringAssert.Contains("public static class UnityEngineTypePreserver", preserverSource);
            StringAssert.Contains("public static void ProtectDynamicContentTypes()", preserverSource);
            StringAssert.Contains("Debug.Log(typeof(AnimationClip))", preserverSource);
            StringAssert.Contains("Debug.Log(typeof(Avatar))", preserverSource);
            StringAssert.Contains("Debug.Log(typeof(SkinnedMeshRenderer))", preserverSource);
            StringAssert.Contains("Debug.Log(typeof(ParticleSystem))", preserverSource);
            StringAssert.Contains("Debug.Log(typeof(ParticleSystemRenderer))", preserverSource);
            StringAssert.Contains("AddComponent<SkinnedMeshRenderer>", preserverSource);
            StringAssert.DoesNotContain("[Preserve]", preserverSource);
            StringAssert.Contains("await DownloadAssetsAsync();", bootstrapSource);
            StringAssert.Contains("UnityEngineTypePreserver.ProtectDynamicContentTypes();", bootstrapSource);
            StringAssert.Contains("await LoadAssembliesAsync();", bootstrapSource);
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 按名称查找包含未激活节点在内的后代节点。
        /// </summary>
        /// <param name="root">查找根节点。</param>
        /// <param name="name">目标节点名称。</param>
        /// <returns>匹配节点；不存在时返回空。</returns>
        private static Transform FindDescendant(Transform root, string name)
        {
            Transform[] descendants = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < descendants.Length; i++)
            {
                if (descendants[i].name == name)
                {
                    return descendants[i];
                }
            }

            return null;
        }

        /// <summary>
        /// 按完整类型名查找节点组件，避免测试程序集额外依赖 Input System。
        /// </summary>
        /// <param name="transform">目标节点。</param>
        /// <param name="typeName">组件完整类型名。</param>
        /// <returns>匹配组件；不存在时返回空。</returns>
        private static Component FindComponent(Transform transform, string typeName)
        {
            Component[] components = transform.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i];
                if (component != null && component.GetType().FullName == typeName)
                {
                    return component;
                }
            }

            return null;
        }

        /// <summary>
        /// 按层级路径查找指定场景中的对象。
        /// </summary>
        /// <param name="scene">目标场景。</param>
        /// <param name="path">从场景根开始的层级路径。</param>
        /// <returns>匹配对象；不存在时返回空。</returns>
        private static GameObject FindSceneObject(Scene scene, string path)
        {
            int separatorIndex = path.IndexOf('/');
            string rootName = separatorIndex < 0 ? path : path.Substring(0, separatorIndex);
            string childPath = separatorIndex < 0 ? string.Empty : path.Substring(separatorIndex + 1);
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i].name != rootName)
                {
                    continue;
                }

                if (childPath.Length == 0)
                {
                    return roots[i];
                }

                Transform child = roots[i].transform.Find(childPath);
                return child == null ? null : child.gameObject;
            }

            return null;
        }

        /// <summary>
        /// 在指定场景全部根节点中查找首个目标组件。
        /// </summary>
        /// <typeparam name="T">需要查找的 Unity 组件类型。</typeparam>
        /// <param name="scene">目标场景。</param>
        /// <returns>找到的组件；不存在时返回空。</returns>
        private static T FindSceneComponent<T>(Scene scene) where T : Component
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int index = 0; index < roots.Length; index++)
            {
                T component = roots[index].GetComponentInChildren<T>(true);
                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }

        #endregion
    }
}
