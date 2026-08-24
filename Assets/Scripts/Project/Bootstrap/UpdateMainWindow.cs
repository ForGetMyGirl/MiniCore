using MiniCore.Threading;
using System;
using System.Reflection;
using UnityEngine;
using YooAsset;
using MiniCore.Bootstrap;
using MiniCore.Core;
using MiniCore.Model;
using MiniCore.Unity;

/// <summary>
/// 初始化 YooAsset、加载 AOT 元数据与全部热更新程序集，并调用项目启动入口。
/// </summary>
public sealed class UpdateMainWindow : AMTaskBehaviour
{
    #region UnityProperty Unity 引用属性

    [SerializeField]
    private BundlePackageMode bundlePackageMode; // YooAsset 运行模式。

    /// <summary>
    /// 默认加载的 YooAsset 资源包名称。
    /// </summary>
    [Tooltip("热更新包名")]
    public string packageName;

    /// <summary>
    /// Host 模式下的主资源服务器地址。
    /// </summary>
    public string resourcesServerURL;

    /// <summary>
    /// Host 模式下的备用资源服务器地址。
    /// </summary>
    public string fallbackServerURL;

    /// <summary>
    /// 单次资源更新允许的最大并发下载数。
    /// </summary>
    [Tooltip("最大并发下载数")]
    public int downloadMaxNum;

    /// <summary>
    /// 单个资源下载失败后的重试次数。
    /// </summary>
    public int failedTryAgain;

    #endregion

    #region Private 私有成员

    private ResourcePackage package; // 当前运行的 YooAsset 资源包。
    private long totalBytes; // 当前下载任务的总字节数。

    #endregion

    #region Private 私有成员

    /// <summary>
    /// 启动 YooAsset、加载热更新程序集并进入业务场景。
    /// </summary>
    private void Awake()
    {
        LaunchWithErrorHandlingAsync().Forget();
    }

    /// <summary>
    /// 执行启动流程并统一处理无法继续启动的异常。
    /// </summary>
    /// <returns>启动流程监督任务。</returns>
    private async MTask LaunchWithErrorHandlingAsync()
    {
        try
        {
            await LaunchAsync();
        }
        catch (Exception exception)
        {
            LogSwitch.Error($"HotUpdate Bootstrap 启动失败：{exception}");
            if (Application.isBatchMode)
            {
                Application.Quit(1);
            }
        }
    }

    /// <summary>
    /// 按既定顺序执行启动流程。
    /// </summary>
    /// <returns>启动流程完成任务。</returns>
    private async MTask LaunchAsync()
    {
        await VersionCheckAsync();
        await DownloadAssetsAsync();
        UnityEngineTypePreserver.ProtectDynamicContentTypes();
        await LoadAssembliesAsync();
        await StartHotUpdateAsync();
    }

    /// <summary>
    /// 初始化 YooAsset 并拉取当前资源包的最新清单。
    /// </summary>
    /// <returns>版本检查完成任务。</returns>
    private async MTask VersionCheckAsync()
    {
        // 初始化资源系统
        YooAssets.Initialize();
        // 创建并设置默认包
        YooAssets.CreatePackage(packageName);
        package = YooAssets.GetPackage(packageName);
        YooAssets.SetDefaultPackage(package);

        await InitPackageAsync();

        // 请求最新版本号
        var versionOpeartion = package.RequestPackageVersionAsync();
        await versionOpeartion.ToMTask();
        if (versionOpeartion.Status == EOperationStatus.Succeed)
        {
            string remoteVersion = versionOpeartion.PackageVersion;
            LogSwitch.Info($"获取最新包版本成功：{remoteVersion}");
            // 更新清单
            await UpdatePackageManifestAsync(remoteVersion);
        }
        else
        {
            throw new InvalidOperationException($"获取最新包版本失败：{versionOpeartion.Error}");
        }
    }

