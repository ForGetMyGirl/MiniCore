using Cysharp.Threading.Tasks;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using YooAsset;
using MiniCore.Bootstrap;
using MiniCore.Core;
using MiniCore.Model;

public class UpdateMainWindow : MonoBehaviour
{
    #region Private 私有成员

    [SerializeField]
    private string hotUpdateDllPath = HybridClrAotMetadata.HotUpdateDllAddress; // YooAsset 中热更新 DLL 的固定地址。

    private const string HotUpdateStartupTypeName = "MiniCore.HotUpdate.MiniCoreStartup"; // 热更新 DLL 中固定的静态启动类型。
    private const string HotUpdateStartupMethodName = "StartAsync"; // 热更新静态启动方法名称。

    [SerializeField]
    private BundlePackageMode bundlePackageMode; // YooAsset 运行模式。

    private ResourcePackage package; // 当前运行的 YooAsset 资源包。
    private long totalBytes; // 当前下载任务的总字节数。

    #endregion

    #region Public 公共成员

    [Tooltip("热更新包名")]
    /// <summary>
    /// 默认加载的 YooAsset 资源包名称。
    /// </summary>
    public string packageName;

    /// <summary>
    /// Host 模式下的主资源服务器地址。
    /// </summary>
    public string resourcesServerURL;

    /// <summary>
    /// Host 模式下的备用资源服务器地址。
    /// </summary>
    public string fallbackServerURL;

    [Tooltip("最大并发下载数")]
    /// <summary>
    /// 单次资源更新允许的最大并发下载数。
    /// </summary>
    public int downloadMaxNum;

    /// <summary>
    /// 单个资源下载失败后的重试次数。
    /// </summary>
    public int failedTryAgain;

    /// <summary>
    /// 客户端热更新完成后进入的 YooAsset 场景地址。
    /// </summary>
    public string mainSceneName;

    #endregion

    #region Private 私有成员

    /// <summary>
    /// 启动 YooAsset、加载热更新程序集并进入业务场景。
    /// </summary>
    private async void Awake()
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
    private async UniTask LaunchAsync()
    {
        await VersionCheckAsync();
        await DownloadAssetsAsync();
        await LoadAssembliesAsync();
        await EnterGameAsync();
        await StartHotUpdateAsync();
    }

