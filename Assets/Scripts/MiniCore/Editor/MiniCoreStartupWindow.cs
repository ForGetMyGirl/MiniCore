using System;
using System.Collections.Generic;
using System.Reflection;
using MiniCore.Model;
using MiniCore.Unity;
using UnityEditor;
using UnityEngine;

namespace MiniCore.EditorTools
{
    /// <summary>
    /// MiniCore 项目启动模块的可视化配置窗口。
    /// 开发者只需勾选标记模块、填写 Args 覆盖值并生成，即可得到稳定的 HotUpdate 启动代码。
    /// </summary>
    public sealed class MiniCoreStartupWindow : EditorWindow
    {
        #region Private 私有成员

        private readonly HashSet<string> expandedModules = new HashSet<string>(); // 当前展开参数面板的模块类型名。
        private Vector2 scrollPosition; // 窗口滚动位置。
        private MiniCoreStartupSettings settings; // 当前项目启动配置资源。

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 打开 MiniCore 项目启动配置窗口。
        /// </summary>
        [MenuItem("MiniCore/项目启动配置", priority = 1900)]
        private static void Open()
        {
            GetWindow<MiniCoreStartupWindow>("MiniCore 启动配置").Show();
        }

        /// <summary>
        /// 窗口启用时加载并同步项目启动配置。
        /// </summary>
        private void OnEnable()
        {
            settings = MiniCoreStartupCodeGenerator.GetOrCreateSettings();
        }

