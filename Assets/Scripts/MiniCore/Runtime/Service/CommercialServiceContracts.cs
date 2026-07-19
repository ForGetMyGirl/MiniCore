using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MiniCore.Service
{
    /// <summary>
    /// 提供由 Global Tick 驱动的计时任务创建与控制能力。
    /// </summary>
    public interface ITimerService : IAppService
    {
        /// <summary>
        /// 创建计时任务。
        /// </summary>
        /// <param name="duration">触发间隔秒数。</param>
        /// <param name="onComplete">到期回调。</param>
        /// <param name="loop">是否循环触发。</param>
        /// <param name="ignoreTimeScale">是否使用非缩放时间。</param>
        /// <param name="autoStart">是否立即开始。</param>
        /// <returns>可暂停、继续或移除的计时任务。</returns>
        TimerTask CreateTimer(float duration, Action onComplete, bool loop = false, bool ignoreTimeScale = true, bool autoStart = true);

        /// <summary>
        /// 停止并移除指定计时任务。
        /// </summary>
        /// <param name="task">待移除任务。</param>
        void RemoveTimer(TimerTask task);

        /// <summary>
        /// 暂停全部计时任务。
        /// </summary>
        void PauseAll();

        /// <summary>
        /// 恢复全部计时任务。
        /// </summary>
        void ResumeAll();
    }

    /// <summary>
    /// 提供本地持久化数据根目录与受控子目录的系统服务契约。
    /// 存档和本地运行数据应通过该服务取得路径，不能硬编码框架目录名称。
    /// </summary>
    public interface IStoragePathService : IAppService
    {
        /// <summary>
        /// 获取当前项目解析后的本地持久化根目录。
        /// </summary>
        string RootPath { get; }

        /// <summary>
        /// 获取并确保指定用途的一级子目录存在。
        /// </summary>
        /// <param name="directoryName">不包含路径分隔符的用途目录名称。</param>
        /// <returns>已创建或已存在的子目录绝对路径。</returns>
        string GetDirectory(string directoryName);
    }

    /// <summary>
    /// 提供版本化加密存档读写能力的系统服务契约。
    /// </summary>
    public interface ISaveService : IAppService
    {
        /// <summary>
        /// 异步保存一个逻辑槽位的数据。
        /// </summary>
        /// <typeparam name="T">待保存数据类型。</typeparam>
        /// <param name="slotName">逻辑槽位名称。</param>
        /// <param name="data">待保存数据。</param>
        /// <param name="token">取消令牌。</param>
        /// <returns>保存完成任务。</returns>
        Task SaveAsync<T>(string slotName, T data, CancellationToken token = default);

        /// <summary>
        /// 异步读取一个逻辑槽位的数据。
        /// </summary>
        /// <typeparam name="T">目标数据类型。</typeparam>
        /// <param name="slotName">逻辑槽位名称。</param>
        /// <param name="token">取消令牌。</param>
        /// <returns>槽位不存在时返回空；否则返回反序列化数据。</returns>
        Task<T> LoadAsync<T>(string slotName, CancellationToken token = default) where T : class;
    }

    /// <summary>
    /// 提供客户端偏好设置读取、写入与变更通知的服务契约。
    /// </summary>
    public interface ISettingsService : IAppService
    {
        /// <summary>
        /// 当前设置发生保存或替换后触发。
        /// </summary>
        event Action<ClientSettings> Changed;

        /// <summary>
        /// 获取当前客户端设置快照。
        /// </summary>
        ClientSettings Current { get; }

        /// <summary>
        /// 异步加载设置。
        /// </summary>
        /// <param name="token">取消令牌。</param>
        /// <returns>加载完成任务。</returns>
        Task LoadAsync(CancellationToken token = default);

        /// <summary>
        /// 替换设置并持久化。
        /// </summary>
        /// <param name="settings">待保存设置。</param>
        /// <param name="token">取消令牌。</param>
        /// <returns>保存完成任务。</returns>
        Task SaveAsync(ClientSettings settings, CancellationToken token = default);
    }

    /// <summary>
    /// 客户端通用设备与偏好设置数据。
    /// </summary>
    [Serializable]
    public sealed class ClientSettings
    {
        /// <summary>
        /// 获取或设置设置数据版本。
        /// </summary>
        public int Version { get; set; } = 1;

        /// <summary>
        /// 获取或设置质量档名称。
        /// </summary>
        public string QualityLevel { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置目标帧率；零表示平台默认值。
        /// </summary>
        public int TargetFrameRate { get; set; } = 60;

        /// <summary>
        /// 获取或设置垂直同步数量。
        /// </summary>
        public int VSyncCount { get; set; }

        /// <summary>
        /// 获取或设置窗口宽度；零表示保留平台默认分辨率。
        /// </summary>
        public int ScreenWidth { get; set; }

        /// <summary>
        /// 获取或设置窗口高度；零表示保留平台默认分辨率。
        /// </summary>
        public int ScreenHeight { get; set; }

        /// <summary>
        /// 获取或设置是否使用全屏显示。
        /// </summary>
        public bool FullScreen { get; set; } = true;

        /// <summary>
        /// 获取或设置 BGM 音量。
        /// </summary>
        public float BgmVolume { get; set; } = 1f;

        /// <summary>
        /// 获取或设置音效音量。
        /// </summary>
        public float SfxVolume { get; set; } = 1f;

        /// <summary>
        /// 获取或设置 UI 音量。
        /// </summary>
        public float UiVolume { get; set; } = 1f;

        /// <summary>
        /// 获取或设置是否允许震动反馈。
        /// </summary>
        public bool VibrationEnabled { get; set; } = true;

        /// <summary>
        /// 获取或设置当前语言代码。
        /// </summary>
        public string Language { get; set; } = string.Empty;
    }

    /// <summary>
    /// 提供结构化指标、事件和异常记录的系统服务契约。
    /// </summary>
    public interface ITelemetryService : IAppService
    {
        /// <summary>
        /// 累加一个计数指标。
        /// </summary>
        /// <param name="name">稳定指标名称。</param>
        /// <param name="value">累加值。</param>
        void Increment(string name, long value = 1);

        /// <summary>
        /// 更新一个瞬时数值指标。
        /// </summary>
        /// <param name="name">稳定指标名称。</param>
        /// <param name="value">当前数值。</param>
        void Gauge(string name, double value);

        /// <summary>
        /// 记录结构化业务事件。
        /// </summary>
        /// <param name="name">稳定事件名称。</param>
        /// <param name="fields">可选结构化字段。</param>
        void Track(string name, IReadOnlyDictionary<string, string> fields = null);

        /// <summary>
        /// 记录异常事件。
        /// </summary>
        /// <param name="exception">待记录异常。</param>
        /// <param name="context">可选错误上下文。</param>
        void TrackException(Exception exception, string context = null);
    }

    /// <summary>
    /// 可选的 HTTP 鉴权服务。实现可在请求发送前写入令牌、签名或渠道标识。
    /// </summary>
    public interface IHttpAuthProvider : IAppService
    {
        /// <summary>
        /// 将鉴权信息追加到本次请求头集合。
        /// </summary>
        /// <param name="headers">可修改的请求头集合。</param>
        /// <param name="token">请求取消令牌。</param>
        /// <returns>鉴权信息准备完成任务。</returns>
        Task ApplyAsync(IDictionary<string, string> headers, CancellationToken token = default);
    }

    /// <summary>
    /// UnityWebRequest 等传输层共同使用的 HTTP 请求描述。
    /// </summary>
    public sealed class HttpRequest
    {
        /// <summary>
        /// 获取或设置 HTTP 或 HTTPS 的绝对请求地址。
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// 获取或设置 HTTP 方法，例如 GET、POST 或 DELETE。
        /// </summary>
        public string Method { get; set; } = "GET";

        /// <summary>
        /// 获取或设置原始请求正文；空请求正文请保持 null。
        /// </summary>
        public byte[] Body { get; set; }

        /// <summary>
        /// 获取或设置请求内容类型。
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// 获取本请求独有的 Header 集合。
        /// </summary>
        public IDictionary<string, string> Headers { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 获取或设置超时秒数；小于等于零时使用服务默认值。
        /// </summary>
        public int TimeoutSeconds { get; set; }

        /// <summary>
        /// 获取或设置调用方是否允许对非 GET/HEAD 请求重试。
        /// </summary>
        public bool IsIdempotent { get; set; }
    }

    /// <summary>
    /// HTTP 请求完成后的原始响应。
    /// </summary>
    public sealed class HttpResponse
    {
        /// <summary>
        /// 获取 HTTP 状态码；传输失败时通常为零。
        /// </summary>
        public long StatusCode { get; set; }

        /// <summary>
        /// 获取响应正文。
        /// </summary>
        public byte[] Body { get; set; }

        /// <summary>
        /// 获取响应头集合。
        /// </summary>
        public IReadOnlyDictionary<string, string> Headers { get; set; }

        /// <summary>
        /// 获取传输层错误文本；成功时为 null。
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// 获取本请求是否成功完成。
        /// </summary>
        public bool IsSuccess => string.IsNullOrEmpty(Error) && StatusCode >= 200 && StatusCode <= 299;
    }

    /// <summary>
    /// 提供原始、JSON 和 Protobuf HTTP 通信能力的系统服务契约。
    /// </summary>
    public interface IHttpService : IAppService
    {
        /// <summary>
        /// 发送原始 HTTP 请求。
        /// </summary>
        /// <param name="request">请求描述。</param>
        /// <param name="token">取消令牌。</param>
        /// <returns>原始 HTTP 响应。</returns>
        Task<HttpResponse> SendAsync(HttpRequest request, CancellationToken token = default);

        /// <summary>
        /// 发送 JSON 请求并反序列化 JSON 响应。
        /// </summary>
        /// <typeparam name="TRequest">JSON 请求类型。</typeparam>
        /// <typeparam name="TResponse">JSON 响应类型。</typeparam>
        /// <param name="url">HTTP 或 HTTPS 的绝对请求地址。</param>
        /// <param name="request">请求对象。</param>
        /// <param name="method">HTTP 方法。</param>
        /// <param name="token">取消令牌。</param>
        /// <returns>反序列化后的响应对象。</returns>
        Task<TResponse> SendJsonAsync<TRequest, TResponse>(string url, TRequest request, string method = "POST", CancellationToken token = default);

        /// <summary>
        /// 发送 Protobuf 二进制请求并将响应反序列化为指定消息类型。
        /// </summary>
        /// <typeparam name="TResponse">响应消息类型。</typeparam>
        /// <param name="url">HTTP 或 HTTPS 的绝对请求地址。</param>
        /// <param name="requestBody">请求消息序列化后的二进制正文。</param>
        /// <param name="responseParser">将原始响应正文转换为目标消息的函数。</param>
        /// <param name="method">HTTP 方法。</param>
        /// <param name="token">取消令牌。</param>
        /// <returns>响应消息。</returns>
        Task<TResponse> SendProtobufAsync<TResponse>(string url, byte[] requestBody, Func<byte[], TResponse> responseParser, string method = "POST", CancellationToken token = default);
    }

    /// <summary>
    /// 将客户端设置应用到当前 Unity 运行环境的服务契约。
    /// Dedicated Server 可不绑定此服务，或绑定安全的空实现。
    /// </summary>
    public interface IDeviceSettingsService : IAppService
    {
        /// <summary>
        /// 将设置应用到当前运行环境。
        /// </summary>
        /// <param name="settings">要应用的设置。</param>
        void Apply(ClientSettings settings);
    }

    /// <summary>
    /// 提供按资源地址播放 BGM、音效与 UI 音效的系统服务契约。
    /// </summary>
    public interface IAudioService : IAppService
    {
        /// <summary>
        /// 从 Resources 路径切换当前背景音乐。
        /// 项目可通过 AudioService 的资源注册 API 替换默认 Resources 加载策略。
        /// </summary>
        /// <param name="resourcePath">AudioClip 的资源路径。</param>
        /// <param name="loop">是否循环播放。</param>
        void PlayBgm(string resourcePath, bool loop = true);

        /// <summary>
        /// 从 Resources 路径播放一次性音效。
        /// </summary>
        /// <param name="resourcePath">AudioClip 的资源路径。</param>
        void PlaySfx(string resourcePath);

        /// <summary>
        /// 从 Resources 路径播放一次性 UI 音效。
        /// </summary>
        /// <param name="resourcePath">AudioClip 的资源路径。</param>
        void PlayUi(string resourcePath);

        /// <summary>
        /// 设置指定混音分组的线性音量。
        /// </summary>
        /// <param name="group">分组名称：Master、BGM、SFX 或 UI。</param>
        /// <param name="volume">零到一之间的线性音量。</param>
        void SetVolume(string group, float volume);

        /// <summary>
        /// 设置指定混音分组的静音状态。
        /// </summary>
        /// <param name="group">分组名称。</param>
        /// <param name="muted">是否静音。</param>
        void SetMuted(string group, bool muted);

        /// <summary>
        /// 停止当前 BGM。
        /// </summary>
        void StopBgm();
    }
}
