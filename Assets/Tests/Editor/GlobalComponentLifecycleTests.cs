using System;
using MiniCore.Core;
using MiniCore.Model;
using NUnit.Framework;

namespace MiniCore.EditorTests
{
    /// <summary>
    /// 验证 Global 基于 owner 的直接组件引用计数生命周期规则。
    /// </summary>
    public sealed class GlobalComponentLifecycleTests
    {
        #region Public 公共成员

        /// <summary>
        /// 创建隔离的 Global 测试容器并重置组件统计数据。
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            CountingComponent.DisposeCount = 0;
            CountingComponent.AwakeCount = 0;
            TickAddedComponent.AwakeCount = 0;
            TickAddedComponent.DisposeCount = 0;
            Global.Shutdown();
            Global.Initialize();
        }

        /// <summary>
        /// 销毁测试容器，避免组件注册状态影响后续用例。
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            Global.Shutdown();
        }

        /// <summary>
        /// 验证不同 owner 获取的是同一实例，且最后一个 owner 释放后才会销毁。
        /// </summary>
        [Test]
        public void GetOrAdd_MultipleOwners_DisposesOnlyAfterLastRemove()
        {
            object firstOwner = new object();
            object secondOwner = new object();
            CountingComponent firstComponent = Global.GetOrAdd<CountingComponent>(firstOwner);
            CountingComponent secondComponent = Global.GetOrAdd<CountingComponent>(secondOwner);

            Assert.AreSame(firstComponent, secondComponent);

            Global.Remove<CountingComponent>(firstOwner);

            Assert.IsFalse(firstComponent.IsDisposed);
            Assert.AreEqual(0, CountingComponent.DisposeCount);

            Global.Remove<CountingComponent>(secondOwner);

            Assert.IsTrue(firstComponent.IsDisposed);
            Assert.IsFalse(firstComponent.IsActive);
            Assert.AreEqual(1, CountingComponent.DisposeCount);
        }

        /// <summary>
        /// 验证未持有组件的 owner 无法释放其他系统持有的引用。
        /// </summary>
        [Test]
        public void Remove_OwnerDoesNotHoldReference_ThrowsInDevelopment()
        {
            object holdingOwner = new object();
            object invalidOwner = new object();
            Global.GetOrAdd<CountingComponent>(holdingOwner);

            Assert.Throws<InvalidOperationException>(() => Global.Remove<CountingComponent>(invalidOwner));

            Global.Remove<CountingComponent>(holdingOwner);
        }

        /// <summary>
        /// 验证 ReleaseAll 会清理指定 owner 的所有重复获取引用，但不会影响其他 owner。
        /// </summary>
        [Test]
        public void ReleaseAll_ReleasesOnlySpecifiedOwnerReferences()
        {
            object firstOwner = new object();
            object secondOwner = new object();
            CountingComponent component = Global.GetOrAdd<CountingComponent>(firstOwner);
            Global.Get<CountingComponent>(firstOwner);
            Global.Get<CountingComponent>(secondOwner);

            Global.ReleaseAll(firstOwner);

            Assert.IsFalse(component.IsDisposed);
            Global.Remove<CountingComponent>(secondOwner);
            Assert.IsTrue(component.IsDisposed);
        }

        /// <summary>
        /// 验证 Pin 提供的根 owner 会使组件在普通 owner 全部释放后继续存活。
        /// </summary>
        [Test]
        public void Pin_NormalOwnerReleased_ComponentRemainsUntilUnpin()
        {
            object owner = new object();
            CountingComponent pinnedComponent = Global.Pin<CountingComponent>();
            CountingComponent ownerComponent = Global.Get<CountingComponent>(owner);

            Assert.AreSame(pinnedComponent, ownerComponent);

            Global.Remove<CountingComponent>(owner);

            Assert.IsFalse(pinnedComponent.IsDisposed);
            Global.Unpin<CountingComponent>();
            Assert.IsTrue(pinnedComponent.IsDisposed);
            Assert.AreEqual(1, CountingComponent.DisposeCount);
        }

        /// <summary>
        /// 验证 Scope 释放时只归还该 Scope 获取的全部组件引用。
        /// </summary>
        [Test]
        public void Scope_Dispose_ReleasesOwnedComponents()
        {
            CountingComponent component;
            using (GlobalScope scope = Global.CreateScope("GlobalComponentLifecycleTests"))
            {
                component = scope.GetOrAdd<CountingComponent>();
                scope.Get<CountingComponent>();
                Assert.IsFalse(component.IsDisposed);
            }

            Assert.IsTrue(component.IsDisposed);
            Assert.AreEqual(1, CountingComponent.DisposeCount);
        }

        /// <summary>
        /// 验证 ForceRemove 会忽略所有现存引用并立即销毁组件。
        /// </summary>
        [Test]
        public void ForceRemove_ClearsAllOwnerReferencesAndDisposesComponent()
        {
            object firstOwner = new object();
            object secondOwner = new object();
            CountingComponent component = Global.GetOrAdd<CountingComponent>(firstOwner);
            Global.Get<CountingComponent>(secondOwner);

            Global.ForceRemove<CountingComponent>();

            Assert.IsTrue(component.IsDisposed);
            Assert.Throws<InvalidOperationException>(() => Global.Get<CountingComponent>(firstOwner));
        }

        /// <summary>
        /// 验证最后一份引用释放后，同类型组件可以安全创建新的实例。
        /// </summary>
        [Test]
        public void Remove_LastReferenceReleased_CanCreateNewComponent()
        {
            object owner = new object();
            CountingComponent firstComponent = Global.GetOrAdd<CountingComponent>(owner);

            Global.Remove<CountingComponent>(owner);
            CountingComponent secondComponent = Global.GetOrAdd<CountingComponent>(owner);

            Assert.AreNotSame(firstComponent, secondComponent);
            Assert.AreEqual(2, CountingComponent.AwakeCount);
        }

        /// <summary>
        /// 验证 Global.Shutdown 无论引用是否归零都会强制回收组件。
        /// </summary>
        [Test]
        public void Shutdown_ReferencesRemain_ForceDisposesAllComponents()
        {
            object owner = new object();
            CountingComponent component = Global.GetOrAdd<CountingComponent>(owner);

            Global.Shutdown();

            Assert.IsTrue(component.IsDisposed);
            Assert.AreEqual(1, CountingComponent.DisposeCount);
        }

        /// <summary>
        /// 验证 Tick 期间增删组件时使用快照，不会破坏当前调度。
        /// </summary>
        [Test]
        public void Tick_ComponentMutatesGlobalRegistry_UsesStableSnapshot()
        {
            object owner = new object();
            TickMutationComponent component = Global.GetOrAdd<TickMutationComponent>(owner);

            Global.Tick();

            Assert.AreEqual(1, component.UpdateCount);
            Assert.AreEqual(1, TickAddedComponent.AwakeCount);
            Assert.AreEqual(1, TickAddedComponent.DisposeCount);
            Global.Remove<TickMutationComponent>(owner);
        }

        /// <summary>
        /// 验证带强类型参数的组件仅在首次创建时初始化一次，后续获取不会覆盖原有配置。
        /// </summary>
        [Test]
        public void GetOrAdd_WithArgs_InitializesOnlyOnce()
        {
            object firstOwner = new object();
            object secondOwner = new object();
            ArgumentComponent firstComponent = Global.GetOrAdd<ArgumentComponent>(firstOwner, new TestInitArgs(42));
            ArgumentComponent secondComponent = Global.GetOrAdd<ArgumentComponent>(secondOwner, new TestInitArgs(7));

            Assert.AreSame(firstComponent, secondComponent);
            Assert.AreEqual(42, firstComponent.ReceivedValue);
            Assert.AreEqual(1, firstComponent.AwakeCount);
            Assert.Throws<ArgumentException>(() => new ArgumentComponent().Awake(new OtherInitArgs()));
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 用于验证引用释放次数的测试组件。
        /// </summary>
        private sealed class CountingComponent : AComponent
        {
            #region Public 公共成员

            /// <summary>
            /// 获取或设置已执行 Dispose 的测试组件数量。
            /// </summary>
            public static int DisposeCount { get; set; }

            /// <summary>
            /// 获取或设置已执行 Awake 的测试组件数量。
            /// </summary>
            public static int AwakeCount { get; set; }

            /// <summary>
            /// 创建测试组件实例。
            /// </summary>
            public CountingComponent()
            {
            }

            #endregion

            #region Override 重写实现

            /// <summary>
            /// 记录初始化次数。
            /// </summary>
            public override void Awake()
            {
                AwakeCount++;
            }

            /// <summary>
            /// 记录释放次数并执行基类资源释放。
            /// </summary>
            public override void Dispose()
            {
                DisposeCount++;
                base.Dispose();
            }

            #endregion
        }

        /// <summary>
        /// 需要强类型初始化参数的测试组件。
        /// </summary>
        private sealed class ArgumentComponent : AComponent<TestInitArgs>
        {
            #region Public 公共成员

            /// <summary>
            /// 获取最近一次初始化收到的数值。
            /// </summary>
            public int ReceivedValue { get; private set; }

            /// <summary>
            /// 获取或设置已执行强类型 Awake 的次数。
            /// </summary>
            public int AwakeCount { get; private set; }

            /// <summary>
            /// 创建测试组件实例。
            /// </summary>
            public ArgumentComponent()
            {
            }

            #endregion

            #region Override 重写实现

            /// <summary>
            /// 保存强类型初始化参数中的测试值。
            /// </summary>
            /// <param name="args">已通过类型校验的测试初始化参数。</param>
            protected override void Awake(TestInitArgs args)
            {
                AwakeCount++;
                ReceivedValue = args.Value;
            }

            #endregion
        }

        /// <summary>
        /// 在 Tick 中临时注册并释放另一组件的测试组件。
        /// </summary>
        private sealed class TickMutationComponent : AComponent
        {
            #region Public 公共成员

            /// <summary>
            /// 获取当前组件已执行的更新次数。
            /// </summary>
            public int UpdateCount { get; private set; }

            /// <summary>
            /// 在首帧增删组件，验证调度快照隔离。
            /// </summary>
            protected override void Update()
            {
                UpdateCount++;
                TickAddedComponent added = Global.GetOrAdd<TickAddedComponent>(this);
                Global.Remove<TickAddedComponent>(this);
                Assert.IsTrue(added.IsDisposed);
            }

            #endregion
        }

        /// <summary>
        /// 用于验证 Tick 中增删生命周期的测试组件。
        /// </summary>
        private sealed class TickAddedComponent : AComponent
        {
            #region Public 公共成员

            /// <summary>
            /// 获取或设置初始化次数。
            /// </summary>
            public static int AwakeCount { get; set; }

            /// <summary>
            /// 获取或设置释放次数。
            /// </summary>
            public static int DisposeCount { get; set; }

            /// <summary>
            /// 记录组件初始化。
            /// </summary>
            public override void Awake()
            {
                AwakeCount++;
            }

            /// <summary>
            /// 记录组件释放。
            /// </summary>
            public override void Dispose()
            {
                DisposeCount++;
                base.Dispose();
            }

            #endregion
        }

        /// <summary>
        /// 强类型组件所需的测试初始化参数。
        /// </summary>
        private sealed class TestInitArgs : ComponentInitArgs
        {
            #region Public 公共成员

            /// <summary>
            /// 获取初始化时传递的测试值。
            /// </summary>
            public int Value { get; }

            /// <summary>
            /// 使用测试值创建初始化参数。
            /// </summary>
            /// <param name="value">要传递给测试组件的数值。</param>
            public TestInitArgs(int value)
            {
                Value = value;
            }

            #endregion
        }

        /// <summary>
        /// 用于验证参数类型错误的另一种初始化参数。
        /// </summary>
        private sealed class OtherInitArgs : ComponentInitArgs
        {
        }

        #endregion
    }
}