    /// <summary>
    /// 将资源包清单更新到指定版本。
    /// </summary>
    /// <param name="packageVersion">需要加载的资源包版本号。</param>
    /// <returns>清单更新完成任务。</returns>
    private async MTask UpdatePackageManifestAsync(string packageVersion)
    {
        var updateOperation = package.UpdatePackageManifestAsync(packageVersion);
        await updateOperation.ToMTask();
        if (updateOperation.Status == EOperationStatus.Succeed)
        {
            LogSwitch.Info("更新清单成功");
        }
        else
        {
            throw new InvalidOperationException($"更新清单失败：{updateOperation.Error}");
        }
    }

    /// <summary>
    /// 下载当前资源包中尚未缓存的资源文件。
    /// </summary>
    /// <returns>资源下载完成任务。</returns>
    private async MTask DownloadAssetsAsync()
    {
        var downloader = package.CreateResourceDownloader(downloadMaxNum, failedTryAgain);
        if (downloader.TotalDownloadCount == 0)
        {
            LogSwitch.Info("已是最新版本，无需下载。");
            return;
        }

        totalBytes = downloader.TotalDownloadBytes;

        downloader.DownloadFinishCallback = OnDownloadFinished;
        downloader.DownloadErrorCallback = OnDownloadError;
        downloader.DownloadUpdateCallback = OnDownloadUpdate;
        downloader.DownloadFileBeginCallback = OnDownloadFileBegin;

        downloader.BeginDownload();
        await downloader.ToMTask();

        if (downloader.Status == EOperationStatus.Succeed)
        {
            LogSwitch.Info("资源下载完成");
        }
        else
        {
            throw new InvalidOperationException($"资源下载失败：{downloader.Error}");
        }
    }

    /// <summary>
    /// 在下载单个文件前更新启动提示。
    /// </summary>
    /// <param name="data">即将下载的文件信息。</param>
    private void OnDownloadFileBegin(DownloadFileData data)
    {
        SetPromptInfo("开始下载文件...");
    }

    /// <summary>
    /// 在下载进度变化时更新日志与启动提示。
    /// </summary>
    /// <param name="data">当前下载进度信息。</param>
    private void OnDownloadUpdate(DownloadUpdateData data)
    {
        LogSwitch.Info($"下载进度 {data.CurrentDownloadBytes}/{totalBytes}");
        SetPromptInfo($"正在下载资源...({data.CurrentDownloadBytes}/{totalBytes} bytes)");
    }

    /// <summary>
    /// 在下载文件失败时输出错误提示。
    /// </summary>
    /// <param name="data">下载失败信息。</param>
    private void OnDownloadError(DownloadErrorData data)
    {
        SetPromptInfo($"<color=red>下载出错：{data.ErrorInfo}</color>");
    }

    /// <summary>
    /// 在所有资源下载完成后更新启动提示。
    /// </summary>
    /// <param name="data">下载完成信息。</param>
    private void OnDownloadFinished(DownloaderFinishData data)
    {
        SetPromptInfo("资源下载完成");
    }

    /// <summary>
    /// 按 Inspector 配置初始化 YooAsset 资源包。
    /// </summary>
    /// <returns>资源包初始化完成任务。</returns>
    private async MTask InitPackageAsync()
    {
        switch (bundlePackageMode)
        {
            case BundlePackageMode.EditorSimulateMode:
                await InitPackageAsync_EditorSimulate();
                break;
            case BundlePackageMode.OfflinePlayMode:
                await InitPackageAsync_OfflinePlayMode();
                break;
            case BundlePackageMode.HostPlayMode:
                await InitPackageAsync_HostPlayMode();
                break;
            case BundlePackageMode.WebPlayMode:
                await InitPackageAsync_WebPlayMode();
                break;
            case BundlePackageMode.CustomPlayMode:
                //TODO: 自定义模式
                break;
        }
    }