        /// <summary>
        /// 绘制模块勾选、参数编辑和生成操作界面。
        /// </summary>
        private void OnGUI()
        {
            EnsureSettings();
            List<MiniCoreStartupCodeGenerator.StartupModuleInfo> modules = MiniCoreStartupCodeGenerator.DiscoverModules();
            GUILayout.Label("MiniCore 项目启动配置", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("每个启动模块都可独立勾选 Client 和 Server。生成器会在对应目标中补齐 DependsOn 并按依赖顺序 Pin。参数的“覆盖默认值”勾选后可填写启动值；未勾选时使用 Args 类中的默认值。", MessageType.Info);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            for (int i = 0; i < modules.Count; i++)
            {
                DrawModule(modules[i]);
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("重新扫描模块"))
            {
                MiniCoreStartupCodeGenerator.SynchronizeSettings(settings);
                AssetDatabase.SaveAssets();
            }

            if (GUILayout.Button("保存启动参数并生成代码", GUILayout.Height(28)))
            {
                GenerateStartupCode();
            }
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 确保当前窗口持有有效的启动配置资源。
        /// </summary>
        private void EnsureSettings()
        {
            if (settings == null)
            {
                settings = MiniCoreStartupCodeGenerator.GetOrCreateSettings();
            }
        }

        /// <summary>
        /// 绘制单个启动模块的目标勾选与初始化参数。
        /// </summary>
        /// <param name="module">要显示的启动模块描述。</param>
        private void DrawModule(MiniCoreStartupCodeGenerator.StartupModuleInfo module)
        {
            MiniCoreStartupModuleSettings moduleSettings = FindModuleSettings(module.Type);
            if (moduleSettings == null)
            {
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(module.Attribute.DisplayName, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            GUILayout.Label(module.Type.FullName, EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            DrawTargetToggle("Client", ref moduleSettings.EnableClient);
            DrawTargetToggle("Server", ref moduleSettings.EnableServer);
            EditorGUILayout.EndHorizontal();

            if (module.Attribute.DependsOn != null && module.Attribute.DependsOn.Length > 0)
            {
                EditorGUILayout.LabelField("依赖", string.Join("、", Array.ConvertAll(module.Attribute.DependsOn, item => item.Name)), EditorStyles.miniLabel);
            }

            if (module.ArgsType != null)
            {
                bool expanded = expandedModules.Contains(module.Type.AssemblyQualifiedName);
                bool nextExpanded = EditorGUILayout.Foldout(expanded, $"启动参数 ({module.ArgsType.Name})", true);
                if (nextExpanded)
                {
                    expandedModules.Add(module.Type.AssemblyQualifiedName);
                    DrawArguments(module, moduleSettings);
                }
                else
                {
                    expandedModules.Remove(module.Type.AssemblyQualifiedName);
                }
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制一个启动目标勾选框。
        /// </summary>
        /// <param name="label">显示给开发者的目标名称。</param>
        /// <param name="value">对应的启用状态。</param>
        private static void DrawTargetToggle(string label, ref bool value)
        {
            value = EditorGUILayout.ToggleLeft(label, value, GUILayout.Width(80));
        }

        /// <summary>
        /// 绘制一个模块的 Args 覆盖成员。
        /// </summary>
        /// <param name="module">拥有 Args 类型的模块描述。</param>
        /// <param name="moduleSettings">模块的可持久化配置。</param>
        private void DrawArguments(MiniCoreStartupCodeGenerator.StartupModuleInfo module, MiniCoreStartupModuleSettings moduleSettings)
        {
            List<MemberInfo> members = MiniCoreStartupCodeGenerator.GetEditableArgumentMembers(module.ArgsType);
            if (members.Count == 0)
            {
                EditorGUILayout.HelpBox("当前 Args 没有可编辑的 public 字段或可写属性。", MessageType.None);
                return;
            }

            EditorGUILayout.HelpBox("勾选“覆盖默认值”后填写此模块的启动值；未勾选时，生成代码使用 Args 类中定义的默认值。", MessageType.None);
            EditorGUI.indentLevel++;
            for (int i = 0; i < members.Count; i++)
            {
                MemberInfo member = members[i];
                MiniCoreStartupArgumentSettings argument = FindArgumentSettings(moduleSettings, member.Name);
                if (argument == null)
                {
                    continue;
                }

                EditorGUILayout.BeginHorizontal();
                bool overrideDefault = EditorGUILayout.ToggleLeft("覆盖默认值", !argument.UseCodeDefault, GUILayout.Width(110));
                argument.UseCodeDefault = !overrideDefault;
                EditorGUILayout.LabelField(member.Name, GUILayout.Width(190));
                if (argument.UseCodeDefault)
                {
                    EditorGUILayout.LabelField("使用 Args 代码默认值", EditorStyles.miniLabel);
                }
                else
                {
                    argument.Value = DrawArgumentValue(GetMemberType(member), argument.Value);
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUI.indentLevel--;
        }

        /// <summary>
        /// 依据成员类型绘制参数值编辑控件。
        /// </summary>
        /// <param name="type">参数成员类型。</param>
        /// <param name="currentValue">当前保存的字符串值。</param>
        /// <returns>编辑后的字符串值。</returns>
        private static string DrawArgumentValue(Type type, string currentValue)
        {
            currentValue = currentValue ?? string.Empty;
            if (type == typeof(bool))
            {
                bool value = bool.TryParse(currentValue, out bool parsedValue) && parsedValue;
                return EditorGUILayout.Toggle(value).ToString();
            }

            if (type == typeof(int))
            {
                int.TryParse(currentValue, out int value);
                return EditorGUILayout.IntField(value).ToString();
            }

            if (type == typeof(long))
            {
                long.TryParse(currentValue, out long value);
                return EditorGUILayout.LongField(value).ToString();
            }

            if (type == typeof(float) || type == typeof(double))
            {
                float.TryParse(currentValue, out float value);
                return EditorGUILayout.FloatField(value).ToString(System.Globalization.CultureInfo.InvariantCulture);
            }

            if (type.IsEnum)
            {
                object selected = Enum.TryParse(type, currentValue, false, out object enumValue) ? enumValue : Enum.GetValues(type).GetValue(0);
                return EditorGUILayout.EnumPopup((Enum)selected).ToString();
            }

            return EditorGUILayout.TextField(currentValue);
        }

        /// <summary>
        /// 获取模块配置中与指定组件类型对应的条目。
        /// </summary>
        /// <param name="componentType">启动组件类型。</param>
        /// <returns>对应的持久化模块配置；不存在时返回 null。</returns>
        private MiniCoreStartupModuleSettings FindModuleSettings(Type componentType)
        {
            for (int i = 0; i < settings.Modules.Count; i++)
            {
                MiniCoreStartupModuleSettings module = settings.Modules[i];
                if (module != null && string.Equals(module.AssemblyQualifiedTypeName, componentType.AssemblyQualifiedName, StringComparison.Ordinal))
                {
                    return module;
                }
            }

            return null;
        }

        /// <summary>
        /// 获取模块参数配置中与指定成员名对应的条目。
        /// </summary>
        /// <param name="module">模块持久化配置。</param>
        /// <param name="memberName">Args 成员名称。</param>
        /// <returns>对应参数配置；不存在时返回 null。</returns>
        private static MiniCoreStartupArgumentSettings FindArgumentSettings(MiniCoreStartupModuleSettings module, string memberName)
        {
            for (int i = 0; i < module.Arguments.Count; i++)
            {
                MiniCoreStartupArgumentSettings argument = module.Arguments[i];
                if (argument != null && string.Equals(argument.MemberName, memberName, StringComparison.Ordinal))
                {
                    return argument;
                }
            }

            return null;
        }

        /// <summary>
        /// 获取字段或属性的 CLR 类型。
        /// </summary>
        /// <param name="member">反射成员信息。</param>
        /// <returns>成员声明类型。</returns>
        private static Type GetMemberType(MemberInfo member)
        {
            return member is FieldInfo field ? field.FieldType : ((PropertyInfo)member).PropertyType;
        }

        /// <summary>
        /// 保存配置并生成启动源代码，同时将可读结果反馈给开发者。
        /// </summary>
        private void GenerateStartupCode()
        {
            EditorUtility.SetDirty(settings);
            if (MiniCoreStartupCodeGenerator.Generate(settings, out string error))
            {
                EditorUtility.DisplayDialog("MiniCore 启动配置", "启动配置已保存，已生成 HotUpdate 启动代码。", "确定");
                return;
            }

            EditorUtility.DisplayDialog("MiniCore 启动配置", "生成失败：\n" + error, "确定");
        }

        #endregion
    }
}
