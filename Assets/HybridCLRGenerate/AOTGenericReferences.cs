using System.Collections.Generic;
public class AOTGenericReferences : UnityEngine.MonoBehaviour
{

	// {{ AOT assemblies
	public static readonly IReadOnlyList<string> PatchedAOTAssemblyList = new List<string>
	{
		"Google.Protobuf.dll",
		"MiniCore.Network.dll",
		"MiniCore.Runtime.dll",
		"MiniCore.Serialization.dll",
		"MiniCore.Unity.dll",
		"Newtonsoft.Json.dll",
		"System.Core.dll",
		"Unity.InputSystem.dll",
		"UnityEngine.CoreModule.dll",
		"mscorlib.dll",
	};
	// }}

	// {{ constraint implement type
	// }} 

	// {{ AOT generic types
	// Google.Protobuf.Collections.RepeatedField.<GetEnumerator>d__28<object>
	// Google.Protobuf.Collections.RepeatedField<object>
	// Google.Protobuf.FieldCodec.<>c<object>
	// Google.Protobuf.FieldCodec.<>c__DisplayClass38_0<object>
	// Google.Protobuf.FieldCodec.<>c__DisplayClass39_0<object>
	// Google.Protobuf.FieldCodec.InputMerger<object>
	// Google.Protobuf.FieldCodec.ValuesMerger<object>
	// Google.Protobuf.FieldCodec<object>
	// Google.Protobuf.IDeepCloneable<object>
	// Google.Protobuf.IMessage<object>
	// Google.Protobuf.MessageParser.<>c__DisplayClass2_0<object>
	// Google.Protobuf.MessageParser<object>
	// Google.Protobuf.ValueReader<object>
	// Google.Protobuf.ValueWriter<object>
	// MiniCore.Core.Global.<>c__16<object,object>
	// MiniCore.Core.Global.<>c__19<object,object>
	// MiniCore.Core.GlobalModuleRegistry.<>c__DisplayClass1_0<object>
	// MiniCore.Core.GlobalServiceRegistry.<>c__DisplayClass2_0<object,object>
	// MiniCore.Model.AMHandler<object>
	// MiniCore.Threading.IMTaskSource<MiniCore.Demo.MiniBomber.MiniBomberCommandResult>
	// MiniCore.Threading.IMTaskSource<byte>
	// MiniCore.Threading.IMTaskSource<object>
	// MiniCore.Threading.MSharedTask<MiniCore.Demo.MiniBomber.MiniBomberCommandResult>
	// MiniCore.Threading.MSharedTask<byte>
	// MiniCore.Threading.MSharedTask<object>
	// MiniCore.Threading.MSharedTaskAwaiter<MiniCore.Demo.MiniBomber.MiniBomberCommandResult>
	// MiniCore.Threading.MSharedTaskAwaiter<byte>
	// MiniCore.Threading.MSharedTaskAwaiter<object>
	// MiniCore.Threading.MSharedTaskWaiter<MiniCore.Demo.MiniBomber.MiniBomberCommandResult>
	// MiniCore.Threading.MSharedTaskWaiter<byte>
	// MiniCore.Threading.MSharedTaskWaiter<object>
	// MiniCore.Threading.MTask<MiniCore.Demo.MiniBomber.MiniBomberCommandResult>
	// MiniCore.Threading.MTask<byte>
	// MiniCore.Threading.MTask<object>
	// MiniCore.Threading.MTaskAwaiter<MiniCore.Demo.MiniBomber.MiniBomberCommandResult>
	// MiniCore.Threading.MTaskAwaiter<byte>
	// MiniCore.Threading.MTaskAwaiter<object>
	// MiniCore.Threading.MTaskForgetObserver<MiniCore.Demo.MiniBomber.MiniBomberCommandResult>
	// MiniCore.Threading.MTaskForgetObserver<byte>
	// MiniCore.Threading.MTaskForgetObserver<object>
	// MiniCore.Threading.MTaskMethodBuilder<MiniCore.Demo.MiniBomber.MiniBomberCommandResult>
	// MiniCore.Threading.MTaskMethodBuilder<byte>
	// MiniCore.Threading.MTaskMethodBuilder<object>
	// MiniCore.Threading.MTaskPromise<MiniCore.Demo.MiniBomber.MiniBomberCommandResult>
	// MiniCore.Threading.MTaskPromise<byte>
	// MiniCore.Threading.MTaskPromise<object>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.AccountSessionComponent.<ConnectAsync>d__23>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.AccountSessionComponent.<ConnectLobbyAsync>d__36>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.AccountSessionComponent.<InitializeAsync>d__22>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.AccountSessionComponent.<LoginAsync>d__25>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.AccountSessionComponent.<LogoutAsync>d__27>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.AccountSessionComponent.<RegisterAsync>d__24>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.AccountSessionComponent.<ResolveLobbyAsync>d__35>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.AccountSessionComponent.<ResumeAsync>d__26>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.BattleClientComponent.<RequestResyncAsync>d__26>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.BattleHudWindowPresenter.<ClearKillFeedAsync>d__12>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.BattleHudWindowPresenter.<RefreshPerformanceLoopAsync>d__13>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.CreateRoomPopupPresenter.<SubmitAsync>d__8>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.LobbyComponent.<RefreshAsync>d__9>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.LobbyWindowPresenter.<JoinAsync>d__15>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.LobbyWindowPresenter.<LogoutAsync>d__17>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.LobbyWindowPresenter.<OpenCreateAsync>d__13>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.LobbyWindowPresenter.<RefreshAsync>d__11>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.LoginWindowPresenter.<LoginAsync>d__7>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.LoginWindowPresenter.<OpenRegisterAsync>d__9>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.MiniBomberBattlePresentationComponent.<ToggleNetworkDebugAsync>d__28>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<CloseBattleHudAsync>d__35>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<CloseLoadingAsync>d__38>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<HandleMatchPrepareModelAsync>d__26>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<HandleNonResumableDisconnectAsync>d__39>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<InitializeAsync>d__20>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<LoadSceneAsync>d__37>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<NavigateAsync>d__21>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<NavigateWindowIfAvailableAsync>d__42>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<OpenBattleHudAsync>d__34>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<OpenReconnectOverlayAsync>d__28>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<OpenResultAsync>d__31>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<OpenWindowIfAvailableAsync>d__41>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<ReconnectAsync>d__27>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<ReturnToRoomAsync>d__33>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.MiniBomberClientStartupComponent.<InitializeAsync>d__8>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.MiniBomberStartupComponentBase.<LoadConfigurationAsync>d__16>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.NetworkDebugWindowPresenter.<RefreshLoopAsync>d__6>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.RegisterWindowPresenter.<SubmitAsync>d__6>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.RoomComponent.<CreateAsync>d__11>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.RoomComponent.<JoinAsync>d__12>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.RoomComponent.<LeaveAsync>d__13>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.RoomComponent.<SetReadyAsync>d__15>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.RoomComponent.<StartMatchAsync>d__16>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.RoomComponent.<UpdateSettingsAsync>d__14>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.RoomWindowPresenter.<ApplySettingsAsync>d__11>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.RoomWindowPresenter.<LeaveAsync>d__17>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.RoomWindowPresenter.<StartMatchAsync>d__15>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.RoomWindowPresenter.<ToggleReadyAsync>d__13>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.HotUpdate.GameStartup.<StartAsync>d__0>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.HotUpdate.KcpTestWindowPresenter.<ConnectClientAsync>d__18>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.HotUpdate.KcpTestWindowPresenter.<DisconnectClientAsync>d__19>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.HotUpdate.KcpTestWindowPresenter.<HandleClientDisconnectedAsync>d__21>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.HotUpdate.KcpTestWindowPresenter.<SendNormalAsync>d__24>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.HotUpdate.KcpTestWindowPresenter.<SendRpcAsync>d__25>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.HotUpdate.KcpTestWindowPresenter.<StartServerAsync>d__16>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.HotUpdate.KcpTestWindowPresenter.<StopServerAsync>d__20>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.HotUpdate.MiniCoreStartup.<StartAsync>d__0>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.HotUpdate.MiniCoreStartup.<StartConfiguredAsync>d__1>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectKcpAsync>d__48>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectTcpAsync>d__47>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectUdpAsync>d__49>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.HotUpdate.MultiProtocolTestPanel.<SendNormalAsync>d__51>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.HotUpdate.MultiProtocolTestPanel.<SendRpcAsync>d__52>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.HotUpdate.MultiProtocolTestPanel.<StartKcpServerAsync>d__42>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.HotUpdate.MultiProtocolTestPanel.<StartTcpServerAsync>d__41>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.HotUpdate.MultiProtocolTestPanel.<StartUdpServerAsync>d__43>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.HotUpdate.TestHandler.<HandleAsync>d__0>
	// MiniCore.UI.AUIWindowPresenter<object>
	// System.Action<MiniCore.Demo.MiniBomber.Unity.BomberInputFrame>
	// System.Action<UnityEngine.InputSystem.InputAction.CallbackContext>
	// System.Action<float>
	// System.Action<long>
	// System.Action<object>
	// System.ArraySegment.Enumerator<byte>
	// System.ArraySegment<byte>
	// System.Buffers.MemoryManager<byte>
	// System.ByReference<byte>
	// System.Collections.Concurrent.ConcurrentQueue.<Enumerate>d__28<object>
	// System.Collections.Concurrent.ConcurrentQueue.Segment<object>
	// System.Collections.Concurrent.ConcurrentQueue<object>
	// System.Collections.Generic.ArraySortHelper<long>
	// System.Collections.Generic.ArraySortHelper<object>
	// System.Collections.Generic.Comparer<long>
	// System.Collections.Generic.Comparer<object>
	// System.Collections.Generic.Dictionary.Enumerator<int,object>
	// System.Collections.Generic.Dictionary.Enumerator<long,object>
	// System.Collections.Generic.Dictionary.Enumerator<object,int>
	// System.Collections.Generic.Dictionary.Enumerator<object,object>
	// System.Collections.Generic.Dictionary.Enumerator<uint,object>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<int,object>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<long,object>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<object,int>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<object,object>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<uint,object>
	// System.Collections.Generic.Dictionary.KeyCollection<int,object>
	// System.Collections.Generic.Dictionary.KeyCollection<long,object>
	// System.Collections.Generic.Dictionary.KeyCollection<object,int>
	// System.Collections.Generic.Dictionary.KeyCollection<object,object>
	// System.Collections.Generic.Dictionary.KeyCollection<uint,object>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<int,object>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<long,object>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<object,int>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<object,object>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<uint,object>
	// System.Collections.Generic.Dictionary.ValueCollection<int,object>
	// System.Collections.Generic.Dictionary.ValueCollection<long,object>
	// System.Collections.Generic.Dictionary.ValueCollection<object,int>
	// System.Collections.Generic.Dictionary.ValueCollection<object,object>
	// System.Collections.Generic.Dictionary.ValueCollection<uint,object>
	// System.Collections.Generic.Dictionary<int,object>
	// System.Collections.Generic.Dictionary<long,object>
	// System.Collections.Generic.Dictionary<object,int>
	// System.Collections.Generic.Dictionary<object,object>
	// System.Collections.Generic.Dictionary<uint,object>
	// System.Collections.Generic.EqualityComparer<int>
	// System.Collections.Generic.EqualityComparer<long>
	// System.Collections.Generic.EqualityComparer<object>
	// System.Collections.Generic.EqualityComparer<uint>
	// System.Collections.Generic.HashSet.Enumerator<long>
	// System.Collections.Generic.HashSet.Enumerator<object>
	// System.Collections.Generic.HashSet<long>
	// System.Collections.Generic.HashSet<object>
	// System.Collections.Generic.HashSetEqualityComparer<long>
	// System.Collections.Generic.HashSetEqualityComparer<object>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int,object>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<long,object>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<object,int>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<uint,object>>
	// System.Collections.Generic.ICollection<long>
	// System.Collections.Generic.ICollection<object>
	// System.Collections.Generic.IComparer<long>
	// System.Collections.Generic.IComparer<object>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<int,object>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<long,object>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,int>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<uint,object>>
	// System.Collections.Generic.IEnumerable<long>
	// System.Collections.Generic.IEnumerable<object>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<int,object>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<long,object>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<object,int>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<uint,object>>
	// System.Collections.Generic.IEnumerator<long>
	// System.Collections.Generic.IEnumerator<object>
	// System.Collections.Generic.IEqualityComparer<int>
	// System.Collections.Generic.IEqualityComparer<long>
	// System.Collections.Generic.IEqualityComparer<object>
	// System.Collections.Generic.IEqualityComparer<uint>
	// System.Collections.Generic.IList<long>
	// System.Collections.Generic.IList<object>
	// System.Collections.Generic.IReadOnlyCollection<byte>
	// System.Collections.Generic.IReadOnlyCollection<object>
	// System.Collections.Generic.IReadOnlyList<byte>
	// System.Collections.Generic.IReadOnlyList<object>
	// System.Collections.Generic.KeyValuePair<int,object>
	// System.Collections.Generic.KeyValuePair<long,object>
	// System.Collections.Generic.KeyValuePair<object,int>
	// System.Collections.Generic.KeyValuePair<object,object>
	// System.Collections.Generic.KeyValuePair<uint,object>
	// System.Collections.Generic.List.Enumerator<long>
	// System.Collections.Generic.List.Enumerator<object>
	// System.Collections.Generic.List<long>
	// System.Collections.Generic.List<object>
	// System.Collections.Generic.ObjectComparer<long>
	// System.Collections.Generic.ObjectComparer<object>
	// System.Collections.Generic.ObjectEqualityComparer<int>
	// System.Collections.Generic.ObjectEqualityComparer<long>
	// System.Collections.Generic.ObjectEqualityComparer<object>
	// System.Collections.Generic.ObjectEqualityComparer<uint>
	// System.Collections.Generic.Stack.Enumerator<object>
	// System.Collections.Generic.Stack<object>
	// System.Collections.ObjectModel.ReadOnlyCollection<long>
	// System.Collections.ObjectModel.ReadOnlyCollection<object>
	// System.Comparison<long>
	// System.Comparison<object>
	// System.Func<object,int>
	// System.Func<object,object,object>
	// System.Func<object,object>
	// System.Func<object>
	// System.IEquatable<object>
	// System.Memory<byte>
	// System.Predicate<long>
	// System.Predicate<object>
	// System.ReadOnlyMemory<byte>
	// System.ReadOnlySpan<byte>
	// System.Span<byte>
	// UnityEngine.InputSystem.InputBindingComposite<UnityEngine.Vector2>
	// UnityEngine.InputSystem.InputControl<UnityEngine.Vector2>
	// UnityEngine.InputSystem.InputProcessor<UnityEngine.Vector2>
	// UnityEngine.InputSystem.Utilities.InlinedArray<object>
	// }}