    /// <summary>
    /// 使用编辑器模拟模式初始化资源包。
    /// </summary>
    /// <returns>资源包初始化完成任务。</returns>
    private async MTask InitPackageAsync_EditorSimulate()
    {
        var buildResult = EditorSimulateModeHelper.SimulateBuild(packageName);
        var packageRoot = buildResult.PackageRootDirectory;
        var fileSystemParameters = FileSystemParameters.CreateDefaultEditorFileSystemParameters(packageRoot);

        var createParameters = new EditorSimulateModeParameters();
        createParameters.EditorFileSystemParameters = fileSystemParameters;

        var initOperation = package.InitializeAsync(createParameters);
        await initOperation.ToMTask();
        if (initOperation.Status == EOperationStatus.Succeed)
        {
            LogSwitch.Info("初始化成功");
        }
        else
        {
            throw new InvalidOperationException($"Editor 模拟资源包初始化失败：{initOperation.Error}");
        }
    }

    /// <summary>
    /// 使用离线首包模式初始化资源包。
    /// </summary>
    /// <returns>资源包初始化完成任务。</returns>
    private async MTask InitPackageAsync_OfflinePlayMode()
    {
        var fileSystemParameters = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();

        var createParameters = new OfflinePlayModeParameters();
        createParameters.BuildinFileSystemParameters = fileSystemParameters;

        var initOperation = package.InitializeAsync(createParameters);
        await initOperation.ToMTask();
        if (initOperation.Status == EOperationStatus.Succeed)
        {
            LogSwitch.Info("初始化成功");
        }
        else
        {
            throw new InvalidOperationException($"离线资源包初始化失败：{initOperation.Error}");
        }
    }

    /// <summary>
    /// 使用 Host 模式初始化资源包。
    /// </summary>
    /// <returns>资源包初始化完成任务。</returns>
    private async MTask InitPackageAsync_HostPlayMode()
    {
        IRemoteServices remoteServices = new RemoteServices(resourcesServerURL, fallbackServerURL);
        var cacheParameters = FileSystemParameters.CreateDefaultCacheFileSystemParameters(remoteServices);
        var buildinParameters = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();

        var createParameters = new HostPlayModeParameters();
        createParameters.BuildinFileSystemParameters = buildinParameters;
        createParameters.CacheFileSystemParameters = cacheParameters;

        var initOperation = package.InitializeAsync(createParameters);
        await initOperation.ToMTask();

        if (initOperation.Status == EOperationStatus.Succeed)
        {
            LogSwitch.Info("初始化成功");
        }
        else
        {
            throw new InvalidOperationException($"Host 资源包初始化失败：{initOperation.Error}");
        }
    }

    /// <summary>
    /// 使用当前 WebGL 页面随包发布的 StreamingAssets 初始化资源包。
    /// </summary>
    /// <returns>资源包初始化完成任务。</returns>
    private async MTask InitPackageAsync_WebPlayMode()
    {
        var webServerFileSystemParameters = FileSystemParameters.CreateDefaultWebServerFileSystemParameters();

        var createParameters = new WebPlayModeParameters();
        createParameters.WebServerFileSystemParameters = webServerFileSystemParameters;

        var initOperation = package.InitializeAsync(createParameters);
        await initOperation.ToMTask();
        if (initOperation.Status == EOperationStatus.Succeed)
        {
            LogSwitch.Info("初始化成功");
        }
        else
        {
            throw new InvalidOperationException($"Web 资源包初始化失败：{initOperation.Error}");
        }
    }

