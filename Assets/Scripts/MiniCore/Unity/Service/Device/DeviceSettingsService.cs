using MiniCore.Model;
using UnityEngine;

namespace MiniCore.Service
{
    /// <summary>
    /// 将客户端设置映射到 Unity 的画质、帧率、分辨率和垂直同步 API。
    /// </summary>
    [AppService("设备设置", typeof(IDeviceSettingsService), Description = "将客户端画质、分辨率、帧率和垂直同步设置应用到当前设备。", RuntimeTargets = AppServiceRuntimeTargets.Client)]
    public sealed class DeviceSettingsService : AAppService, IDeviceSettingsService
    {
        #region Interface 接口实现

        /// <summary>
        /// 在当前平台支持的范围内应用画质、显示与帧率设置。
        /// </summary>
        /// <param name="settings">要应用的客户端设置。</param>
        public void Apply(ClientSettings settings)
        {
            if (settings == null || Application.isBatchMode)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(settings.QualityLevel))
            {
                int qualityIndex = QualitySettings.names == null ? -1 : System.Array.IndexOf(QualitySettings.names, settings.QualityLevel);
                if (qualityIndex >= 0)
                {
                    QualitySettings.SetQualityLevel(qualityIndex, true);
                }
            }

            QualitySettings.vSyncCount = Mathf.Clamp(settings.VSyncCount, 0, 4);
            Application.targetFrameRate = settings.TargetFrameRate;
            if (settings.ScreenWidth > 0 && settings.ScreenHeight > 0)
            {
                Screen.SetResolution(settings.ScreenWidth, settings.ScreenHeight, settings.FullScreen);
            }
        }

        #endregion
    }
}