	public void RefMethods()
	{
		// System.Void MiniCore.Core.Global.BindAppService<object,object>()
		// object MiniCore.Core.Global.Get<object>(object)
		// object MiniCore.Core.Global.GetOrAdd<object>(object)
		// object MiniCore.Core.Global.GetOrAddModule<object>(object,MiniCore.Model.ComponentInitArgs)
		// object MiniCore.Core.Global.GetOrAddModule<object>(string,object,MiniCore.Model.ComponentInitArgs)
		// object MiniCore.Core.Global.GetService<object>(object)
		// object MiniCore.Core.Global.Pin<object>()
		// System.Void MiniCore.Core.Global.RegisterAppModule<object,object>(string)
		// object MiniCore.Core.Global.RegisterAppService<object,object>(MiniCore.Model.ComponentInitArgs)
		// System.Void MiniCore.Core.Global.ThrowIfDirectAppServiceAccess<object>()
		// object MiniCore.Core.GlobalModuleRegistry.GetOrAdd<object>(string,object,MiniCore.Model.ComponentInitArgs)
		// System.Void MiniCore.Core.GlobalModuleRegistry.Register<object>(string,System.Func<object,MiniCore.Model.ComponentInitArgs,object>)
		// object MiniCore.Core.GlobalRuntime.Get<object>(object)
		// object MiniCore.Core.GlobalRuntime.GetOrAdd<object>(object)
		// object MiniCore.Core.GlobalRuntime.GetOrCreate<object>(object,MiniCore.Model.ComponentInitArgs)
		// object MiniCore.Core.GlobalRuntime.Pin<object>()
		// object MiniCore.Core.GlobalRuntime.Pin<object>(MiniCore.Model.ComponentInitArgs)
		// object MiniCore.Core.GlobalRuntime.PinInternal<object>(MiniCore.Model.ComponentInitArgs)
		// object MiniCore.Core.GlobalServiceRegistry.Get<object>(object)
		// System.Void MiniCore.Core.GlobalServiceRegistry.Register<object,object>(System.Func<object,object>)
		// System.Void MiniCore.Eventing.IEventBus.Publish<object>(object)
		// MiniCore.Eventing.EventSubscription MiniCore.Eventing.IEventBus.Subscribe<object>(System.Action<object>)
		// System.Void MiniCore.Model.NetworkProtocolBuilder.RegisterMessage<object>(uint,MiniCore.Model.NetworkMessageRole,MiniCore.Serialization.IMessageParser)
		// MiniCore.Threading.MTask MiniCore.Serialization.ProtobufSaveServiceExtensions.SaveProtobufAsync<object>(MiniCore.Service.ISaveService,string,object)
		// MiniCore.Threading.MTask<object> MiniCore.Service.INetworkService.CallAsync<object,object>(string,object,int)
		// MiniCore.Threading.MTask MiniCore.Service.INetworkService.SendAsync<object>(string,object)
		// MiniCore.Model.NetworkSendResult MiniCore.Service.INetworkService.TrySend<object>(string,object)
		// MiniCore.Threading.MTask<object> MiniCore.Service.IResourceService.PreloadAssetAsync<object>(string)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.Demo.MiniBomber.AccountSessionComponent.<LogoutAsync>d__27>(MiniCore.Threading.MTaskAwaiter&,MiniCore.Demo.MiniBomber.AccountSessionComponent.<LogoutAsync>d__27&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.Demo.MiniBomber.BattleHudWindowPresenter.<ClearKillFeedAsync>d__12>(MiniCore.Threading.MTaskAwaiter&,MiniCore.Demo.MiniBomber.BattleHudWindowPresenter.<ClearKillFeedAsync>d__12&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.Demo.MiniBomber.BattleHudWindowPresenter.<RefreshPerformanceLoopAsync>d__13>(MiniCore.Threading.MTaskAwaiter&,MiniCore.Demo.MiniBomber.BattleHudWindowPresenter.<RefreshPerformanceLoopAsync>d__13&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.Demo.MiniBomber.CreateRoomPopupPresenter.<SubmitAsync>d__8>(MiniCore.Threading.MTaskAwaiter&,MiniCore.Demo.MiniBomber.CreateRoomPopupPresenter.<SubmitAsync>d__8&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.Demo.MiniBomber.LobbyWindowPresenter.<JoinAsync>d__15>(MiniCore.Threading.MTaskAwaiter&,MiniCore.Demo.MiniBomber.LobbyWindowPresenter.<JoinAsync>d__15&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.Demo.MiniBomber.LobbyWindowPresenter.<LogoutAsync>d__17>(MiniCore.Threading.MTaskAwaiter&,MiniCore.Demo.MiniBomber.LobbyWindowPresenter.<LogoutAsync>d__17&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.Demo.MiniBomber.LoginWindowPresenter.<LoginAsync>d__7>(MiniCore.Threading.MTaskAwaiter&,MiniCore.Demo.MiniBomber.LoginWindowPresenter.<LoginAsync>d__7&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.Demo.MiniBomber.MiniBomberBattlePresentationComponent.<ToggleNetworkDebugAsync>d__28>(MiniCore.Threading.MTaskAwaiter&,MiniCore.Demo.MiniBomber.MiniBomberBattlePresentationComponent.<ToggleNetworkDebugAsync>d__28&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<CloseBattleHudAsync>d__35>(MiniCore.Threading.MTaskAwaiter&,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<CloseBattleHudAsync>d__35&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<CloseLoadingAsync>d__38>(MiniCore.Threading.MTaskAwaiter&,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<CloseLoadingAsync>d__38&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<HandleMatchPrepareModelAsync>d__26>(MiniCore.Threading.MTaskAwaiter&,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<HandleMatchPrepareModelAsync>d__26&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<HandleNonResumableDisconnectAsync>d__39>(MiniCore.Threading.MTaskAwaiter&,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<HandleNonResumableDisconnectAsync>d__39&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<InitializeAsync>d__20>(MiniCore.Threading.MTaskAwaiter&,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<InitializeAsync>d__20&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<LoadSceneAsync>d__37>(MiniCore.Threading.MTaskAwaiter&,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<LoadSceneAsync>d__37&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<NavigateAsync>d__21>(MiniCore.Threading.MTaskAwaiter&,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<NavigateAsync>d__21&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<NavigateWindowIfAvailableAsync>d__42>(MiniCore.Threading.MTaskAwaiter&,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<NavigateWindowIfAvailableAsync>d__42&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<ReconnectAsync>d__27>(MiniCore.Threading.MTaskAwaiter&,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<ReconnectAsync>d__27&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<ReturnToRoomAsync>d__33>(MiniCore.Threading.MTaskAwaiter&,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<ReturnToRoomAsync>d__33&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.Demo.MiniBomber.MiniBomberClientStartupComponent.<InitializeAsync>d__8>(MiniCore.Threading.MTaskAwaiter&,MiniCore.Demo.MiniBomber.MiniBomberClientStartupComponent.<InitializeAsync>d__8&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.Demo.MiniBomber.NetworkDebugWindowPresenter.<RefreshLoopAsync>d__6>(MiniCore.Threading.MTaskAwaiter&,MiniCore.Demo.MiniBomber.NetworkDebugWindowPresenter.<RefreshLoopAsync>d__6&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.Demo.MiniBomber.RoomWindowPresenter.<LeaveAsync>d__17>(MiniCore.Threading.MTaskAwaiter&,MiniCore.Demo.MiniBomber.RoomWindowPresenter.<LeaveAsync>d__17&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.HotUpdate.GameStartup.<StartAsync>d__0>(MiniCore.Threading.MTaskAwaiter&,MiniCore.HotUpdate.GameStartup.<StartAsync>d__0&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.HotUpdate.KcpTestWindowPresenter.<DisconnectClientAsync>d__19>(MiniCore.Threading.MTaskAwaiter&,MiniCore.HotUpdate.KcpTestWindowPresenter.<DisconnectClientAsync>d__19&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.HotUpdate.KcpTestWindowPresenter.<SendNormalAsync>d__24>(MiniCore.Threading.MTaskAwaiter&,MiniCore.HotUpdate.KcpTestWindowPresenter.<SendNormalAsync>d__24&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.HotUpdate.KcpTestWindowPresenter.<StartServerAsync>d__16>(MiniCore.Threading.MTaskAwaiter&,MiniCore.HotUpdate.KcpTestWindowPresenter.<StartServerAsync>d__16&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.HotUpdate.KcpTestWindowPresenter.<StopServerAsync>d__20>(MiniCore.Threading.MTaskAwaiter&,MiniCore.HotUpdate.KcpTestWindowPresenter.<StopServerAsync>d__20&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.HotUpdate.MiniCoreStartup.<StartAsync>d__0>(MiniCore.Threading.MTaskAwaiter&,MiniCore.HotUpdate.MiniCoreStartup.<StartAsync>d__0&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.HotUpdate.MiniCoreStartup.<StartConfiguredAsync>d__1>(MiniCore.Threading.MTaskAwaiter&,MiniCore.HotUpdate.MiniCoreStartup.<StartConfiguredAsync>d__1&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.HotUpdate.MultiProtocolTestPanel.<SendNormalAsync>d__51>(MiniCore.Threading.MTaskAwaiter&,MiniCore.HotUpdate.MultiProtocolTestPanel.<SendNormalAsync>d__51&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.HotUpdate.MultiProtocolTestPanel.<StartKcpServerAsync>d__42>(MiniCore.Threading.MTaskAwaiter&,MiniCore.HotUpdate.MultiProtocolTestPanel.<StartKcpServerAsync>d__42&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.HotUpdate.MultiProtocolTestPanel.<StartTcpServerAsync>d__41>(MiniCore.Threading.MTaskAwaiter&,MiniCore.HotUpdate.MultiProtocolTestPanel.<StartTcpServerAsync>d__41&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.HotUpdate.MultiProtocolTestPanel.<StartUdpServerAsync>d__43>(MiniCore.Threading.MTaskAwaiter&,MiniCore.HotUpdate.MultiProtocolTestPanel.<StartUdpServerAsync>d__43&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.HotUpdate.TestHandler.<HandleAsync>d__0>(MiniCore.Threading.MTaskAwaiter&,MiniCore.HotUpdate.TestHandler.<HandleAsync>d__0&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<MiniCore.Demo.MiniBomber.MiniBomberCommandResult>,MiniCore.Demo.MiniBomber.CreateRoomPopupPresenter.<SubmitAsync>d__8>(MiniCore.Threading.MTaskAwaiter<MiniCore.Demo.MiniBomber.MiniBomberCommandResult>&,MiniCore.Demo.MiniBomber.CreateRoomPopupPresenter.<SubmitAsync>d__8&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<MiniCore.Demo.MiniBomber.MiniBomberCommandResult>,MiniCore.Demo.MiniBomber.LobbyWindowPresenter.<JoinAsync>d__15>(MiniCore.Threading.MTaskAwaiter<MiniCore.Demo.MiniBomber.MiniBomberCommandResult>&,MiniCore.Demo.MiniBomber.LobbyWindowPresenter.<JoinAsync>d__15&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<MiniCore.Demo.MiniBomber.MiniBomberCommandResult>,MiniCore.Demo.MiniBomber.LobbyWindowPresenter.<RefreshAsync>d__11>(MiniCore.Threading.MTaskAwaiter<MiniCore.Demo.MiniBomber.MiniBomberCommandResult>&,MiniCore.Demo.MiniBomber.LobbyWindowPresenter.<RefreshAsync>d__11&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<MiniCore.Demo.MiniBomber.MiniBomberCommandResult>,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<NavigateAsync>d__21>(MiniCore.Threading.MTaskAwaiter<MiniCore.Demo.MiniBomber.MiniBomberCommandResult>&,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<NavigateAsync>d__21&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<MiniCore.Demo.MiniBomber.MiniBomberCommandResult>,MiniCore.Demo.MiniBomber.RegisterWindowPresenter.<SubmitAsync>d__6>(MiniCore.Threading.MTaskAwaiter<MiniCore.Demo.MiniBomber.MiniBomberCommandResult>&,MiniCore.Demo.MiniBomber.RegisterWindowPresenter.<SubmitAsync>d__6&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<MiniCore.Demo.MiniBomber.MiniBomberCommandResult>,MiniCore.Demo.MiniBomber.RoomWindowPresenter.<ApplySettingsAsync>d__11>(MiniCore.Threading.MTaskAwaiter<MiniCore.Demo.MiniBomber.MiniBomberCommandResult>&,MiniCore.Demo.MiniBomber.RoomWindowPresenter.<ApplySettingsAsync>d__11&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<MiniCore.Demo.MiniBomber.MiniBomberCommandResult>,MiniCore.Demo.MiniBomber.RoomWindowPresenter.<LeaveAsync>d__17>(MiniCore.Threading.MTaskAwaiter<MiniCore.Demo.MiniBomber.MiniBomberCommandResult>&,MiniCore.Demo.MiniBomber.RoomWindowPresenter.<LeaveAsync>d__17&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<MiniCore.Demo.MiniBomber.MiniBomberCommandResult>,MiniCore.Demo.MiniBomber.RoomWindowPresenter.<StartMatchAsync>d__15>(MiniCore.Threading.MTaskAwaiter<MiniCore.Demo.MiniBomber.MiniBomberCommandResult>&,MiniCore.Demo.MiniBomber.RoomWindowPresenter.<StartMatchAsync>d__15&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<MiniCore.Demo.MiniBomber.MiniBomberCommandResult>,MiniCore.Demo.MiniBomber.RoomWindowPresenter.<ToggleReadyAsync>d__13>(MiniCore.Threading.MTaskAwaiter<MiniCore.Demo.MiniBomber.MiniBomberCommandResult>&,MiniCore.Demo.MiniBomber.RoomWindowPresenter.<ToggleReadyAsync>d__13&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<byte>,MiniCore.Demo.MiniBomber.LoginWindowPresenter.<LoginAsync>d__7>(MiniCore.Threading.MTaskAwaiter<byte>&,MiniCore.Demo.MiniBomber.LoginWindowPresenter.<LoginAsync>d__7&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<byte>,MiniCore.Demo.MiniBomber.LoginWindowPresenter.<OpenRegisterAsync>d__9>(MiniCore.Threading.MTaskAwaiter<byte>&,MiniCore.Demo.MiniBomber.LoginWindowPresenter.<OpenRegisterAsync>d__9&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<byte>,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<ReconnectAsync>d__27>(MiniCore.Threading.MTaskAwaiter<byte>&,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<ReconnectAsync>d__27&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<byte>,MiniCore.HotUpdate.KcpTestWindowPresenter.<ConnectClientAsync>d__18>(MiniCore.Threading.MTaskAwaiter<byte>&,MiniCore.HotUpdate.KcpTestWindowPresenter.<ConnectClientAsync>d__18&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<byte>,MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectKcpAsync>d__48>(MiniCore.Threading.MTaskAwaiter<byte>&,MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectKcpAsync>d__48&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<byte>,MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectTcpAsync>d__47>(MiniCore.Threading.MTaskAwaiter<byte>&,MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectTcpAsync>d__47&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<byte>,MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectUdpAsync>d__49>(MiniCore.Threading.MTaskAwaiter<byte>&,MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectUdpAsync>d__49&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Demo.MiniBomber.AccountSessionComponent.<InitializeAsync>d__22>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Demo.MiniBomber.AccountSessionComponent.<InitializeAsync>d__22&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Demo.MiniBomber.BattleClientComponent.<RequestResyncAsync>d__26>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Demo.MiniBomber.BattleClientComponent.<RequestResyncAsync>d__26&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Demo.MiniBomber.LobbyWindowPresenter.<OpenCreateAsync>d__13>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Demo.MiniBomber.LobbyWindowPresenter.<OpenCreateAsync>d__13&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Demo.MiniBomber.LoginWindowPresenter.<LoginAsync>d__7>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Demo.MiniBomber.LoginWindowPresenter.<LoginAsync>d__7&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Demo.MiniBomber.LoginWindowPresenter.<OpenRegisterAsync>d__9>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Demo.MiniBomber.LoginWindowPresenter.<OpenRegisterAsync>d__9&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Demo.MiniBomber.MiniBomberBattlePresentationComponent.<ToggleNetworkDebugAsync>d__28>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Demo.MiniBomber.MiniBomberBattlePresentationComponent.<ToggleNetworkDebugAsync>d__28&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<HandleMatchPrepareModelAsync>d__26>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<HandleMatchPrepareModelAsync>d__26&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<InitializeAsync>d__20>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<InitializeAsync>d__20&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<NavigateAsync>d__21>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<NavigateAsync>d__21&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<OpenBattleHudAsync>d__34>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<OpenBattleHudAsync>d__34&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<OpenReconnectOverlayAsync>d__28>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<OpenReconnectOverlayAsync>d__28&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<OpenResultAsync>d__31>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<OpenResultAsync>d__31&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<ReconnectAsync>d__27>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<ReconnectAsync>d__27&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Demo.MiniBomber.MiniBomberClientStartupComponent.<InitializeAsync>d__8>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Demo.MiniBomber.MiniBomberClientStartupComponent.<InitializeAsync>d__8&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Demo.MiniBomber.MiniBomberStartupComponentBase.<LoadConfigurationAsync>d__16>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Demo.MiniBomber.MiniBomberStartupComponentBase.<LoadConfigurationAsync>d__16&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.HotUpdate.KcpTestWindowPresenter.<SendRpcAsync>d__25>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.HotUpdate.KcpTestWindowPresenter.<SendRpcAsync>d__25&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.HotUpdate.MultiProtocolTestPanel.<SendRpcAsync>d__52>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.HotUpdate.MultiProtocolTestPanel.<SendRpcAsync>d__52&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskSwitchAwaiter,MiniCore.HotUpdate.KcpTestWindowPresenter.<HandleClientDisconnectedAsync>d__21>(MiniCore.Threading.MTaskSwitchAwaiter&,MiniCore.HotUpdate.KcpTestWindowPresenter.<HandleClientDisconnectedAsync>d__21&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<MiniCore.Demo.MiniBomber.MiniBomberCommandResult>.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Demo.MiniBomber.AccountSessionComponent.<RegisterAsync>d__24>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Demo.MiniBomber.AccountSessionComponent.<RegisterAsync>d__24&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<MiniCore.Demo.MiniBomber.MiniBomberCommandResult>.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Demo.MiniBomber.LobbyComponent.<RefreshAsync>d__9>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Demo.MiniBomber.LobbyComponent.<RefreshAsync>d__9&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<MiniCore.Demo.MiniBomber.MiniBomberCommandResult>.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Demo.MiniBomber.RoomComponent.<CreateAsync>d__11>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Demo.MiniBomber.RoomComponent.<CreateAsync>d__11&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<MiniCore.Demo.MiniBomber.MiniBomberCommandResult>.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Demo.MiniBomber.RoomComponent.<JoinAsync>d__12>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Demo.MiniBomber.RoomComponent.<JoinAsync>d__12&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<MiniCore.Demo.MiniBomber.MiniBomberCommandResult>.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Demo.MiniBomber.RoomComponent.<LeaveAsync>d__13>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Demo.MiniBomber.RoomComponent.<LeaveAsync>d__13&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<MiniCore.Demo.MiniBomber.MiniBomberCommandResult>.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Demo.MiniBomber.RoomComponent.<SetReadyAsync>d__15>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Demo.MiniBomber.RoomComponent.<SetReadyAsync>d__15&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<MiniCore.Demo.MiniBomber.MiniBomberCommandResult>.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Demo.MiniBomber.RoomComponent.<StartMatchAsync>d__16>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Demo.MiniBomber.RoomComponent.<StartMatchAsync>d__16&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<MiniCore.Demo.MiniBomber.MiniBomberCommandResult>.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Demo.MiniBomber.RoomComponent.<UpdateSettingsAsync>d__14>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Demo.MiniBomber.RoomComponent.<UpdateSettingsAsync>d__14&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<byte>.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<byte>,MiniCore.Demo.MiniBomber.AccountSessionComponent.<ConnectAsync>d__23>(MiniCore.Threading.MTaskAwaiter<byte>&,MiniCore.Demo.MiniBomber.AccountSessionComponent.<ConnectAsync>d__23&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<byte>.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<byte>,MiniCore.Demo.MiniBomber.AccountSessionComponent.<ConnectLobbyAsync>d__36>(MiniCore.Threading.MTaskAwaiter<byte>&,MiniCore.Demo.MiniBomber.AccountSessionComponent.<ConnectLobbyAsync>d__36&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.Demo.MiniBomber.AccountSessionComponent.<LoginAsync>d__25>(MiniCore.Threading.MTaskAwaiter&,MiniCore.Demo.MiniBomber.AccountSessionComponent.<LoginAsync>d__25&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.Demo.MiniBomber.AccountSessionComponent.<ResumeAsync>d__26>(MiniCore.Threading.MTaskAwaiter&,MiniCore.Demo.MiniBomber.AccountSessionComponent.<ResumeAsync>d__26&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<byte>,MiniCore.Demo.MiniBomber.AccountSessionComponent.<LoginAsync>d__25>(MiniCore.Threading.MTaskAwaiter<byte>&,MiniCore.Demo.MiniBomber.AccountSessionComponent.<LoginAsync>d__25&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<byte>,MiniCore.Demo.MiniBomber.AccountSessionComponent.<ResolveLobbyAsync>d__35>(MiniCore.Threading.MTaskAwaiter<byte>&,MiniCore.Demo.MiniBomber.AccountSessionComponent.<ResolveLobbyAsync>d__35&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Demo.MiniBomber.AccountSessionComponent.<LoginAsync>d__25>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Demo.MiniBomber.AccountSessionComponent.<LoginAsync>d__25&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Demo.MiniBomber.AccountSessionComponent.<ResolveLobbyAsync>d__35>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Demo.MiniBomber.AccountSessionComponent.<ResolveLobbyAsync>d__35&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Demo.MiniBomber.AccountSessionComponent.<ResumeAsync>d__26>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Demo.MiniBomber.AccountSessionComponent.<ResumeAsync>d__26&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<OpenWindowIfAvailableAsync>d__41>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<OpenWindowIfAvailableAsync>d__41&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.AccountSessionComponent.<InitializeAsync>d__22>(MiniCore.Demo.MiniBomber.AccountSessionComponent.<InitializeAsync>d__22&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.AccountSessionComponent.<LogoutAsync>d__27>(MiniCore.Demo.MiniBomber.AccountSessionComponent.<LogoutAsync>d__27&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.BattleClientComponent.<RequestResyncAsync>d__26>(MiniCore.Demo.MiniBomber.BattleClientComponent.<RequestResyncAsync>d__26&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.BattleHudWindowPresenter.<ClearKillFeedAsync>d__12>(MiniCore.Demo.MiniBomber.BattleHudWindowPresenter.<ClearKillFeedAsync>d__12&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.BattleHudWindowPresenter.<RefreshPerformanceLoopAsync>d__13>(MiniCore.Demo.MiniBomber.BattleHudWindowPresenter.<RefreshPerformanceLoopAsync>d__13&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.CreateRoomPopupPresenter.<SubmitAsync>d__8>(MiniCore.Demo.MiniBomber.CreateRoomPopupPresenter.<SubmitAsync>d__8&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.LobbyWindowPresenter.<JoinAsync>d__15>(MiniCore.Demo.MiniBomber.LobbyWindowPresenter.<JoinAsync>d__15&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.LobbyWindowPresenter.<LogoutAsync>d__17>(MiniCore.Demo.MiniBomber.LobbyWindowPresenter.<LogoutAsync>d__17&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.LobbyWindowPresenter.<OpenCreateAsync>d__13>(MiniCore.Demo.MiniBomber.LobbyWindowPresenter.<OpenCreateAsync>d__13&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.LobbyWindowPresenter.<RefreshAsync>d__11>(MiniCore.Demo.MiniBomber.LobbyWindowPresenter.<RefreshAsync>d__11&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.LoginWindowPresenter.<LoginAsync>d__7>(MiniCore.Demo.MiniBomber.LoginWindowPresenter.<LoginAsync>d__7&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.LoginWindowPresenter.<OpenRegisterAsync>d__9>(MiniCore.Demo.MiniBomber.LoginWindowPresenter.<OpenRegisterAsync>d__9&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.MiniBomberBattlePresentationComponent.<ToggleNetworkDebugAsync>d__28>(MiniCore.Demo.MiniBomber.MiniBomberBattlePresentationComponent.<ToggleNetworkDebugAsync>d__28&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<CloseBattleHudAsync>d__35>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<CloseBattleHudAsync>d__35&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<CloseLoadingAsync>d__38>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<CloseLoadingAsync>d__38&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<HandleMatchPrepareModelAsync>d__26>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<HandleMatchPrepareModelAsync>d__26&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<HandleNonResumableDisconnectAsync>d__39>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<HandleNonResumableDisconnectAsync>d__39&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<InitializeAsync>d__20>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<InitializeAsync>d__20&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<LoadSceneAsync>d__37>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<LoadSceneAsync>d__37&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<NavigateAsync>d__21>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<NavigateAsync>d__21&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<NavigateWindowIfAvailableAsync>d__42>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<NavigateWindowIfAvailableAsync>d__42&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<OpenBattleHudAsync>d__34>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<OpenBattleHudAsync>d__34&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<OpenReconnectOverlayAsync>d__28>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<OpenReconnectOverlayAsync>d__28&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<OpenResultAsync>d__31>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<OpenResultAsync>d__31&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<ReconnectAsync>d__27>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<ReconnectAsync>d__27&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<ReturnToRoomAsync>d__33>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<ReturnToRoomAsync>d__33&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.MiniBomberClientStartupComponent.<InitializeAsync>d__8>(MiniCore.Demo.MiniBomber.MiniBomberClientStartupComponent.<InitializeAsync>d__8&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.MiniBomberStartupComponentBase.<LoadConfigurationAsync>d__16>(MiniCore.Demo.MiniBomber.MiniBomberStartupComponentBase.<LoadConfigurationAsync>d__16&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.NetworkDebugWindowPresenter.<RefreshLoopAsync>d__6>(MiniCore.Demo.MiniBomber.NetworkDebugWindowPresenter.<RefreshLoopAsync>d__6&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.RegisterWindowPresenter.<SubmitAsync>d__6>(MiniCore.Demo.MiniBomber.RegisterWindowPresenter.<SubmitAsync>d__6&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.RoomWindowPresenter.<ApplySettingsAsync>d__11>(MiniCore.Demo.MiniBomber.RoomWindowPresenter.<ApplySettingsAsync>d__11&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.RoomWindowPresenter.<LeaveAsync>d__17>(MiniCore.Demo.MiniBomber.RoomWindowPresenter.<LeaveAsync>d__17&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.RoomWindowPresenter.<StartMatchAsync>d__15>(MiniCore.Demo.MiniBomber.RoomWindowPresenter.<StartMatchAsync>d__15&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.RoomWindowPresenter.<ToggleReadyAsync>d__13>(MiniCore.Demo.MiniBomber.RoomWindowPresenter.<ToggleReadyAsync>d__13&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.HotUpdate.GameStartup.<StartAsync>d__0>(MiniCore.HotUpdate.GameStartup.<StartAsync>d__0&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.HotUpdate.KcpTestWindowPresenter.<ConnectClientAsync>d__18>(MiniCore.HotUpdate.KcpTestWindowPresenter.<ConnectClientAsync>d__18&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.HotUpdate.KcpTestWindowPresenter.<DisconnectClientAsync>d__19>(MiniCore.HotUpdate.KcpTestWindowPresenter.<DisconnectClientAsync>d__19&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.HotUpdate.KcpTestWindowPresenter.<HandleClientDisconnectedAsync>d__21>(MiniCore.HotUpdate.KcpTestWindowPresenter.<HandleClientDisconnectedAsync>d__21&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.HotUpdate.KcpTestWindowPresenter.<SendNormalAsync>d__24>(MiniCore.HotUpdate.KcpTestWindowPresenter.<SendNormalAsync>d__24&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.HotUpdate.KcpTestWindowPresenter.<SendRpcAsync>d__25>(MiniCore.HotUpdate.KcpTestWindowPresenter.<SendRpcAsync>d__25&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.HotUpdate.KcpTestWindowPresenter.<StartServerAsync>d__16>(MiniCore.HotUpdate.KcpTestWindowPresenter.<StartServerAsync>d__16&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.HotUpdate.KcpTestWindowPresenter.<StopServerAsync>d__20>(MiniCore.HotUpdate.KcpTestWindowPresenter.<StopServerAsync>d__20&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.HotUpdate.MiniCoreStartup.<StartAsync>d__0>(MiniCore.HotUpdate.MiniCoreStartup.<StartAsync>d__0&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.HotUpdate.MiniCoreStartup.<StartConfiguredAsync>d__1>(MiniCore.HotUpdate.MiniCoreStartup.<StartConfiguredAsync>d__1&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectKcpAsync>d__48>(MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectKcpAsync>d__48&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectTcpAsync>d__47>(MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectTcpAsync>d__47&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectUdpAsync>d__49>(MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectUdpAsync>d__49&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.HotUpdate.MultiProtocolTestPanel.<SendNormalAsync>d__51>(MiniCore.HotUpdate.MultiProtocolTestPanel.<SendNormalAsync>d__51&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.HotUpdate.MultiProtocolTestPanel.<SendRpcAsync>d__52>(MiniCore.HotUpdate.MultiProtocolTestPanel.<SendRpcAsync>d__52&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.HotUpdate.MultiProtocolTestPanel.<StartKcpServerAsync>d__42>(MiniCore.HotUpdate.MultiProtocolTestPanel.<StartKcpServerAsync>d__42&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.HotUpdate.MultiProtocolTestPanel.<StartTcpServerAsync>d__41>(MiniCore.HotUpdate.MultiProtocolTestPanel.<StartTcpServerAsync>d__41&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.HotUpdate.MultiProtocolTestPanel.<StartUdpServerAsync>d__43>(MiniCore.HotUpdate.MultiProtocolTestPanel.<StartUdpServerAsync>d__43&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.HotUpdate.TestHandler.<HandleAsync>d__0>(MiniCore.HotUpdate.TestHandler.<HandleAsync>d__0&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<MiniCore.Demo.MiniBomber.MiniBomberCommandResult>.Start<MiniCore.Demo.MiniBomber.AccountSessionComponent.<RegisterAsync>d__24>(MiniCore.Demo.MiniBomber.AccountSessionComponent.<RegisterAsync>d__24&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<MiniCore.Demo.MiniBomber.MiniBomberCommandResult>.Start<MiniCore.Demo.MiniBomber.LobbyComponent.<RefreshAsync>d__9>(MiniCore.Demo.MiniBomber.LobbyComponent.<RefreshAsync>d__9&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<MiniCore.Demo.MiniBomber.MiniBomberCommandResult>.Start<MiniCore.Demo.MiniBomber.RoomComponent.<CreateAsync>d__11>(MiniCore.Demo.MiniBomber.RoomComponent.<CreateAsync>d__11&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<MiniCore.Demo.MiniBomber.MiniBomberCommandResult>.Start<MiniCore.Demo.MiniBomber.RoomComponent.<JoinAsync>d__12>(MiniCore.Demo.MiniBomber.RoomComponent.<JoinAsync>d__12&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<MiniCore.Demo.MiniBomber.MiniBomberCommandResult>.Start<MiniCore.Demo.MiniBomber.RoomComponent.<LeaveAsync>d__13>(MiniCore.Demo.MiniBomber.RoomComponent.<LeaveAsync>d__13&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<MiniCore.Demo.MiniBomber.MiniBomberCommandResult>.Start<MiniCore.Demo.MiniBomber.RoomComponent.<SetReadyAsync>d__15>(MiniCore.Demo.MiniBomber.RoomComponent.<SetReadyAsync>d__15&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<MiniCore.Demo.MiniBomber.MiniBomberCommandResult>.Start<MiniCore.Demo.MiniBomber.RoomComponent.<StartMatchAsync>d__16>(MiniCore.Demo.MiniBomber.RoomComponent.<StartMatchAsync>d__16&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<MiniCore.Demo.MiniBomber.MiniBomberCommandResult>.Start<MiniCore.Demo.MiniBomber.RoomComponent.<UpdateSettingsAsync>d__14>(MiniCore.Demo.MiniBomber.RoomComponent.<UpdateSettingsAsync>d__14&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<byte>.Start<MiniCore.Demo.MiniBomber.AccountSessionComponent.<ConnectAsync>d__23>(MiniCore.Demo.MiniBomber.AccountSessionComponent.<ConnectAsync>d__23&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<byte>.Start<MiniCore.Demo.MiniBomber.AccountSessionComponent.<ConnectLobbyAsync>d__36>(MiniCore.Demo.MiniBomber.AccountSessionComponent.<ConnectLobbyAsync>d__36&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.Start<MiniCore.Demo.MiniBomber.AccountSessionComponent.<LoginAsync>d__25>(MiniCore.Demo.MiniBomber.AccountSessionComponent.<LoginAsync>d__25&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.Start<MiniCore.Demo.MiniBomber.AccountSessionComponent.<ResolveLobbyAsync>d__35>(MiniCore.Demo.MiniBomber.AccountSessionComponent.<ResolveLobbyAsync>d__35&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.Start<MiniCore.Demo.MiniBomber.AccountSessionComponent.<ResumeAsync>d__26>(MiniCore.Demo.MiniBomber.AccountSessionComponent.<ResumeAsync>d__26&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.Start<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<OpenWindowIfAvailableAsync>d__41>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<OpenWindowIfAvailableAsync>d__41&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.Start<MiniCore.Serialization.ProtobufSaveServiceExtensions.<LoadProtobufAsync>d__1<object>>(MiniCore.Serialization.ProtobufSaveServiceExtensions.<LoadProtobufAsync>d__1<object>&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.AccountSessionComponent.<ConnectAsync>d__23>(MiniCore.Demo.MiniBomber.AccountSessionComponent.<ConnectAsync>d__23&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.AccountSessionComponent.<ConnectLobbyAsync>d__36>(MiniCore.Demo.MiniBomber.AccountSessionComponent.<ConnectLobbyAsync>d__36&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.AccountSessionComponent.<InitializeAsync>d__22>(MiniCore.Demo.MiniBomber.AccountSessionComponent.<InitializeAsync>d__22&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.AccountSessionComponent.<LoginAsync>d__25>(MiniCore.Demo.MiniBomber.AccountSessionComponent.<LoginAsync>d__25&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.AccountSessionComponent.<LogoutAsync>d__27>(MiniCore.Demo.MiniBomber.AccountSessionComponent.<LogoutAsync>d__27&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.AccountSessionComponent.<RegisterAsync>d__24>(MiniCore.Demo.MiniBomber.AccountSessionComponent.<RegisterAsync>d__24&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.AccountSessionComponent.<ResolveLobbyAsync>d__35>(MiniCore.Demo.MiniBomber.AccountSessionComponent.<ResolveLobbyAsync>d__35&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.AccountSessionComponent.<ResumeAsync>d__26>(MiniCore.Demo.MiniBomber.AccountSessionComponent.<ResumeAsync>d__26&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.BattleClientComponent.<RequestResyncAsync>d__26>(MiniCore.Demo.MiniBomber.BattleClientComponent.<RequestResyncAsync>d__26&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.BattleHudWindowPresenter.<ClearKillFeedAsync>d__12>(MiniCore.Demo.MiniBomber.BattleHudWindowPresenter.<ClearKillFeedAsync>d__12&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.BattleHudWindowPresenter.<RefreshPerformanceLoopAsync>d__13>(MiniCore.Demo.MiniBomber.BattleHudWindowPresenter.<RefreshPerformanceLoopAsync>d__13&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.CreateRoomPopupPresenter.<SubmitAsync>d__8>(MiniCore.Demo.MiniBomber.CreateRoomPopupPresenter.<SubmitAsync>d__8&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.LobbyComponent.<RefreshAsync>d__9>(MiniCore.Demo.MiniBomber.LobbyComponent.<RefreshAsync>d__9&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.LobbyWindowPresenter.<JoinAsync>d__15>(MiniCore.Demo.MiniBomber.LobbyWindowPresenter.<JoinAsync>d__15&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.LobbyWindowPresenter.<LogoutAsync>d__17>(MiniCore.Demo.MiniBomber.LobbyWindowPresenter.<LogoutAsync>d__17&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.LobbyWindowPresenter.<OpenCreateAsync>d__13>(MiniCore.Demo.MiniBomber.LobbyWindowPresenter.<OpenCreateAsync>d__13&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.LobbyWindowPresenter.<RefreshAsync>d__11>(MiniCore.Demo.MiniBomber.LobbyWindowPresenter.<RefreshAsync>d__11&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.LoginWindowPresenter.<LoginAsync>d__7>(MiniCore.Demo.MiniBomber.LoginWindowPresenter.<LoginAsync>d__7&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.LoginWindowPresenter.<OpenRegisterAsync>d__9>(MiniCore.Demo.MiniBomber.LoginWindowPresenter.<OpenRegisterAsync>d__9&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.MiniBomberBattlePresentationComponent.<ToggleNetworkDebugAsync>d__28>(MiniCore.Demo.MiniBomber.MiniBomberBattlePresentationComponent.<ToggleNetworkDebugAsync>d__28&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<CloseBattleHudAsync>d__35>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<CloseBattleHudAsync>d__35&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<CloseLoadingAsync>d__38>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<CloseLoadingAsync>d__38&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<HandleMatchPrepareModelAsync>d__26>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<HandleMatchPrepareModelAsync>d__26&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<HandleNonResumableDisconnectAsync>d__39>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<HandleNonResumableDisconnectAsync>d__39&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<InitializeAsync>d__20>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<InitializeAsync>d__20&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<LoadSceneAsync>d__37>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<LoadSceneAsync>d__37&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<NavigateAsync>d__21>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<NavigateAsync>d__21&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<NavigateWindowIfAvailableAsync>d__42>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<NavigateWindowIfAvailableAsync>d__42&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<OpenBattleHudAsync>d__34>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<OpenBattleHudAsync>d__34&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<OpenReconnectOverlayAsync>d__28>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<OpenReconnectOverlayAsync>d__28&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<OpenResultAsync>d__31>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<OpenResultAsync>d__31&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<OpenWindowIfAvailableAsync>d__41>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<OpenWindowIfAvailableAsync>d__41&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<ReconnectAsync>d__27>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<ReconnectAsync>d__27&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<ReturnToRoomAsync>d__33>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<ReturnToRoomAsync>d__33&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.MiniBomberClientStartupComponent.<InitializeAsync>d__8>(MiniCore.Demo.MiniBomber.MiniBomberClientStartupComponent.<InitializeAsync>d__8&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.MiniBomberStartupComponentBase.<LoadConfigurationAsync>d__16>(MiniCore.Demo.MiniBomber.MiniBomberStartupComponentBase.<LoadConfigurationAsync>d__16&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.NetworkDebugWindowPresenter.<RefreshLoopAsync>d__6>(MiniCore.Demo.MiniBomber.NetworkDebugWindowPresenter.<RefreshLoopAsync>d__6&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.RegisterWindowPresenter.<SubmitAsync>d__6>(MiniCore.Demo.MiniBomber.RegisterWindowPresenter.<SubmitAsync>d__6&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.RoomComponent.<CreateAsync>d__11>(MiniCore.Demo.MiniBomber.RoomComponent.<CreateAsync>d__11&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.RoomComponent.<JoinAsync>d__12>(MiniCore.Demo.MiniBomber.RoomComponent.<JoinAsync>d__12&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.RoomComponent.<LeaveAsync>d__13>(MiniCore.Demo.MiniBomber.RoomComponent.<LeaveAsync>d__13&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.RoomComponent.<SetReadyAsync>d__15>(MiniCore.Demo.MiniBomber.RoomComponent.<SetReadyAsync>d__15&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.RoomComponent.<StartMatchAsync>d__16>(MiniCore.Demo.MiniBomber.RoomComponent.<StartMatchAsync>d__16&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.RoomComponent.<UpdateSettingsAsync>d__14>(MiniCore.Demo.MiniBomber.RoomComponent.<UpdateSettingsAsync>d__14&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.RoomWindowPresenter.<ApplySettingsAsync>d__11>(MiniCore.Demo.MiniBomber.RoomWindowPresenter.<ApplySettingsAsync>d__11&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.RoomWindowPresenter.<LeaveAsync>d__17>(MiniCore.Demo.MiniBomber.RoomWindowPresenter.<LeaveAsync>d__17&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.RoomWindowPresenter.<StartMatchAsync>d__15>(MiniCore.Demo.MiniBomber.RoomWindowPresenter.<StartMatchAsync>d__15&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.RoomWindowPresenter.<ToggleReadyAsync>d__13>(MiniCore.Demo.MiniBomber.RoomWindowPresenter.<ToggleReadyAsync>d__13&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.HotUpdate.GameStartup.<StartAsync>d__0>(MiniCore.HotUpdate.GameStartup.<StartAsync>d__0&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.HotUpdate.KcpTestWindowPresenter.<ConnectClientAsync>d__18>(MiniCore.HotUpdate.KcpTestWindowPresenter.<ConnectClientAsync>d__18&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.HotUpdate.KcpTestWindowPresenter.<DisconnectClientAsync>d__19>(MiniCore.HotUpdate.KcpTestWindowPresenter.<DisconnectClientAsync>d__19&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.HotUpdate.KcpTestWindowPresenter.<HandleClientDisconnectedAsync>d__21>(MiniCore.HotUpdate.KcpTestWindowPresenter.<HandleClientDisconnectedAsync>d__21&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.HotUpdate.KcpTestWindowPresenter.<SendNormalAsync>d__24>(MiniCore.HotUpdate.KcpTestWindowPresenter.<SendNormalAsync>d__24&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.HotUpdate.KcpTestWindowPresenter.<SendRpcAsync>d__25>(MiniCore.HotUpdate.KcpTestWindowPresenter.<SendRpcAsync>d__25&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.HotUpdate.KcpTestWindowPresenter.<StartServerAsync>d__16>(MiniCore.HotUpdate.KcpTestWindowPresenter.<StartServerAsync>d__16&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.HotUpdate.KcpTestWindowPresenter.<StopServerAsync>d__20>(MiniCore.HotUpdate.KcpTestWindowPresenter.<StopServerAsync>d__20&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.HotUpdate.MiniCoreStartup.<StartAsync>d__0>(MiniCore.HotUpdate.MiniCoreStartup.<StartAsync>d__0&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.HotUpdate.MiniCoreStartup.<StartConfiguredAsync>d__1>(MiniCore.HotUpdate.MiniCoreStartup.<StartConfiguredAsync>d__1&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectKcpAsync>d__48>(MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectKcpAsync>d__48&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectTcpAsync>d__47>(MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectTcpAsync>d__47&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectUdpAsync>d__49>(MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectUdpAsync>d__49&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.HotUpdate.MultiProtocolTestPanel.<SendNormalAsync>d__51>(MiniCore.HotUpdate.MultiProtocolTestPanel.<SendNormalAsync>d__51&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.HotUpdate.MultiProtocolTestPanel.<SendRpcAsync>d__52>(MiniCore.HotUpdate.MultiProtocolTestPanel.<SendRpcAsync>d__52&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.HotUpdate.MultiProtocolTestPanel.<StartKcpServerAsync>d__42>(MiniCore.HotUpdate.MultiProtocolTestPanel.<StartKcpServerAsync>d__42&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.HotUpdate.MultiProtocolTestPanel.<StartTcpServerAsync>d__41>(MiniCore.HotUpdate.MultiProtocolTestPanel.<StartTcpServerAsync>d__41&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.HotUpdate.MultiProtocolTestPanel.<StartUdpServerAsync>d__43>(MiniCore.HotUpdate.MultiProtocolTestPanel.<StartUdpServerAsync>d__43&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.HotUpdate.TestHandler.<HandleAsync>d__0>(MiniCore.HotUpdate.TestHandler.<HandleAsync>d__0&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.AccountSessionComponent.<ConnectAsync>d__23>(MiniCore.Demo.MiniBomber.AccountSessionComponent.<ConnectAsync>d__23&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.AccountSessionComponent.<ConnectLobbyAsync>d__36>(MiniCore.Demo.MiniBomber.AccountSessionComponent.<ConnectLobbyAsync>d__36&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.AccountSessionComponent.<InitializeAsync>d__22>(MiniCore.Demo.MiniBomber.AccountSessionComponent.<InitializeAsync>d__22&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.AccountSessionComponent.<LoginAsync>d__25>(MiniCore.Demo.MiniBomber.AccountSessionComponent.<LoginAsync>d__25&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.AccountSessionComponent.<LogoutAsync>d__27>(MiniCore.Demo.MiniBomber.AccountSessionComponent.<LogoutAsync>d__27&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.AccountSessionComponent.<RegisterAsync>d__24>(MiniCore.Demo.MiniBomber.AccountSessionComponent.<RegisterAsync>d__24&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.AccountSessionComponent.<ResolveLobbyAsync>d__35>(MiniCore.Demo.MiniBomber.AccountSessionComponent.<ResolveLobbyAsync>d__35&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.AccountSessionComponent.<ResumeAsync>d__26>(MiniCore.Demo.MiniBomber.AccountSessionComponent.<ResumeAsync>d__26&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.BattleClientComponent.<RequestResyncAsync>d__26>(MiniCore.Demo.MiniBomber.BattleClientComponent.<RequestResyncAsync>d__26&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.BattleHudWindowPresenter.<ClearKillFeedAsync>d__12>(MiniCore.Demo.MiniBomber.BattleHudWindowPresenter.<ClearKillFeedAsync>d__12&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.BattleHudWindowPresenter.<RefreshPerformanceLoopAsync>d__13>(MiniCore.Demo.MiniBomber.BattleHudWindowPresenter.<RefreshPerformanceLoopAsync>d__13&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.CreateRoomPopupPresenter.<SubmitAsync>d__8>(MiniCore.Demo.MiniBomber.CreateRoomPopupPresenter.<SubmitAsync>d__8&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.LobbyComponent.<RefreshAsync>d__9>(MiniCore.Demo.MiniBomber.LobbyComponent.<RefreshAsync>d__9&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.LobbyWindowPresenter.<JoinAsync>d__15>(MiniCore.Demo.MiniBomber.LobbyWindowPresenter.<JoinAsync>d__15&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.LobbyWindowPresenter.<LogoutAsync>d__17>(MiniCore.Demo.MiniBomber.LobbyWindowPresenter.<LogoutAsync>d__17&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.LobbyWindowPresenter.<OpenCreateAsync>d__13>(MiniCore.Demo.MiniBomber.LobbyWindowPresenter.<OpenCreateAsync>d__13&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.LobbyWindowPresenter.<RefreshAsync>d__11>(MiniCore.Demo.MiniBomber.LobbyWindowPresenter.<RefreshAsync>d__11&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.LoginWindowPresenter.<LoginAsync>d__7>(MiniCore.Demo.MiniBomber.LoginWindowPresenter.<LoginAsync>d__7&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.LoginWindowPresenter.<OpenRegisterAsync>d__9>(MiniCore.Demo.MiniBomber.LoginWindowPresenter.<OpenRegisterAsync>d__9&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.MiniBomberBattlePresentationComponent.<ToggleNetworkDebugAsync>d__28>(MiniCore.Demo.MiniBomber.MiniBomberBattlePresentationComponent.<ToggleNetworkDebugAsync>d__28&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<CloseBattleHudAsync>d__35>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<CloseBattleHudAsync>d__35&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<CloseLoadingAsync>d__38>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<CloseLoadingAsync>d__38&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<HandleMatchPrepareModelAsync>d__26>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<HandleMatchPrepareModelAsync>d__26&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<HandleNonResumableDisconnectAsync>d__39>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<HandleNonResumableDisconnectAsync>d__39&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<InitializeAsync>d__20>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<InitializeAsync>d__20&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<LoadSceneAsync>d__37>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<LoadSceneAsync>d__37&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<NavigateAsync>d__21>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<NavigateAsync>d__21&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<NavigateWindowIfAvailableAsync>d__42>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<NavigateWindowIfAvailableAsync>d__42&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<OpenBattleHudAsync>d__34>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<OpenBattleHudAsync>d__34&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<OpenReconnectOverlayAsync>d__28>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<OpenReconnectOverlayAsync>d__28&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<OpenResultAsync>d__31>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<OpenResultAsync>d__31&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<OpenWindowIfAvailableAsync>d__41>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<OpenWindowIfAvailableAsync>d__41&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<ReconnectAsync>d__27>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<ReconnectAsync>d__27&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<ReturnToRoomAsync>d__33>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<ReturnToRoomAsync>d__33&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.MiniBomberClientStartupComponent.<InitializeAsync>d__8>(MiniCore.Demo.MiniBomber.MiniBomberClientStartupComponent.<InitializeAsync>d__8&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.MiniBomberStartupComponentBase.<LoadConfigurationAsync>d__16>(MiniCore.Demo.MiniBomber.MiniBomberStartupComponentBase.<LoadConfigurationAsync>d__16&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.NetworkDebugWindowPresenter.<RefreshLoopAsync>d__6>(MiniCore.Demo.MiniBomber.NetworkDebugWindowPresenter.<RefreshLoopAsync>d__6&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.RegisterWindowPresenter.<SubmitAsync>d__6>(MiniCore.Demo.MiniBomber.RegisterWindowPresenter.<SubmitAsync>d__6&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.RoomComponent.<CreateAsync>d__11>(MiniCore.Demo.MiniBomber.RoomComponent.<CreateAsync>d__11&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.RoomComponent.<JoinAsync>d__12>(MiniCore.Demo.MiniBomber.RoomComponent.<JoinAsync>d__12&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.RoomComponent.<LeaveAsync>d__13>(MiniCore.Demo.MiniBomber.RoomComponent.<LeaveAsync>d__13&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.RoomComponent.<SetReadyAsync>d__15>(MiniCore.Demo.MiniBomber.RoomComponent.<SetReadyAsync>d__15&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.RoomComponent.<StartMatchAsync>d__16>(MiniCore.Demo.MiniBomber.RoomComponent.<StartMatchAsync>d__16&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.RoomComponent.<UpdateSettingsAsync>d__14>(MiniCore.Demo.MiniBomber.RoomComponent.<UpdateSettingsAsync>d__14&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.RoomWindowPresenter.<ApplySettingsAsync>d__11>(MiniCore.Demo.MiniBomber.RoomWindowPresenter.<ApplySettingsAsync>d__11&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.RoomWindowPresenter.<LeaveAsync>d__17>(MiniCore.Demo.MiniBomber.RoomWindowPresenter.<LeaveAsync>d__17&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.RoomWindowPresenter.<StartMatchAsync>d__15>(MiniCore.Demo.MiniBomber.RoomWindowPresenter.<StartMatchAsync>d__15&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.RoomWindowPresenter.<ToggleReadyAsync>d__13>(MiniCore.Demo.MiniBomber.RoomWindowPresenter.<ToggleReadyAsync>d__13&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.HotUpdate.GameStartup.<StartAsync>d__0>(MiniCore.HotUpdate.GameStartup.<StartAsync>d__0&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.HotUpdate.KcpTestWindowPresenter.<ConnectClientAsync>d__18>(MiniCore.HotUpdate.KcpTestWindowPresenter.<ConnectClientAsync>d__18&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.HotUpdate.KcpTestWindowPresenter.<DisconnectClientAsync>d__19>(MiniCore.HotUpdate.KcpTestWindowPresenter.<DisconnectClientAsync>d__19&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.HotUpdate.KcpTestWindowPresenter.<HandleClientDisconnectedAsync>d__21>(MiniCore.HotUpdate.KcpTestWindowPresenter.<HandleClientDisconnectedAsync>d__21&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.HotUpdate.KcpTestWindowPresenter.<SendNormalAsync>d__24>(MiniCore.HotUpdate.KcpTestWindowPresenter.<SendNormalAsync>d__24&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.HotUpdate.KcpTestWindowPresenter.<SendRpcAsync>d__25>(MiniCore.HotUpdate.KcpTestWindowPresenter.<SendRpcAsync>d__25&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.HotUpdate.KcpTestWindowPresenter.<StartServerAsync>d__16>(MiniCore.HotUpdate.KcpTestWindowPresenter.<StartServerAsync>d__16&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.HotUpdate.KcpTestWindowPresenter.<StopServerAsync>d__20>(MiniCore.HotUpdate.KcpTestWindowPresenter.<StopServerAsync>d__20&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.HotUpdate.MiniCoreStartup.<StartAsync>d__0>(MiniCore.HotUpdate.MiniCoreStartup.<StartAsync>d__0&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.HotUpdate.MiniCoreStartup.<StartConfiguredAsync>d__1>(MiniCore.HotUpdate.MiniCoreStartup.<StartConfiguredAsync>d__1&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectKcpAsync>d__48>(MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectKcpAsync>d__48&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectTcpAsync>d__47>(MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectTcpAsync>d__47&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectUdpAsync>d__49>(MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectUdpAsync>d__49&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.HotUpdate.MultiProtocolTestPanel.<SendNormalAsync>d__51>(MiniCore.HotUpdate.MultiProtocolTestPanel.<SendNormalAsync>d__51&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.HotUpdate.MultiProtocolTestPanel.<SendRpcAsync>d__52>(MiniCore.HotUpdate.MultiProtocolTestPanel.<SendRpcAsync>d__52&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.HotUpdate.MultiProtocolTestPanel.<StartKcpServerAsync>d__42>(MiniCore.HotUpdate.MultiProtocolTestPanel.<StartKcpServerAsync>d__42&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.HotUpdate.MultiProtocolTestPanel.<StartTcpServerAsync>d__41>(MiniCore.HotUpdate.MultiProtocolTestPanel.<StartTcpServerAsync>d__41&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.HotUpdate.MultiProtocolTestPanel.<StartUdpServerAsync>d__43>(MiniCore.HotUpdate.MultiProtocolTestPanel.<StartUdpServerAsync>d__43&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.HotUpdate.TestHandler.<HandleAsync>d__0>(MiniCore.HotUpdate.TestHandler.<HandleAsync>d__0&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Serialization.ProtobufSaveServiceExtensions.<LoadProtobufAsync>d__1<object>>(MiniCore.Serialization.ProtobufSaveServiceExtensions.<LoadProtobufAsync>d__1<object>&)
		// object Newtonsoft.Json.JsonConvert.DeserializeObject<object>(string)
		// object Newtonsoft.Json.JsonConvert.DeserializeObject<object>(string,Newtonsoft.Json.JsonSerializerSettings)
		// object System.Activator.CreateInstance<object>()
		// UnityEngine.Vector2Int[] System.Array.Empty<UnityEngine.Vector2Int>()
		// byte[] System.Array.Empty<byte>()
		// object[] System.Array.Empty<object>()
		// int System.Array.IndexOf<int>(int[],int)
		// int System.Array.IndexOfImpl<int>(int[],int,int,int)
		// object& System.Runtime.CompilerServices.Unsafe.As<object,object>(object&)
		// System.Void* System.Runtime.CompilerServices.Unsafe.AsPointer<object>(object&)
		// System.Void* Unity.Collections.LowLevel.Unsafe.UnsafeUtility.AddressOf<UnityEngine.Vector2>(UnityEngine.Vector2&)
		// int Unity.Collections.LowLevel.Unsafe.UnsafeUtility.SizeOf<UnityEngine.Vector2>()
		// object UnityEngine.Component.GetComponentInChildren<object>()
		// object UnityEngine.GameObject.GetComponent<object>()
		// UnityEngine.Vector2 UnityEngine.InputSystem.InputAction.ReadValue<UnityEngine.Vector2>()
		// UnityEngine.Vector2 UnityEngine.InputSystem.InputActionState.ReadValue<UnityEngine.Vector2>(int,int,bool)
		// object UnityEngine.Object.FindObjectOfType<object>()
		// object UnityEngine.Object.Instantiate<object>(object,UnityEngine.Transform)
		// object UnityEngine.Object.Instantiate<object>(object,UnityEngine.Transform,bool)
	}
}