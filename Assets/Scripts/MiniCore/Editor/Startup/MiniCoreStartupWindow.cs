using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MiniCore.Model;
using MiniCore.Unity;
using UnityEditor;
using UnityEngine;

namespace MiniCore.EditorTools
{
    /// <summary>
    /// MiniCore 项目启动模块的可视化配置窗口。
    /// 开发者按服务接口选择唯一 Provider、填写 Args 覆盖值并生成，即可得到稳定的 HotUpdate 启动代码。
    /// </summary>
    public sealed class MiniCoreStartupWindow : EditorWindow
    {
        #region Private 私有成员

        private static readonly Color[] ServiceGroupColors =
        {
            new Color(0.82f, 0.91f, 1.00f),
            new Color(0.86f, 0.96f, 0.86f),
            new Color(0.96f, 0.91f, 0.80f),
            new Color(0.93f, 0.86f, 0.98f)
        }; // 接口分组交替使用的柔和底色。
        private readonly HashSet<string> expandedServices = new HashSet<string>(); // 当前展开参数面板的服务类型名。
        private Vector2 scrollPosition; // 窗口滚动位置。
        private Vector2 catalogScrollPosition; // 项目能力目录滚动位置。
        private bool showServiceCatalog = true; // 是否展开服务目录。
        private bool showModuleCatalog = true; // 是否展开模块目录。
        private bool showComponentCatalog = true; // 是否展开普通组件目录。
        private MiniCoreStartupSettings settings; // 当前项目启动配置资源。

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
            List<MiniCoreStartupCodeGenerator.AppServiceInfo> services = MiniCoreStartupCodeGenerator.DiscoverAppServices();
            List<MiniCoreStartupCodeGenerator.AppModuleInfo> appModules = MiniCoreStartupCodeGenerator.DiscoverAppModules();
            List<Type> components = DiscoverOrdinaryComponents(services, appModules);
            GUILayout.Label("MiniCore 项目启动配置", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("每个 AppService 接口选择一个 Provider；选择“不启用”时该接口不会注册。普通业务组件继续由 GameStartup 按流程创建。", MessageType.Info);

            float catalogWidth = Mathf.Clamp(position.width * 0.30f, 340f, 500f);
            float configurationWidth = Mathf.Max(760f, position.width - catalogWidth - 18f);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical(GUILayout.Width(configurationWidth));
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            DrawServices(services);
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            DrawProjectCatalog(services, appModules, components, catalogWidth);
            EditorGUILayout.EndHorizontal();

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
        /// 绘制项目当前可调用能力的只读目录。
        /// 目录不会变更启动勾选或生成配置，仅用于开发阶段发现功能。
        /// </summary>
        /// <param name="services">已发现的应用服务。</param>
        /// <param name="appModules">已发现的应用模块。</param>
        /// <param name="components">已标注能力说明的普通组件。</param>
        /// <param name="catalogWidth">右侧能力目录宽度。</param>
        private void DrawProjectCatalog(
            List<MiniCoreStartupCodeGenerator.AppServiceInfo> services,
            List<MiniCoreStartupCodeGenerator.AppModuleInfo> appModules,
            List<Type> components,
            float catalogWidth)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(catalogWidth));
            GUILayout.Label("项目能力目录（只读）", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("这里列出当前项目可发现的 Service、AppModule 与已标注职责的普通 AComponent，便于查找可调用能力；不会修改启动配置，也不显示框架内部实现与 GameStartup。", MessageType.None);

            catalogScrollPosition = EditorGUILayout.BeginScrollView(catalogScrollPosition);
            showServiceCatalog = EditorGUILayout.Foldout(showServiceCatalog, $"Service ({services.Count})", true);
            if (showServiceCatalog)
            {
                for (int index = 0; index < services.Count; index++)
                {
                    MiniCoreStartupCodeGenerator.AppServiceInfo service = services[index];
                    DrawCatalogItem(
                        service.Attribute.DisplayName,
                        service.Type,
                        GetCatalogDescription(service.Attribute.Description),
                        $"服务接口：{string.Join("、", service.Attribute.ServiceTypes.Select(item => item.Name))}");
                }
            }

            showModuleCatalog = EditorGUILayout.Foldout(showModuleCatalog, $"AppModule ({appModules.Count})", true);
            if (showModuleCatalog)
            {
                for (int index = 0; index < appModules.Count; index++)
                {
                    MiniCoreStartupCodeGenerator.AppModuleInfo module = appModules[index];
                    string key = string.IsNullOrEmpty(module.Attribute.Key) ? "默认实现" : module.Attribute.Key;
                    DrawCatalogItem(
                        module.Type.Name,
                        module.Type,
                        GetCatalogDescription(module.Attribute.Description),
                        $"模块接口：{module.Attribute.ModuleType.Name} / Key: {key}");
                }
            }

            showComponentCatalog = EditorGUILayout.Foldout(showComponentCatalog, $"普通 AComponent ({components.Count})", true);
            if (showComponentCatalog)
            {
                for (int index = 0; index < components.Count; index++)
                {
                    Type component = components[index];
                    ComponentCatalogAttribute attribute = component.GetCustomAttribute<ComponentCatalogAttribute>();
                    DrawCatalogItem(attribute.DisplayName, component, GetCatalogDescription(attribute.Description));
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制能力目录中的单个只读条目。
        /// </summary>
        /// <param name="title">条目显示名称。</param>
        /// <param name="type">条目具体类型。</param>
        /// <param name="description">条目面向开发者的具体职责说明。</param>
        /// <param name="detail">条目的接口或 Key 等补充信息。</param>
        private static void DrawCatalogItem(string title, Type type, string description, string detail = null)
        {
            EditorGUILayout.BeginVertical(EditorStyles.inspectorDefaultMargins);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(description, EditorStyles.wordWrappedMiniLabel);
            if (!string.IsNullOrWhiteSpace(detail))
            {
                EditorGUILayout.LabelField(detail, EditorStyles.miniLabel);
            }

            EditorGUILayout.SelectableLabel(type.FullName, EditorStyles.miniLabel, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 获取用于能力目录显示的职责说明。
        /// </summary>
        /// <param name="description">标记中声明的职责说明。</param>
        /// <returns>可直接显示的职责说明。</returns>
        private static string GetCatalogDescription(string description)
        {
            return string.IsNullOrWhiteSpace(description) ? "未填写用途说明。" : description;
        }

        /// <summary>
        /// 收集项目程序集内已标注具体职责的普通 AComponent 类型。
        /// 未标注的类型以及 GameStartup、网络会话等框架内部装配实现不会出现在能力目录中。
        /// </summary>
        /// <param name="services">已发现的应用服务。</param>
        /// <param name="appModules">已发现的应用模块。</param>
        /// <returns>按完整类型名稳定排序的已标注组件集合。</returns>
        private static List<Type> DiscoverOrdinaryComponents(
            List<MiniCoreStartupCodeGenerator.AppServiceInfo> services,
            List<MiniCoreStartupCodeGenerator.AppModuleInfo> appModules)
        {
            var classifiedTypes = new HashSet<Type>();
            for (int index = 0; index < services.Count; index++)
            {
                classifiedTypes.Add(services[index].Type);
            }

            for (int index = 0; index < appModules.Count; index++)
            {
                classifiedTypes.Add(appModules[index].Type);
            }

            var result = new List<Type>();
            foreach (Type type in TypeCache.GetTypesDerivedFrom<AComponent>())
            {
                if (type == null || type.IsAbstract || classifiedTypes.Contains(type) || !IsProjectRuntimeType(type) || IsInfrastructureComponent(type) || type.GetCustomAttribute<ComponentCatalogAttribute>() == null)
                {
                    continue;
                }

                result.Add(type);
            }

            result.Sort((left, right) => string.CompareOrdinal(left.FullName, right.FullName));
            return result;
        }

        /// <summary>
        /// 判断类型是否属于项目的可运行 MiniCore 或 Bootstrap 程序集。
        /// </summary>
        /// <param name="type">待检查的类型。</param>
        /// <returns>类型应出现在项目能力目录时返回 true。</returns>
        private static bool IsProjectRuntimeType(Type type)
        {
            string assemblyName = type.Assembly.GetName().Name;
            return assemblyName.StartsWith("MiniCore.", StringComparison.Ordinal) &&
                   !assemblyName.EndsWith(".Editor", StringComparison.Ordinal) &&
                   !assemblyName.EndsWith(".EditorTests", StringComparison.Ordinal);
        }

        /// <summary>
        /// 判断普通组件是否为框架内部装配实现，而非业务直接使用的能力。
        /// </summary>
        /// <param name="type">待检查的组件类型。</param>
        /// <returns>应从业务能力目录隐藏时返回 true。</returns>
        private static bool IsInfrastructureComponent(Type type)
        {
            return typeof(AGameStartup).IsAssignableFrom(type) || typeof(MiniCore.Core.INetworkSessionService).IsAssignableFrom(type);
        }

        /// <summary>
        /// 按 AppService 接口绘制单选 Provider 配置。
        /// </summary>
        /// <param name="services">已发现服务实现。</param>
        private void DrawServices(List<MiniCoreStartupCodeGenerator.AppServiceInfo> services)
        {
            GUILayout.Label("AppService", EditorStyles.boldLabel);
            List<AppServiceContractGroup> groups = AppServiceProviderConfiguration.BuildGroups(services, settings);
            for (int groupIndex = 0; groupIndex < groups.Count; groupIndex++)
            {
                AppServiceContractGroup group = groups[groupIndex];
                Color previousBackgroundColor = GUI.backgroundColor;
                GUI.backgroundColor = ServiceGroupColors[groupIndex % ServiceGroupColors.Length];
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUI.backgroundColor = previousBackgroundColor;
                EditorGUILayout.LabelField(group.Contract.Name, EditorStyles.boldLabel);
                EditorGUILayout.SelectableLabel(group.Contract.FullName, EditorStyles.miniLabel, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                DrawProviderPopup(group, services);

                if (group.HasConflict)
                {
                    EditorGUILayout.HelpBox(
                        $"当前资产同时启用了 {string.Join("、", group.EnabledProviders.Select(item => item.Type.Name))}。请选择唯一 Provider 后才能生成启动代码。",
                        MessageType.Error);
                }

                MiniCoreStartupCodeGenerator.AppServiceInfo selectedProvider = group.SelectedProvider;
                if (selectedProvider != null)
                {
                    DrawSelectedProvider(group, selectedProvider, groups);
                    MiniCoreAppServiceSettings serviceSettings = FindServiceSettings(selectedProvider.Type);
                    if (selectedProvider.ArgsType != null && serviceSettings != null)
                    {
                        string expansionKey = group.Contract.AssemblyQualifiedName + "|" + selectedProvider.Type.AssemblyQualifiedName;
                        bool expanded = expandedServices.Contains(expansionKey);
                        bool nextExpanded = EditorGUILayout.Foldout(expanded, $"启动参数 ({selectedProvider.ArgsType.Name})", true);
                        if (nextExpanded)
                        {
                            expandedServices.Add(expansionKey);
                            DrawArguments(selectedProvider.ArgsType, serviceSettings.Arguments, selectedProvider.Attribute.DisplayName);
                        }
                        else
                        {
                            expandedServices.Remove(expansionKey);
                        }
                    }
                }

                EditorGUILayout.EndVertical();
            }
        }

        /// <summary>
        /// 绘制接口级 Provider 单选下拉框并应用选择。
        /// </summary>
        /// <param name="group">当前接口分组。</param>
        /// <param name="services">全部服务实现。</param>
        private void DrawProviderPopup(
            AppServiceContractGroup group,
            List<MiniCoreStartupCodeGenerator.AppServiceInfo> services)
        {
            int optionOffset = group.HasConflict ? 2 : 1;
            var options = new GUIContent[group.Providers.Count + optionOffset];
            int currentIndex = 0;
            if (group.HasConflict)
            {
                options[0] = new GUIContent("配置冲突（请选择）");
                options[1] = new GUIContent("不启用");
            }
            else
            {
                options[0] = new GUIContent("不启用");
            }

            for (int providerIndex = 0; providerIndex < group.Providers.Count; providerIndex++)
            {
                MiniCoreStartupCodeGenerator.AppServiceInfo provider = group.Providers[providerIndex];
                int optionIndex = providerIndex + optionOffset;
                options[optionIndex] = new GUIContent($"{provider.Attribute.DisplayName} — {provider.Type.Name}");
                if (!group.HasConflict && group.SelectedProvider?.Type == provider.Type)
                {
                    currentIndex = optionIndex;
                }
            }

            EditorGUI.BeginChangeCheck();
            int nextIndex = EditorGUILayout.Popup(new GUIContent("Provider"), currentIndex, options);
            if (!EditorGUI.EndChangeCheck())
            {
                return;
            }

            if (group.HasConflict && nextIndex == 0)
            {
                return;
            }

            MiniCoreStartupCodeGenerator.AppServiceInfo selectedProvider = nextIndex < optionOffset ? null : group.Providers[nextIndex - optionOffset];
            AppServiceProviderConfiguration.SelectProvider(settings, services, group.Contract, selectedProvider);
            EditorUtility.SetDirty(settings);
        }

        /// <summary>
        /// 绘制当前接口所选 Provider 的说明、运行目标和依赖诊断。
        /// </summary>
        /// <param name="group">当前接口分组。</param>
        /// <param name="provider">唯一选中的 Provider。</param>
        /// <param name="groups">全部接口分组。</param>
        private static void DrawSelectedProvider(
            AppServiceContractGroup group,
            MiniCoreStartupCodeGenerator.AppServiceInfo provider,
            List<AppServiceContractGroup> groups)
        {
            EditorGUILayout.Space(2f);
            EditorGUILayout.LabelField("当前实现", provider.Attribute.DisplayName, EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel(provider.Type.FullName, EditorStyles.miniLabel, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            EditorGUILayout.LabelField("描述", GetCatalogDescription(provider.Attribute.Description), EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.LabelField("提供接口", string.Join("、", provider.Attribute.ServiceTypes.Select(item => item.Name)), EditorStyles.miniLabel);
            EditorGUILayout.LabelField("运行目标", provider.Attribute.RunInBatchMode ? "普通客户端 + BatchMode" : "仅普通客户端（BatchMode 跳过）", EditorStyles.miniLabel);

            Type[] dependencies = provider.Attribute.RequiresServices ?? Array.Empty<Type>();
            if (dependencies.Length > 0)
            {
                EditorGUILayout.LabelField("依赖", string.Join("、", dependencies.Select(item => item.Name)), EditorStyles.miniLabel);
                List<Type> missingDependencies = AppServiceProviderConfiguration.GetMissingDependencies(provider, groups);
                if (missingDependencies.Count > 0)
                {
                    EditorGUILayout.HelpBox(
                        $"缺少依赖 Provider：{string.Join("、", missingDependencies.Select(item => item.Name))}。请在对应接口分组中手动选择，生成器不会自动启用依赖。",
                        MessageType.Warning);
                }
            }

            if (provider.Attribute.ServiceTypes.Length > 1)
            {
                EditorGUILayout.HelpBox($"该 Provider 同时提供多个接口；在“{group.Contract.Name}”分组中的选择会同步到它声明的其他接口。", MessageType.None);
            }
        }

        /// <summary>
        /// 绘制启动模块或 AppService 的 Args 覆盖成员。
        /// </summary>
        /// <param name="argsType">启动参数类型。</param>
        /// <param name="arguments">对应类型的持久化参数配置。</param>
        /// <param name="ownerName">当前参数所属模块或服务的显示名称。</param>
        private void DrawArguments(Type argsType, List<MiniCoreStartupArgumentSettings> arguments, string ownerName)
        {
            List<MemberInfo> members = MiniCoreStartupCodeGenerator.GetEditableArgumentMembers(argsType);
            if (members.Count == 0)
            {
                EditorGUILayout.HelpBox("当前 Args 没有可编辑的 public 字段或可写属性。", MessageType.None);
                return;
            }

            EditorGUILayout.HelpBox($"勾选“覆盖默认值”后填写“{ownerName}”的启动值；未勾选时，生成代码使用 Args 类中定义的默认值。", MessageType.None);
            EditorGUI.indentLevel++;
            for (int i = 0; i < members.Count; i++)
            {
                MemberInfo member = members[i];
                MiniCoreStartupArgumentSettings argument = FindArgumentSettings(arguments, member.Name);
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
        /// 获取指定 AppService 实现的持久化配置。
        /// </summary>
        /// <param name="serviceType">具体服务实现类型。</param>
        /// <returns>对应服务设置；不存在时返回 null。</returns>
        private MiniCoreAppServiceSettings FindServiceSettings(Type serviceType)
        {
            for (int index = 0; index < settings.Services.Count; index++)
            {
                MiniCoreAppServiceSettings service = settings.Services[index];
                if (service != null && string.Equals(service.AssemblyQualifiedTypeName, serviceType.AssemblyQualifiedName, StringComparison.Ordinal))
                {
                    return service;
                }
            }

            return null;
        }

        /// <summary>
        /// 获取参数配置中与指定成员名对应的条目。
        /// </summary>
        /// <param name="arguments">模块或服务的参数配置集合。</param>
        /// <param name="memberName">Args 成员名称。</param>
        /// <returns>对应参数配置；不存在时返回 null。</returns>
        private static MiniCoreStartupArgumentSettings FindArgumentSettings(List<MiniCoreStartupArgumentSettings> arguments, string memberName)
        {
            for (int i = 0; i < arguments.Count; i++)
            {
                MiniCoreStartupArgumentSettings argument = arguments[i];
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
