using Cysharp.Threading.Tasks;
using System;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YooAsset;
using MiniCore.Core;
using MiniCore.Model;

public class UpdateMainWindow : MonoBehaviour
{
    [SerializeField]
    private TMP_Text promptText;

    [SerializeField]
    private string hotUpdateDllPath;

    [SerializeField]
    private Image progressBar;
    [SerializeField]
    private TMP_Text progressText;

    [SerializeField]
    private BundlePackageMode bundlePackageMode;

    [Tooltip("热更新包名")]
    public string packageName;
    public string resourcesServerURL;
    public string fallbackServerURL;

    [Tooltip("最大并发下载数")]
    public int downloadMaxNum;
    public int failedTryAgain;

    public string mainSceneName;

    private async void Awake()
    {
        await LaunchAsync();
    }

    private async UniTask LaunchAsync()
    {
        await VersionCheckAsync();
        await DownloadAssetsAsync();
        //await LoadAssemebliesAsync();
        await EnterGameAsync();
    }

    private ResourcePackage package;
    private async UniTask VersionCheckAsync()
    {
        // 初始化资源系统
        YooAssets.Initialize();
        // 创建并设置默认包
        var defaultPackage = YooAssets.CreatePackage(packageName);
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
            EventCenter.Broadcast(GameEvent.LogError, $"获取最新包版本失败：{versionOpeartion.Error}");
        }
    }

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
            EventCenter.Broadcast(GameEvent.LogError, $"更新清单失败：{updateOperation.Error}");
        }
    }

    int totalCount;
    long totalBytes;
    private async UniTask DownloadAssetsAsync()
    {
        var downloader = package.CreateResourceDownloader(downloadMaxNum, failedTryAgain);
        if (downloader.TotalDownloadCount == 0)
        {
            EventCenter.Broadcast(GameEvent.LogInfo, "已是最新版本，无需下载。");
            return;
        }

        totalCount = downloader.TotalDownloadCount;
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
            EventCenter.Broadcast(GameEvent.LogError, "资源下载失败");
        }
    }

    private void OnDownloadFileBegin(DownloadFileData data)
    {
        SetPromptInfo("开始下载文件...");
    }

    private void OnDownloadUpdate(DownloadUpdateData data)
    {
        EventCenter.Broadcast(GameEvent.LogInfo, $"下载进度 {data.CurrentDownloadBytes}/{totalBytes}");
        SetPromptInfo($"正在下载资源...({data.CurrentDownloadBytes}/{totalBytes} bytes)");
    }

    private void OnDownloadError(DownloadErrorData data)
    {
        SetPromptInfo($"<color=red>下载出错：{data.ErrorInfo}</color>");
    }

    private void OnDownloadFinished(DownloaderFinishData data)
    {
        SetPromptInfo("资源下载完成");
    }

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
    /// 编辑器模拟
    /// </summary>
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
            EventCenter.Broadcast(GameEvent.LogError, $"初始化失败：{initOperation.Error}");
        }
    }

    /// <summary>
    /// 离线模式
    /// </summary>
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
            EventCenter.Broadcast(GameEvent.LogError, $"初始化失败：{initOperation.Error}");
        }
    }

    /// <summary>
    /// Host 模式
    /// </summary>
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
            EventCenter.Broadcast(GameEvent.LogError, $"初始化失败：{initOperation.Error}");
        }
    }

    /// <summary>
    /// Web 模式
    /// </summary>
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
            EventCenter.Broadcast(GameEvent.LogError, $"初始化失败：{initOperation.Error}");
        }
    }

/*    private async UniTask LoadAssemebliesAsync()
    {
        SetPromptInfo("正在加载热更代码...");
#if !UNITY_EDITOR
        // 加载 AOT 补充元数据
        foreach (var dll in AOTGenericReferences.PatchedAOTAssemblyList)
        {
            AssetHandle handle = package.LoadAssetAsync<TextAsset>(dll);
            await handle.Task;
            TextAsset dllText = handle.AssetObject as TextAsset;
            HybridCLR.RuntimeApi.LoadMetadataForAOTAssembly(dllText.bytes, HybridCLR.HomologousImageMode.SuperSet);
        }

        AssetHandle hotUpdateHandle = package.LoadAssetAsync<TextAsset>(hotUpdateDllPath);
        await hotUpdateHandle.Task;
        TextAsset hotUpdateText = hotUpdateHandle.AssetObject as TextAsset;
        Assembly hotUpdateAssemebly = Assembly.Load(hotUpdateText.bytes);
#endif
        SetPromptInfo("加载完成，准备进入游戏...");
    }*/

    private async UniTask EnterGameAsync()
    {
        SetPromptInfo("即将进入游戏...");
        await UniTask.Delay(TimeSpan.FromSeconds(3));
        var handle = package.LoadSceneAsync(mainSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single, UnityEngine.SceneManagement.LocalPhysicsMode.None, false);
        await handle.ToUniTask();
    }

    private void SetPromptInfo(string msg)
    {
        promptText.text = msg;
    }
}

class RemoteServices : IRemoteServices
{
    private readonly string _resourcesServerURL;
    private readonly string _fallbackServerURL;

    public RemoteServices(string resourcesServerUrl, string fallbackServerUrl)
    {
        _resourcesServerURL = resourcesServerUrl;
        _fallbackServerURL = fallbackServerUrl;
    }

    string IRemoteServices.GetRemoteFallbackURL(string fileName)
    {
        return $"{_fallbackServerURL}/{fileName}";
    }

    string IRemoteServices.GetRemoteMainURL(string fileName)
    {
        return $"{_resourcesServerURL}/{fileName}";
    }
}