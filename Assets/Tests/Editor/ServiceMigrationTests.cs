using System;
using System.Linq;
using MiniCore.Core;
using MiniCore.Service;
using MiniCore.UI;
using NUnit.Framework;

namespace MiniCore.EditorTests
{
    /// <summary>
    /// 验证资源、通用资产与 UI 服务化迁移后的公开契约和依赖关系。
    /// </summary>
    public sealed class ServiceMigrationTests
    {
        #region Public 公共成员

        /// <summary>
        /// 初始化隔离的全局服务容器。
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            Global.Shutdown();
            Global.Initialize();
        }

        /// <summary>
        /// 关闭全局服务容器，避免服务注册影响其他测试。
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            Global.Shutdown();
        }

        /// <summary>
        /// 验证四项迁移后的实现均为 AppService，且公开服务接口与依赖声明正确。
        /// </summary>
        [Test]
        public void MigratedServices_ExposeExpectedContractsAndDependencies()
        {
            AssertServiceContract<YooAssetResourceService, IResourceService>();
            AssertServiceContract<AssetService, IAssetService>(typeof(IResourceService));
            AssertServiceContract<UIService, MiniCore.UI.IUIService>(typeof(IResourceService));
            AssertServiceContract<StoragePathService, IStoragePathService>();
            AssertServiceContract<EncryptedSaveService, ISaveService>(typeof(IStoragePathService));
            AssertServiceContract<LocalTelemetryFileService, ITelemetryService>(typeof(IStoragePathService));
        }

        /// <summary>
        /// 验证服务解析取得的是已注册实现，且 owner 与根引用均释放后服务会销毁。
        /// </summary>
        [Test]
        public void AppService_InterfacesResolveAndReleaseThroughOwnerLifecycle()
        {
            object owner = new object();
            TestResourceService resource = Global.RegisterAppService<ITestResourceService, TestResourceService>();
            TestAssetService asset = Global.RegisterAppService<ITestAssetService, TestAssetService>();
            TestUIService ui = Global.RegisterAppService<ITestUIService, TestUIService>();

            Assert.AreSame(resource, Global.GetService<ITestResourceService>(owner));
            Assert.AreSame(asset, Global.GetService<ITestAssetService>(owner));
            Assert.AreSame(ui, Global.GetService<ITestUIService>(owner));

            Global.ReleaseAll(owner);
            Global.Unpin<TestUIService>();
            Global.Unpin<TestAssetService>();
            Global.Unpin<TestResourceService>();

            Assert.IsTrue(resource.IsDisposed);
            Assert.IsTrue(asset.IsDisposed);
            Assert.IsTrue(ui.IsDisposed);
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 验证一个迁移服务声明的接口集合和依赖集合。
        /// </summary>
        /// <typeparam name="TService">待验证的服务实现类型。</typeparam>
        /// <typeparam name="TContract">服务必须公开的主接口类型。</typeparam>
        /// <param name="expectedDependencies">预期依赖的服务接口集合。</param>
        private static void AssertServiceContract<TService, TContract>(params Type[] expectedDependencies)
            where TService : AAppService, TContract
            where TContract : IAppService
        {
            AppServiceAttribute attribute = (AppServiceAttribute)Attribute.GetCustomAttributes(typeof(TService), typeof(AppServiceAttribute)).Single();

            Assert.Contains(typeof(TContract), attribute.ServiceTypes);
            CollectionAssert.AreEquivalent(expectedDependencies, attribute.RequiresServices ?? Array.Empty<Type>());
        }

        /// <summary>
        /// 用于验证资源服务注册生命周期的测试接口。
        /// </summary>
        private interface ITestResourceService : IAppService
        {
        }

        /// <summary>
        /// 用于验证资产服务注册生命周期的测试接口。
        /// </summary>
        private interface ITestAssetService : IAppService
        {
        }

        /// <summary>
        /// 用于验证 UI 服务注册生命周期的测试接口。
        /// </summary>
        private interface ITestUIService : IAppService
        {
        }

        /// <summary>
        /// 资源服务的最小测试实现。
        /// </summary>
        private sealed class TestResourceService : AAppService, ITestResourceService
        {
        }

        /// <summary>
        /// 资产服务的最小测试实现。
        /// </summary>
        private sealed class TestAssetService : AAppService, ITestAssetService
        {
        }

        /// <summary>
        /// UI 服务的最小测试实现。
        /// </summary>
        private sealed class TestUIService : AAppService, ITestUIService
        {
        }

        #endregion
    }
}