    /// <summary>
    /// 初始化 YooAsset 并拉取当前资源包的最新清单。
    /// </summary>
    /// <returns>版本检查完成任务。</returns>
    private async UniTask VersionCheckAsync()
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
        await versionOpeartion.Task;
        if (versionOpeartion.Status == EOperationStatus.Succeed)
        {
            string remoteVersion = versionOpeartion.PackageVersion;
            EventCenter.Broadcast(GameEvent.LogInfo, $"获取最新包版本成功：{remoteVersion}");
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
    private async UniTask UpdatePackageManifestAsync(string packageVersion)
    {
        var updateOperation = package.UpdatePackageManifestAsync(packageVersion);
        await updateOperation.Task;
        if (updateOperation.Status == EOperationStatus.Succeed)
        {
            EventCenter.Broadcast(GameEvent.LogInfo, "更新清单成功");
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
    private async UniTask DownloadAssetsAsync()
    {
        var downloader = package.CreateResourceDownloader(downloadMaxNum, failedTryAgain);
        if (downloader.TotalDownloadCount == 0)
        {
            EventCenter.Broadcast(GameEvent.LogInfo, "已是最新版本，无需下载。");
            return;
        }

        totalBytes = downloader.TotalDownloadBytes;

        downloader.DownloadFinishCallback = OnDownloadFinished;
        downloader.DownloadErrorCallback = OnDownloadError;
        downloader.DownloadUpdateCallback = OnDownloadUpdate;
        downloader.DownloadFileBeginCallback = OnDownloadFileBegin;

        downloader.BeginDownload();
        await downloader.ToUniTask();

        if (downloader.Status == EOperationStatus.Succeed)
        {
            EventCenter.Broadcast(GameEvent.LogInfo, "资源下载完成");
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
        EventCenter.Broadcast(GameEvent.LogInfo, $"下载进度 {data.CurrentDownloadBytes}/{totalBytes}");
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
    private async UniTask InitPackageAsync()
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
    private async UniTask InitPackageAsync_EditorSimulate()
    {
        var buildResult = EditorSimulateModeHelper.SimulateBuild(packageName);
        var packageRoot = buildResult.PackageRootDirectory;
        var fileSystemParameters = FileSystemParameters.CreateDefaultEditorFileSystemParameters(packageRoot);

        var createParameters = new EditorSimulateModeParameters();
        createParameters.EditorFileSystemParameters = fileSystemParameters;

        var initOperation = package.InitializeAsync(createParameters);
        await initOperation.Task;
        if (initOperation.Status == EOperationStatus.Succeed)
        {
            EventCenter.Broadcast(GameEvent.LogInfo, "初始化成功");
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
    private async UniTask InitPackageAsync_OfflinePlayMode()
    {
        var fileSystemParameters = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();

        var createParameters = new OfflinePlayModeParameters();
        createParameters.BuildinFileSystemParameters = fileSystemParameters;

        var initOperation = package.InitializeAsync(createParameters);
        await initOperation.Task;
        if (initOperation.Status == EOperationStatus.Succeed)
        {
            EventCenter.Broadcast(GameEvent.LogInfo, "初始化成功");
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
    private async UniTask InitPackageAsync_HostPlayMode()
    {
        IRemoteServices remoteServices = new RemoteServices(resourcesServerURL, fallbackServerURL);
        var cacheParameters = FileSystemParameters.CreateDefaultCacheFileSystemParameters(remoteServices);
        var buildinParameters = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();

        var createParameters = new HostPlayModeParameters();
        createParameters.BuildinFileSystemParameters = buildinParameters;
        createParameters.CacheFileSystemParameters = cacheParameters;

        var initOperation = package.InitializeAsync(createParameters);
        await initOperation.Task;

        if (initOperation.Status == EOperationStatus.Succeed)
        {
            EventCenter.Broadcast(GameEvent.LogInfo, "初始化成功");
        }
        else
        {
            throw new InvalidOperationException($"Host 资源包初始化失败：{initOperation.Error}");
        }
    }

    /// <summary>
    /// 使用 Web 模式初始化资源包。
    /// </summary>
    /// <returns>资源包初始化完成任务。</returns>
    private async UniTask InitPackageAsync_WebPlayMode()
    {
        IRemoteServices remoteServices = new RemoteServices(resourcesServerURL, fallbackServerURL);
        var webServerFileSystemParameters = FileSystemParameters.CreateDefaultWebServerFileSystemParameters();
        var webRemoteFileSystemParameters = FileSystemParameters.CreateDefaultWebRemoteFileSystemParameters(remoteServices);

        var createParameters = new WebPlayModeParameters();
        createParameters.WebServerFileSystemParameters = webServerFileSystemParameters;
        createParameters.WebRemoteFileSystemParameters = webRemoteFileSystemParameters;

        var initOperation = package.InitializeAsync(createParameters);
        await initOperation.Task;
        if (initOperation.Status == EOperationStatus.Succeed)
        {
            EventCenter.Broadcast(GameEvent.LogInfo, "初始化成功");
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
    private async UniTask LoadAssembliesAsync()
    {
        SetPromptInfo("正在加载热更代码...");
#if !UNITY_EDITOR
        // 加载 AOT 补充元数据
        foreach (string dll in HybridClrAotMetadata.AotMetadataAddresses)
        {
            if (string.IsNullOrWhiteSpace(dll))
            {
                continue;
            }

            AssetHandle handle = package.LoadAssetAsync<TextAsset>(dll);
            await handle.Task;
            TextAsset dllText = handle.AssetObject as TextAsset;
            if (dllText == null)
            {
                throw new InvalidOperationException($"未找到 AOT 元数据：{dll}");
            }

            HybridCLR.RuntimeApi.LoadMetadataForAOTAssembly(dllText.bytes, HybridCLR.HomologousImageMode.SuperSet);
        }

        AssetHandle hotUpdateHandle = package.LoadAssetAsync<TextAsset>(hotUpdateDllPath);
        await hotUpdateHandle.Task;
        TextAsset hotUpdateText = hotUpdateHandle.AssetObject as TextAsset;
        if (hotUpdateText == null)
        {
            throw new InvalidOperationException($"未找到 HotUpdate DLL：{hotUpdateDllPath}");
        }

        Assembly.Load(hotUpdateText.bytes);
#endif
        SetPromptInfo("加载完成，准备进入游戏...");
    }

    /// <summary>
    /// 在目标场景加载完成后调用 HotUpdate 的静态启动方法。
    /// </summary>
    /// <returns>入口启动完成任务。</returns>
    private async UniTask StartHotUpdateAsync()
    {
        Type startupType = AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType(HotUpdateStartupTypeName, false))
            .FirstOrDefault(type => type != null);
        if (startupType == null)
        {
            throw new InvalidOperationException($"未找到 HotUpdate 启动类型：{HotUpdateStartupTypeName}");
        }

        MethodInfo startMethod = startupType.GetMethod(HotUpdateStartupMethodName, BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);
        if (startMethod == null)
        {
            throw new InvalidOperationException($"HotUpdate 启动类型缺少无参静态 Task 方法：{startupType.FullName}.{HotUpdateStartupMethodName}");
        }

        object invokeResult = startMethod.Invoke(null, null);
        if (!(invokeResult is Task startTask))
        {
            throw new InvalidOperationException($"HotUpdate 启动方法必须返回 {nameof(Task)}：{startupType.FullName}.{HotUpdateStartupMethodName}");
        }

        await startTask;
    }

    /// <summary>
    /// 在客户端模式下加载热更新业务场景。
    /// </summary>
    /// <returns>场景加载完成任务。</returns>
    private async UniTask EnterGameAsync()
    {
        if (Application.isBatchMode)
        {
            return;
        }

        SetPromptInfo("即将进入游戏...");
        var handle = package.LoadSceneAsync(mainSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single, UnityEngine.SceneManagement.LocalPhysicsMode.None, false);
        await handle.ToUniTask();
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

class RemoteServices : IRemoteServices
{
    #region Private 私有成员

    private readonly string _resourcesServerURL;
    private readonly string _fallbackServerURL;

    #endregion

    #region Public 公共成员

    /// <summary>
    /// 使用主、备用服务器地址创建 YooAsset 远端服务。
    /// </summary>
    /// <param name="resourcesServerUrl">主资源服务器地址。</param>
    /// <param name="fallbackServerUrl">备用资源服务器地址。</param>
    public RemoteServices(string resourcesServerUrl, string fallbackServerUrl)
    {
        _resourcesServerURL = resourcesServerUrl;
        _fallbackServerURL = fallbackServerUrl;
    }

    #endregion

    #region Interface 接口实现

    /// <summary>
    /// 获取指定资源文件的备用下载地址。
    /// </summary>
    /// <param name="fileName">资源文件名。</param>
    /// <returns>备用服务器中的资源完整地址。</returns>
    string IRemoteServices.GetRemoteFallbackURL(string fileName)
    {
        return $"{_fallbackServerURL}/{fileName}";
    }

    /// <summary>
    /// 获取指定资源文件的主下载地址。
    /// </summary>
    /// <param name="fileName">资源文件名。</param>
    /// <returns>主服务器中的资源完整地址。</returns>
    string IRemoteServices.GetRemoteMainURL(string fileName)
    {
        return $"{_resourcesServerURL}/{fileName}";
    }

    #endregion
}
