using System;
using MiniCore.Core;
using MiniCore.Model;
using MiniCore.Service;
using MiniCore.Threading;

namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// MiniBomber 客户端与 Dedicated Server 共用的启动资产加载基类。
    /// </summary>
    public abstract class MiniBomberStartupComponentBase : AComponent
    {
        #region Protected 受保护成员

        protected IResourceService Resources { get; private set; }
        protected MiniBomberRuntimeConfig RuntimeConfig { get; private set; }
        protected MiniBomberRuleConfig RuleConfig { get; private set; }
        protected BomberMapDefinition MapDefinition { get; private set; }

        /// <summary>
        /// 预加载客户端与服务器共用的运行、规则和地图配置。
        /// </summary>
        /// <returns>共享配置加载完成任务。</returns>
        protected async MTask LoadConfigurationAsync()
        {
            if (RuntimeConfig != null)
            {
                return;
            }

            Resources = Global.GetService<IResourceService>(this);
            RuntimeConfig = await Resources.PreloadAssetAsync<MiniBomberRuntimeConfig>(MiniBomberConstants.RuntimeConfigAddress);
            RuleConfig = await Resources.PreloadAssetAsync<MiniBomberRuleConfig>(MiniBomberConstants.RuleConfigAddress);
            MapDefinition = await Resources.PreloadAssetAsync<BomberMapDefinition>(MiniBomberConstants.DefaultMapAddress);
            if (RuntimeConfig == null || RuleConfig == null || MapDefinition == null)
            {
                throw new InvalidOperationException("MiniBomber 配置资产不完整，请运行 MiniCore/Demos/MiniBomber/Create Default Assets。");
            }
        }

        #endregion

        #region Override 重写实现

        /// <summary>
        /// 释放 Demo 配置资产租约和当前启动组件持有的服务引用。
        /// </summary>
        protected override void OnDispose()
        {
            Resources?.ReleaseAsset(MiniBomberConstants.RuntimeConfigAddress);
            Resources?.ReleaseAsset(MiniBomberConstants.RuleConfigAddress);
            Resources?.ReleaseAsset(MiniBomberConstants.DefaultMapAddress);
            Resources = null;
            RuntimeConfig = null;
            RuleConfig = null;
            MapDefinition = null;
            Global.ReleaseAll(this);
            base.OnDispose();
        }

        #endregion
    }
}
