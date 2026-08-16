using System;
using System.IO;
using MiniCore.Demo.MiniBomber;
using MiniCore.Demo.MiniBomber.Unity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MiniCore.EditorTools.Demos.MiniBomber
{
    /// <summary>
    /// 创建 MiniBomber 默认配置、17×13 地图和无业务 Canvas 的场景骨架。
    /// </summary>
    public static class MiniBomberDemoAssetGenerator
    {
        #region Private 私有成员

        private const string ConfigRoot = "Assets/AssetRes/Demos/MiniBomber/Config"; // Demo 配置资源目录。
        private const string MapRoot = "Assets/AssetRes/Demos/MiniBomber/Maps"; // Demo 地图资源目录。
        private const string PrefabRoot = "Assets/AssetRes/Demos/MiniBomber/Prefabs"; // Demo 世界 Prefab 目录。
        private const string UIRoot = "Assets/AssetRes/Demos/MiniBomber/UI"; // Demo UI Prefab 目录。
        private const string SceneRoot = "Assets/Scenes/Demos/MiniBomber"; // Demo 场景目录。
        private const string ServerBootstrapScenePath = SceneRoot + "/ServerBootstrapScene.unity"; // Dedicated Server 独立启动场景。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 幂等创建 Demo 配置和场景骨架；不覆盖已有用户场景或资产。
        /// </summary>
        [MenuItem("MiniCore/Demos/MiniBomber/Create Default Assets", priority = 2300)]
        public static void Generate()
        {
            EnsureFolder(ConfigRoot);
            EnsureFolder(MapRoot);
            EnsureFolder(PrefabRoot);
            EnsureFolder(UIRoot);
            EnsureFolder(SceneRoot);
            CreateAssetIfMissing<MiniBomberRuntimeConfig>($"{ConfigRoot}/MiniBomberRuntimeConfig.asset");
            CreateAssetIfMissing<MiniBomberClientNetworkProfile>($"{ConfigRoot}/MiniBomberClientNetworkProfile.asset");
            CreateAssetIfMissing<MiniBomberRuleConfig>($"{ConfigRoot}/MiniBomberRuleConfig.asset");
            CreateDefaultMapIfMissing($"{MapRoot}/MiniBomberDefaultMap.asset");
            CreateSimpleSceneIfMissing($"{SceneRoot}/LoginScene.unity", "LoginScene");
            CreateSimpleSceneIfMissing($"{SceneRoot}/LobbyScene.unity", "LobbyScene");
            CreateBattleSceneIfMissing($"{SceneRoot}/BattleScene.unity");
            CreateServerBootstrapSceneIfMissing();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("MiniBomber 默认配置、地图和场景骨架已创建；接下来按 Docs/Demos/MiniBomber.md 制作 Prefab 与 UI。");
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 创建不存在的 ScriptableObject 资产。
        /// </summary>
        /// <typeparam name="T">资产类型。</typeparam>
        /// <param name="path">资产路径。</param>
        private static void CreateAssetIfMissing<T>(string path) where T : ScriptableObject
        {
            if (AssetDatabase.LoadAssetAtPath<T>(path) != null)
            {
                return;
            }

            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<T>(), path);
        }

        /// <summary>
        /// 创建默认经典炸弹人地图资产。
        /// </summary>
        /// <param name="path">地图资产路径。</param>
        private static void CreateDefaultMapIfMissing(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<BomberMapDefinition>(path) != null)
            {
                return;
            }

            BomberMapDefinition map = ScriptableObject.CreateInstance<BomberMapDefinition>();
            for (int z = 0; z < map.Height; z++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    bool boundary = x == 0 || z == 0 || x == map.Width - 1 || z == map.Height - 1;
                    bool pillar = (x & 1) == 0 && (z & 1) == 0;
                    bool spawnClear = IsSpawnClearCell(x, z, map.Width, map.Height);
                    map.SetCell(x, z, boundary || pillar
                        ? MiniBomberCellType.Solid
                        : !spawnClear && ((x + (z * 3)) % 4 != 0) ? MiniBomberCellType.Breakable : MiniBomberCellType.Road);
                }
            }

            AssetDatabase.CreateAsset(map, path);
        }

        /// <summary>
        /// 创建只有环境、相机和灯光的登录或大厅场景。
        /// </summary>
        /// <param name="path">场景路径。</param>
        /// <param name="sceneName">场景名称。</param>
        private static void CreateSimpleSceneIfMissing(string path, string sceneName)
        {
            if (File.Exists(path))
            {
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = sceneName;
            new GameObject("Environment");
            CreateCamera("MainCamera", null);
            CreateLighting();
            EditorSceneManager.SaveScene(scene, path);
        }

        /// <summary>
        /// 创建无 Canvas、无 EventSystem 的 BattleScene 引用骨架。
        /// </summary>
        /// <param name="path">战斗场景路径。</param>
        private static void CreateBattleSceneIfMissing(string path)
        {
            if (File.Exists(path))
            {
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "BattleScene";
            GameObject environment = new GameObject("Environment");
            new GameObject("Ground").transform.SetParent(environment.transform, false);
            new GameObject("Decorations").transform.SetParent(environment.transform, false);
            GameObject gameplay = new GameObject("GameplayRoot", typeof(BomberBattleSceneBinding));
            GameObject map = new GameObject("MapRoot", typeof(BomberMapView));
            map.transform.SetParent(gameplay.transform, false);
            new GameObject("SolidBlockRoot").transform.SetParent(map.transform, false);
            new GameObject("BreakableBlockRoot").transform.SetParent(map.transform, false);
            new GameObject("PickupRoot").transform.SetParent(map.transform, false);
            new GameObject("PlayerRoot").transform.SetParent(gameplay.transform, false);
            new GameObject("BombRoot").transform.SetParent(gameplay.transform, false);
            new GameObject("EffectRoot").transform.SetParent(gameplay.transform, false);
            GameObject cameraRig = new GameObject("CameraRig", typeof(BomberCameraController));
            CreateCamera("MainCamera", cameraRig.transform);
            CreateLighting();
            EditorSceneManager.SaveScene(scene, path);
        }

        /// <summary>
        /// 创建只含 HotUpdate Bootstrap 的 Dedicated Server 启动场景。
        /// </summary>
        private static void CreateServerBootstrapSceneIfMissing()
        {
            if (File.Exists(ServerBootstrapScenePath))
            {
                return;
            }

            Type bootstrapType = Type.GetType("UpdateMainWindow, Project.Bootstrap", false);
            if (bootstrapType == null || !typeof(Component).IsAssignableFrom(bootstrapType))
            {
                throw new InvalidOperationException("未找到 Project.Bootstrap.UpdateMainWindow，无法创建 Dedicated Server 启动场景。");
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "ServerBootstrapScene";
            GameObject bootstrapObject = new GameObject("ServerBootstrap");
            Component bootstrap = bootstrapObject.AddComponent(bootstrapType);
            SerializedObject serializedBootstrap = new SerializedObject(bootstrap);
            SetSerializedValue(serializedBootstrap, "hotUpdateDllPath", "HotUpdate");
            SetSerializedValue(serializedBootstrap, "packageName", "DefaultPackage");
            SetSerializedValue(serializedBootstrap, "downloadMaxNum", 10);
            SetSerializedValue(serializedBootstrap, "failedTryAgain", 3);
            SerializedProperty mode = serializedBootstrap.FindProperty("bundlePackageMode");
            if (mode != null)
            {
                mode.enumValueIndex = 1;
            }

            serializedBootstrap.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene, ServerBootstrapScenePath);
        }

        /// <summary>
        /// 为 Bootstrap 组件写入字符串序列化字段。
        /// </summary>
        /// <param name="serializedObject">目标 Bootstrap 组件。</param>
        /// <param name="propertyName">字段名称。</param>
        /// <param name="value">字段值。</param>
        private static void SetSerializedValue(SerializedObject serializedObject, string propertyName, string value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.stringValue = value;
            }
        }

        /// <summary>
        /// 为 Bootstrap 组件写入整数序列化字段。
        /// </summary>
        /// <param name="serializedObject">目标 Bootstrap 组件。</param>
        /// <param name="propertyName">字段名称。</param>
        /// <param name="value">字段值。</param>
        private static void SetSerializedValue(SerializedObject serializedObject, string propertyName, int value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.intValue = value;
            }
        }

        /// <summary>
        /// 创建主相机节点。
        /// </summary>
        /// <param name="name">节点名称。</param>
        /// <param name="parent">可选父节点。</param>
        private static void CreateCamera(string name, Transform parent)
        {
            GameObject cameraObject = new GameObject(name, typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(parent, false);
            cameraObject.transform.position = new Vector3(8.5f, 12f, -2f);
            cameraObject.transform.rotation = Quaternion.Euler(55f, 0f, 0f);
        }

        /// <summary>
        /// 创建默认方向光。
        /// </summary>
        private static void CreateLighting()
        {
            GameObject lighting = new GameObject("Lighting");
            GameObject lightObject = new GameObject("Directional Light", typeof(Light));
            lightObject.transform.SetParent(lighting.transform, false);
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            lightObject.GetComponent<Light>().type = LightType.Directional;
        }

        /// <summary>
        /// 判断地图格是否属于四角出生安全区。
        /// </summary>
        /// <param name="x">横向格坐标。</param>
        /// <param name="z">纵向格坐标。</param>
        /// <param name="width">地图宽度。</param>
        /// <param name="height">地图高度。</param>
        /// <returns>属于安全区时返回 true。</returns>
        private static bool IsSpawnClearCell(int x, int z, int width, int height)
        {
            int right = width - 2;
            int top = height - 2;
            return (x <= 2 && z <= 2) || (x >= right - 1 && z <= 2) || (x <= 2 && z >= top - 1) || (x >= right - 1 && z >= top - 1);
        }

        /// <summary>
        /// 递归创建 Unity 资产目录。
        /// </summary>
        /// <param name="path">Assets 开头的目录。</param>
        private static void EnsureFolder(string path)
        {
            string[] segments = path.Split('/');
            string current = segments[0];
            for (int index = 1; index < segments.Length; index++)
            {
                string next = current + "/" + segments[index];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[index]);
                }

                current = next;
            }
        }

        #endregion
    }
}