    /// <summary>
    /// 从 YooAsset 加载 AOT 元数据和热更新 DLL。
    /// </summary>
    /// <returns>DLL 加载完成任务。</returns>
    private MTask LoadAssembliesAsync()
    {
        SetPromptInfo("正在加载热更代码...");
#if !UNITY_EDITOR
        return LoadPlayerAssembliesAsync();
#else
        SetPromptInfo("加载完成，准备进入游戏...");
        return MTask.CompletedTask;
#endif
    }

#if !UNITY_EDITOR
    /// <summary>
    /// 在 Player 中依次加载 AOT 补充元数据和 HotUpdate 程序集。
    /// </summary>
    /// <returns>Player 程序集加载完成任务。</returns>
    private async MTask LoadPlayerAssembliesAsync()
    {
        // 加载 AOT 补充元数据
        foreach (string dll in HybridClrAotMetadata.AotMetadataAddresses)
        {
            if (string.IsNullOrWhiteSpace(dll))
            {
                continue;
            }

            AssetHandle handle = package.LoadAssetAsync<TextAsset>(dll);
            try
            {
                await handle.ToMTask();
                TextAsset dllText = handle.AssetObject as TextAsset;
                if (dllText == null)
                {
                    throw new InvalidOperationException($"未找到 AOT 元数据：{dll}");
                }

                HybridCLR.RuntimeApi.LoadMetadataForAOTAssembly(
                    dllText.bytes,
                    HybridCLR.HomologousImageMode.SuperSet);
            }
            finally
            {
                handle.Release();
            }
        }

        foreach (string address in HybridClrAotMetadata.HotUpdateAssemblyAddresses)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                throw new InvalidOperationException("热更新程序集地址表包含空地址。");
            }

            AssetHandle handle = package.LoadAssetAsync<TextAsset>(address);
            try
            {
                await handle.ToMTask();
                TextAsset assemblyAsset = handle.AssetObject as TextAsset;
                if (assemblyAsset == null)
                {
                    throw new InvalidOperationException($"未找到热更新 DLL：{address}");
                }

                Assembly.Load(assemblyAsset.bytes);
            }
            finally
            {
                handle.Release();
            }
        }

        SetPromptInfo("加载完成，准备进入游戏...");
    }
#endif

    /// <summary>
    /// 在目标场景加载完成后调用 HotUpdate 的静态启动方法。
    /// </summary>
    /// <returns>入口启动完成任务。</returns>
    private async MTask StartHotUpdateAsync()
    {
        Assembly startupAssembly = FindLoadedAssembly(HybridClrAotMetadata.StartupAssemblyName);
        if (startupAssembly == null)
        {
            throw new InvalidOperationException($"未加载 HotUpdate 启动程序集：{HybridClrAotMetadata.StartupAssemblyName}");
        }

        Type startupType = startupAssembly.GetType(HybridClrAotMetadata.StartupTypeName, false);
        if (startupType == null)
        {
            throw new InvalidOperationException($"未找到 HotUpdate 启动类型：{HybridClrAotMetadata.StartupTypeName}");
        }

        MethodInfo startMethod = startupType.GetMethod(
            HybridClrAotMetadata.StartupMethodName,
            BindingFlags.Public | BindingFlags.Static,
            null,
            Type.EmptyTypes,
            null);
        if (startMethod == null)
        {
            throw new InvalidOperationException($"HotUpdate 启动类型缺少无参静态 MTask 方法：{startupType.FullName}.{HybridClrAotMetadata.StartupMethodName}");
        }

        object invokeResult = startMethod.Invoke(null, null);
        if (!(invokeResult is MTask startTask))
        {
            throw new InvalidOperationException($"HotUpdate 启动方法必须返回 {nameof(MTask)}：{startupType.FullName}.{HybridClrAotMetadata.StartupMethodName}");
        }

        await startTask;
    }

    /// <summary>
    /// 按程序集简单名称查找已经由 Unity 或 Bootstrap 加载的程序集。
    /// </summary>
    /// <param name="assemblyName">不含 DLL 后缀的程序集名称。</param>
    /// <returns>找到的程序集；尚未加载时返回空。</returns>
    private static Assembly FindLoadedAssembly(string assemblyName)
    {
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
        for (int index = 0; index < assemblies.Length; index++)
        {
            if (string.Equals(assemblies[index].GetName().Name, assemblyName, StringComparison.Ordinal))
            {
                return assemblies[index];
            }
        }

        return null;
    }

    /// <summary>
    /// 输出启动阶段的提示信息。
    /// </summary>
    /// <param name="msg">需要输出的提示文本。</param>
    private void SetPromptInfo(string msg)
    {
        LogSwitch.Info($"[Bootstrap] {msg}");
    }

    #endregion
}
