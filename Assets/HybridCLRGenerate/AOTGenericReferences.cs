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
		"YooAsset.dll",
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
	// MiniCore.Model.ARpcHandler<object,object>
	// MiniCore.Model.MonoSingleton<object>
	// MiniCore.Threading.IMTaskSource<MiniCore.Demo.MiniBomber.MiniBomberRegisterResult>
	// MiniCore.Threading.IMTaskSource<byte>
	// MiniCore.Threading.IMTaskSource<object>
	// MiniCore.Threading.MSharedTask<MiniCore.Demo.MiniBomber.MiniBomberRegisterResult>
	// MiniCore.Threading.MSharedTask<byte>
	// MiniCore.Threading.MSharedTask<object>
	// MiniCore.Threading.MSharedTaskAwaiter<MiniCore.Demo.MiniBomber.MiniBomberRegisterResult>
	// MiniCore.Threading.MSharedTaskAwaiter<byte>
	// MiniCore.Threading.MSharedTaskAwaiter<object>
	// MiniCore.Threading.MSharedTaskWaiter<MiniCore.Demo.MiniBomber.MiniBomberRegisterResult>
	// MiniCore.Threading.MSharedTaskWaiter<byte>
	// MiniCore.Threading.MSharedTaskWaiter<object>
	// MiniCore.Threading.MTask<MiniCore.Demo.MiniBomber.MiniBomberRegisterResult>
	// MiniCore.Threading.MTask<byte>
	// MiniCore.Threading.MTask<object>
	// MiniCore.Threading.MTaskAwaiter<MiniCore.Demo.MiniBomber.MiniBomberRegisterResult>
	// MiniCore.Threading.MTaskAwaiter<byte>
	// MiniCore.Threading.MTaskAwaiter<object>
	// MiniCore.Threading.MTaskCompletionSource<byte>
	// MiniCore.Threading.MTaskCompletionSource<object>
	// MiniCore.Threading.MTaskForgetObserver<MiniCore.Demo.MiniBomber.MiniBomberRegisterResult>
	// MiniCore.Threading.MTaskForgetObserver<byte>
	// MiniCore.Threading.MTaskForgetObserver<object>
	// MiniCore.Threading.MTaskMethodBuilder<MiniCore.Demo.MiniBomber.MiniBomberRegisterResult>
	// MiniCore.Threading.MTaskMethodBuilder<byte>
	// MiniCore.Threading.MTaskMethodBuilder<object>
	// MiniCore.Threading.MTaskPromise<MiniCore.Demo.MiniBomber.MiniBomberRegisterResult>
	// MiniCore.Threading.MTaskPromise<byte>
	// MiniCore.Threading.MTaskPromise<object>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Core.ExcelTool.<LoadCsvFileAsync>d__1<object>>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Core.GameObjectPool.<CreateAsync>d__16>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Core.GameObjectPool.<PrewarmAsync>d__12>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Core.GameObjectPool.<RentAsync>d__10<object>>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Core.GameObjectPoolMgr.<GeneratePoolObject>d__5<object>>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.AccountSessionComponent.<ConnectAsync>d__21>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.AccountSessionComponent.<InitializeAsync>d__20>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.AccountSessionComponent.<LoginAsync>d__23>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.AccountSessionComponent.<LogoutAsync>d__25>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.AccountSessionComponent.<ResumeAsync>d__24>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.BattleClientComponent.<RequestResyncAsync>d__35>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.BattleHudWindowPresenter.<ClearKillFeedAsync>d__10>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.BattleHudWindowPresenter.<RefreshPerformanceLoopAsync>d__11>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.CreateRoomPopupPresenter.<SubmitAsync>d__8>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.LobbyComponent.<RefreshAsync>d__17>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.LobbyWindowPresenter.<JoinAsync>d__15>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.LobbyWindowPresenter.<LogoutAsync>d__17>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.LobbyWindowPresenter.<OpenCreateAsync>d__13>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.LobbyWindowPresenter.<RefreshAsync>d__11>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.LoginWindowPresenter.<LoginAsync>d__7>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.LoginWindowPresenter.<OpenRegisterAsync>d__9>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.MiniBomberAccountRepositoryComponent.<InitializeAsync>d__0>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.MiniBomberAccountRepositoryComponent.<RegisterAsync>d__1>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.MiniBomberBattlePresentationComponent.<ToggleNetworkDebugAsync>d__28>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<CloseBattleHudAsync>d__48>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<CloseLoadingAsync>d__51>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<HandleMatchPrepareAsync>d__36>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<HandleNonResumableDisconnectAsync>d__52>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<InitializeAsync>d__34>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<LoadSceneAsync>d__50>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<NavigateAsync>d__35>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<NavigateWindowIfAvailableAsync>d__54>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<OpenBattleHudAsync>d__47>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<OpenReconnectOverlayAsync>d__41>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<OpenResultAsync>d__44>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<OpenWindowIfAvailableAsync>d__53>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<ReconnectAsync>d__40>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<ReturnToRoomAsync>d__46>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.MiniBomberClientStartupComponent.<InitializeAsync>d__7>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.MiniBomberRegisterHandler.<HandleAsync>d__0>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.MiniBomberServerRuntimeComponent.<InitializeAsync>d__34>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.MiniBomberServerRuntimeComponent.<RegisterAsync>d__35>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.MiniBomberServerStartupComponent.<InitializeAsync>d__0>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.MiniBomberStartupComponentBase.<LoadConfigurationAsync>d__16>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.NetworkDebugWindowPresenter.<RefreshLoopAsync>d__6>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.RegisterWindowPresenter.<SubmitAsync>d__6>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.RoomComponent.<CreateAsync>d__12>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.RoomComponent.<JoinAsync>d__13>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.RoomComponent.<LeaveAsync>d__14>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.RoomComponent.<SetReadyAsync>d__16>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.RoomComponent.<UpdateSettingsAsync>d__15>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.RoomWindowPresenter.<ApplySettingsAsync>d__11>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.RoomWindowPresenter.<LeaveAsync>d__17>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.RoomWindowPresenter.<StartMatchAsync>d__15>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.RoomWindowPresenter.<ToggleReadyAsync>d__13>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.HotUpdate.DedicatedClientSmokeTestRunner.<RunInternalAsync>d__28>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.HotUpdate.GameStartup.<StartAsync>d__0>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.HotUpdate.KcpTestWindowPresenter.<ConnectClientAsync>d__19>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.HotUpdate.KcpTestWindowPresenter.<DisconnectClientAsync>d__20>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.HotUpdate.KcpTestWindowPresenter.<HandleClientDisconnectedAsync>d__22>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.HotUpdate.KcpTestWindowPresenter.<SendNormalAsync>d__25>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.HotUpdate.KcpTestWindowPresenter.<SendRpcAsync>d__26>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.HotUpdate.KcpTestWindowPresenter.<StartServerAsync>d__17>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.HotUpdate.KcpTestWindowPresenter.<StopServerAsync>d__21>
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
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.HotUpdate.NetworkBenchmarkRunner.<RunInternalAsync>d__101>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.HotUpdate.NetworkBenchmarkRunner.<RunMainThreadHitchBenchmarkAsync>d__108>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.HotUpdate.NetworkBenchmarkRunner.<RunNormalBenchmarkAsync>d__106>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.HotUpdate.NetworkBenchmarkRunner.<RunRpcBenchmarkAsync>d__107>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.HotUpdate.NetworkBenchmarkRunner.<RunRpcCallAsync>d__110>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.HotUpdate.NetworkBenchmarkRunner.<RunTransportAsync>d__105>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.HotUpdate.NetworkBenchmarkRunner.<SendNormalMessagesForDurationAsync>d__109>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.HotUpdate.NetworkBenchmarkRunner.<WaitForConditionAsync>d__126>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.HotUpdate.NetworkBenchmarkRunner.<WaitForNormalMessagesAsync>d__123>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.HotUpdate.NetworkBenchmarkRunner.<WaitForQueueToDrainAsync>d__125>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.HotUpdate.NetworkBenchmarkRunner.<WaitForWarmupMessagesAsync>d__124>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.HotUpdate.NetworkSmokeTestRunner.<RunInternalAsync>d__43>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.HotUpdate.NetworkSmokeTestRunner.<RunTransportAsync>d__47>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.HotUpdate.NetworkSmokeTestRunner.<WaitForConditionAsync>d__48>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.HotUpdate.TestHandler.<HandleAsync>d__0>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Service.AssetService.<PreloadAssetAsync>d__6<object>>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Service.UIService.<AcquireResourceLeaseAsync>d__44>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Service.UIService.<CloseNavigationAsync>d__25>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Service.UIService.<CreateViewAsync>d__43>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Service.UIService.<HandleDuplicateAsync>d__40>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Service.UIService.<InitializeAsync>d__20>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Service.UIService.<NavigateAsync>d__23<object>>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Service.UIService.<NavigateAsync>d__24>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Service.UIService.<OpenCoreAsync>d__39>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Service.UIService.<PrefetchAsync>d__26<object>>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Service.UIService.<ShowAsync>d__29<object,object,object>>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Service.YooAssetResourceService.<InstantiateAsync>d__2>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Service.YooAssetResourceService.<LoadAssetAsync>d__3<object>>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Service.YooAssetResourceService.<PreloadAssetAsync>d__4<object>>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Service.YooAssetSceneService.<LoadSingleAsync>d__8>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.UI.UIWindowSession.<CloseInternalAsync>d__41>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.UI.UIWindowSession.<OpenInternalAsync>d__36>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.UI.UIWindowSession.<WaitUntilActiveAsync>d__30>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.UI.UIWindowSession.<WaitUntilClosedAsync>d__31>
	// MiniCore.UI.AUIWindowPresenter<object>
	// MiniCore.UI.IUIWindowArgs<object>
	// System.Action<MiniCore.Demo.MiniBomber.MiniBomberBattleParticipant>
	// System.Action<MiniCore.Demo.MiniBomber.MiniBomberSimulationEvent>
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
	// System.Collections.Generic.ArraySortHelper<MiniCore.Demo.MiniBomber.MiniBomberBattleParticipant>
	// System.Collections.Generic.ArraySortHelper<MiniCore.Demo.MiniBomber.MiniBomberSimulationEvent>
	// System.Collections.Generic.ArraySortHelper<long>
	// System.Collections.Generic.ArraySortHelper<object>
	// System.Collections.Generic.Comparer<MiniCore.Demo.MiniBomber.MiniBomberBattleParticipant>
	// System.Collections.Generic.Comparer<MiniCore.Demo.MiniBomber.MiniBomberSimulationEvent>
	// System.Collections.Generic.Comparer<long>
	// System.Collections.Generic.Comparer<object>
	// System.Collections.Generic.Dictionary.Enumerator<MiniCore.Core.GameObjectPoolKey,object>
	// System.Collections.Generic.Dictionary.Enumerator<MiniCore.Service.UIService.UIWindowLogicalKey,object>
	// System.Collections.Generic.Dictionary.Enumerator<MiniCore.UI.UIWindowId,object>
	// System.Collections.Generic.Dictionary.Enumerator<MiniCore.UI.UIWindowId,uint>
	// System.Collections.Generic.Dictionary.Enumerator<MiniCore.UI.UIWindowInstanceId,object>
	// System.Collections.Generic.Dictionary.Enumerator<int,object>
	// System.Collections.Generic.Dictionary.Enumerator<long,int>
	// System.Collections.Generic.Dictionary.Enumerator<long,object>
	// System.Collections.Generic.Dictionary.Enumerator<object,int>
	// System.Collections.Generic.Dictionary.Enumerator<object,object>
	// System.Collections.Generic.Dictionary.Enumerator<uint,object>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<MiniCore.Core.GameObjectPoolKey,object>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<MiniCore.Service.UIService.UIWindowLogicalKey,object>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<MiniCore.UI.UIWindowId,object>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<MiniCore.UI.UIWindowId,uint>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<MiniCore.UI.UIWindowInstanceId,object>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<int,object>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<long,int>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<long,object>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<object,int>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<object,object>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<uint,object>
	// System.Collections.Generic.Dictionary.KeyCollection<MiniCore.Core.GameObjectPoolKey,object>
	// System.Collections.Generic.Dictionary.KeyCollection<MiniCore.Service.UIService.UIWindowLogicalKey,object>
	// System.Collections.Generic.Dictionary.KeyCollection<MiniCore.UI.UIWindowId,object>
	// System.Collections.Generic.Dictionary.KeyCollection<MiniCore.UI.UIWindowId,uint>
	// System.Collections.Generic.Dictionary.KeyCollection<MiniCore.UI.UIWindowInstanceId,object>
	// System.Collections.Generic.Dictionary.KeyCollection<int,object>
	// System.Collections.Generic.Dictionary.KeyCollection<long,int>
	// System.Collections.Generic.Dictionary.KeyCollection<long,object>
	// System.Collections.Generic.Dictionary.KeyCollection<object,int>
	// System.Collections.Generic.Dictionary.KeyCollection<object,object>
	// System.Collections.Generic.Dictionary.KeyCollection<uint,object>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<MiniCore.Core.GameObjectPoolKey,object>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<MiniCore.Service.UIService.UIWindowLogicalKey,object>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<MiniCore.UI.UIWindowId,object>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<MiniCore.UI.UIWindowId,uint>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<MiniCore.UI.UIWindowInstanceId,object>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<int,object>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<long,int>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<long,object>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<object,int>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<object,object>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<uint,object>
	// System.Collections.Generic.Dictionary.ValueCollection<MiniCore.Core.GameObjectPoolKey,object>
	// System.Collections.Generic.Dictionary.ValueCollection<MiniCore.Service.UIService.UIWindowLogicalKey,object>
	// System.Collections.Generic.Dictionary.ValueCollection<MiniCore.UI.UIWindowId,object>
	// System.Collections.Generic.Dictionary.ValueCollection<MiniCore.UI.UIWindowId,uint>
	// System.Collections.Generic.Dictionary.ValueCollection<MiniCore.UI.UIWindowInstanceId,object>
	// System.Collections.Generic.Dictionary.ValueCollection<int,object>
	// System.Collections.Generic.Dictionary.ValueCollection<long,int>
	// System.Collections.Generic.Dictionary.ValueCollection<long,object>
	// System.Collections.Generic.Dictionary.ValueCollection<object,int>
	// System.Collections.Generic.Dictionary.ValueCollection<object,object>
	// System.Collections.Generic.Dictionary.ValueCollection<uint,object>
	// System.Collections.Generic.Dictionary<MiniCore.Core.GameObjectPoolKey,object>
	// System.Collections.Generic.Dictionary<MiniCore.Service.UIService.UIWindowLogicalKey,object>
	// System.Collections.Generic.Dictionary<MiniCore.UI.UIWindowId,object>
	// System.Collections.Generic.Dictionary<MiniCore.UI.UIWindowId,uint>
	// System.Collections.Generic.Dictionary<MiniCore.UI.UIWindowInstanceId,object>
	// System.Collections.Generic.Dictionary<int,object>
	// System.Collections.Generic.Dictionary<long,int>
	// System.Collections.Generic.Dictionary<long,object>
	// System.Collections.Generic.Dictionary<object,int>
	// System.Collections.Generic.Dictionary<object,object>
	// System.Collections.Generic.Dictionary<uint,object>
	// System.Collections.Generic.EqualityComparer<MiniCore.Core.GameObjectPoolKey>
	// System.Collections.Generic.EqualityComparer<MiniCore.Service.UIService.UIWindowLogicalKey>
	// System.Collections.Generic.EqualityComparer<MiniCore.UI.UIWindowId>
	// System.Collections.Generic.EqualityComparer<MiniCore.UI.UIWindowInstanceId>
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
	// System.Collections.Generic.ICollection<MiniCore.Demo.MiniBomber.MiniBomberBattleParticipant>
	// System.Collections.Generic.ICollection<MiniCore.Demo.MiniBomber.MiniBomberSimulationEvent>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<MiniCore.Core.GameObjectPoolKey,object>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<MiniCore.Service.UIService.UIWindowLogicalKey,object>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<MiniCore.UI.UIWindowId,object>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<MiniCore.UI.UIWindowId,uint>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<MiniCore.UI.UIWindowInstanceId,object>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<int,object>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<long,int>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<long,object>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<object,int>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<uint,object>>
	// System.Collections.Generic.ICollection<long>
	// System.Collections.Generic.ICollection<object>
	// System.Collections.Generic.IComparer<MiniCore.Demo.MiniBomber.MiniBomberBattleParticipant>
	// System.Collections.Generic.IComparer<MiniCore.Demo.MiniBomber.MiniBomberSimulationEvent>
	// System.Collections.Generic.IComparer<long>
	// System.Collections.Generic.IComparer<object>
	// System.Collections.Generic.IEnumerable<MiniCore.Demo.MiniBomber.MiniBomberBattleParticipant>
	// System.Collections.Generic.IEnumerable<MiniCore.Demo.MiniBomber.MiniBomberSimulationEvent>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<MiniCore.Core.GameObjectPoolKey,object>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<MiniCore.Service.UIService.UIWindowLogicalKey,object>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<MiniCore.UI.UIWindowId,object>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<MiniCore.UI.UIWindowId,uint>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<MiniCore.UI.UIWindowInstanceId,object>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<int,object>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<long,int>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<long,object>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,int>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<uint,object>>
	// System.Collections.Generic.IEnumerable<long>
	// System.Collections.Generic.IEnumerable<object>
	// System.Collections.Generic.IEnumerator<MiniCore.Demo.MiniBomber.MiniBomberBattleParticipant>
	// System.Collections.Generic.IEnumerator<MiniCore.Demo.MiniBomber.MiniBomberSimulationEvent>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<MiniCore.Core.GameObjectPoolKey,object>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<MiniCore.Service.UIService.UIWindowLogicalKey,object>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<MiniCore.UI.UIWindowId,object>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<MiniCore.UI.UIWindowId,uint>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<MiniCore.UI.UIWindowInstanceId,object>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<int,object>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<long,int>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<long,object>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<object,int>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<uint,object>>
	// System.Collections.Generic.IEnumerator<long>
	// System.Collections.Generic.IEnumerator<object>
	// System.Collections.Generic.IEqualityComparer<MiniCore.Core.GameObjectPoolKey>
	// System.Collections.Generic.IEqualityComparer<MiniCore.Service.UIService.UIWindowLogicalKey>
	// System.Collections.Generic.IEqualityComparer<MiniCore.UI.UIWindowId>
	// System.Collections.Generic.IEqualityComparer<MiniCore.UI.UIWindowInstanceId>
	// System.Collections.Generic.IEqualityComparer<int>
	// System.Collections.Generic.IEqualityComparer<long>
	// System.Collections.Generic.IEqualityComparer<object>
	// System.Collections.Generic.IEqualityComparer<uint>
	// System.Collections.Generic.IList<MiniCore.Demo.MiniBomber.MiniBomberBattleParticipant>
	// System.Collections.Generic.IList<MiniCore.Demo.MiniBomber.MiniBomberSimulationEvent>
	// System.Collections.Generic.IList<long>
	// System.Collections.Generic.IList<object>
	// System.Collections.Generic.IReadOnlyCollection<MiniCore.Demo.MiniBomber.MiniBomberBattleParticipant>
	// System.Collections.Generic.IReadOnlyCollection<MiniCore.Demo.MiniBomber.MiniBomberMatchResult>
	// System.Collections.Generic.IReadOnlyCollection<MiniCore.Demo.MiniBomber.MiniBomberSimulationEvent>
	// System.Collections.Generic.IReadOnlyCollection<object>
	// System.Collections.Generic.IReadOnlyList<MiniCore.Demo.MiniBomber.MiniBomberBattleParticipant>
	// System.Collections.Generic.IReadOnlyList<MiniCore.Demo.MiniBomber.MiniBomberMatchResult>
	// System.Collections.Generic.IReadOnlyList<MiniCore.Demo.MiniBomber.MiniBomberSimulationEvent>
	// System.Collections.Generic.IReadOnlyList<object>
	// System.Collections.Generic.KeyValuePair<MiniCore.Core.GameObjectPoolKey,object>
	// System.Collections.Generic.KeyValuePair<MiniCore.Service.UIService.UIWindowLogicalKey,object>
	// System.Collections.Generic.KeyValuePair<MiniCore.UI.UIWindowId,object>
	// System.Collections.Generic.KeyValuePair<MiniCore.UI.UIWindowId,uint>
	// System.Collections.Generic.KeyValuePair<MiniCore.UI.UIWindowInstanceId,object>
	// System.Collections.Generic.KeyValuePair<int,object>
	// System.Collections.Generic.KeyValuePair<long,int>
	// System.Collections.Generic.KeyValuePair<long,object>
	// System.Collections.Generic.KeyValuePair<object,int>
	// System.Collections.Generic.KeyValuePair<object,object>
	// System.Collections.Generic.KeyValuePair<uint,object>
	// System.Collections.Generic.List.Enumerator<MiniCore.Demo.MiniBomber.MiniBomberBattleParticipant>
	// System.Collections.Generic.List.Enumerator<MiniCore.Demo.MiniBomber.MiniBomberSimulationEvent>
	// System.Collections.Generic.List.Enumerator<long>
	// System.Collections.Generic.List.Enumerator<object>
	// System.Collections.Generic.List<MiniCore.Demo.MiniBomber.MiniBomberBattleParticipant>
	// System.Collections.Generic.List<MiniCore.Demo.MiniBomber.MiniBomberSimulationEvent>
	// System.Collections.Generic.List<long>
	// System.Collections.Generic.List<object>
	// System.Collections.Generic.ObjectComparer<MiniCore.Demo.MiniBomber.MiniBomberBattleParticipant>
	// System.Collections.Generic.ObjectComparer<MiniCore.Demo.MiniBomber.MiniBomberSimulationEvent>
	// System.Collections.Generic.ObjectComparer<long>
	// System.Collections.Generic.ObjectComparer<object>
	// System.Collections.Generic.ObjectEqualityComparer<MiniCore.Core.GameObjectPoolKey>
	// System.Collections.Generic.ObjectEqualityComparer<MiniCore.Service.UIService.UIWindowLogicalKey>
	// System.Collections.Generic.ObjectEqualityComparer<MiniCore.UI.UIWindowId>
	// System.Collections.Generic.ObjectEqualityComparer<MiniCore.UI.UIWindowInstanceId>
	// System.Collections.Generic.ObjectEqualityComparer<int>
	// System.Collections.Generic.ObjectEqualityComparer<long>
	// System.Collections.Generic.ObjectEqualityComparer<object>
	// System.Collections.Generic.ObjectEqualityComparer<uint>
	// System.Collections.Generic.Stack.Enumerator<object>
	// System.Collections.Generic.Stack<object>
	// System.Collections.ObjectModel.ReadOnlyCollection<MiniCore.Demo.MiniBomber.MiniBomberBattleParticipant>
	// System.Collections.ObjectModel.ReadOnlyCollection<MiniCore.Demo.MiniBomber.MiniBomberSimulationEvent>
	// System.Collections.ObjectModel.ReadOnlyCollection<long>
	// System.Collections.ObjectModel.ReadOnlyCollection<object>
	// System.Comparison<MiniCore.Demo.MiniBomber.MiniBomberBattleParticipant>
	// System.Comparison<MiniCore.Demo.MiniBomber.MiniBomberSimulationEvent>
	// System.Comparison<long>
	// System.Comparison<object>
	// System.Func<MiniCore.Threading.MTask<byte>>
	// System.Func<MiniCore.Threading.MTask>
	// System.Func<byte>
	// System.Func<object,int>
	// System.Func<object,object,object>
	// System.Func<object,object>
	// System.Func<object>
	// System.IEquatable<MiniCore.Core.GameObjectPoolKey>
	// System.IEquatable<MiniCore.Service.UIService.UIWindowLogicalKey>
	// System.IEquatable<object>
	// System.Memory<byte>
	// System.Predicate<MiniCore.Demo.MiniBomber.MiniBomberBattleParticipant>
	// System.Predicate<MiniCore.Demo.MiniBomber.MiniBomberRoomWorkerCommand>
	// System.Predicate<MiniCore.Demo.MiniBomber.MiniBomberSimulationEvent>
	// System.Predicate<long>
	// System.Predicate<object>
	// System.ReadOnlyMemory<byte>
	// System.ReadOnlySpan<byte>
	// System.Span.Enumerator<byte>
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
		// object MiniCore.Model.AComponent.AddComponent<object>()
		// System.Void MiniCore.Model.NetworkProtocolBuilder.RegisterMessage<object>(uint,MiniCore.Model.NetworkMessageRole,MiniCore.Serialization.IMessageParser)
		// MiniCore.Threading.MTask MiniCore.Serialization.ProtobufSaveServiceExtensions.SaveProtobufAsync<object>(MiniCore.Service.ISaveService,string,object)
		// MiniCore.Threading.MTask<object> MiniCore.Service.INetworkService.CallAsync<object,object>(string,object)
		// MiniCore.Threading.MTask MiniCore.Service.INetworkService.SendAsync<object>(string,object)
		// MiniCore.Model.NetworkSendResult MiniCore.Service.INetworkService.TrySend<object>(string,object)
		// MiniCore.Threading.MTask<object> MiniCore.Threading.MTask.FromResult<object>(object)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MSharedTaskAwaiter<byte>,MiniCore.UI.UIWindowSession.<WaitUntilClosedAsync>d__31>(MiniCore.Threading.MSharedTaskAwaiter<byte>&,MiniCore.UI.UIWindowSession.<WaitUntilClosedAsync>d__31&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MSharedTaskAwaiter<object>,MiniCore.Service.UIService.<AcquireResourceLeaseAsync>d__44>(MiniCore.Threading.MSharedTaskAwaiter<object>&,MiniCore.Service.UIService.<AcquireResourceLeaseAsync>d__44&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.Demo.MiniBomber.AccountSessionComponent.<LogoutAsync>d__25>(MiniCore.Threading.MTaskAwaiter&,MiniCore.Demo.MiniBomber.AccountSessionComponent.<LogoutAsync>d__25&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.Demo.MiniBomber.BattleHudWindowPresenter.<ClearKillFeedAsync>d__10>(MiniCore.Threading.MTaskAwaiter&,MiniCore.Demo.MiniBomber.BattleHudWindowPresenter.<ClearKillFeedAsync>d__10&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.Demo.MiniBomber.BattleHudWindowPresenter.<RefreshPerformanceLoopAsync>d__11>(MiniCore.Threading.MTaskAwaiter&,MiniCore.Demo.MiniBomber.BattleHudWindowPresenter.<RefreshPerformanceLoopAsync>d__11&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.Demo.MiniBomber.CreateRoomPopupPresenter.<SubmitAsync>d__8>(MiniCore.Threading.MTaskAwaiter&,MiniCore.Demo.MiniBomber.CreateRoomPopupPresenter.<SubmitAsync>d__8&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.Demo.MiniBomber.LobbyWindowPresenter.<JoinAsync>d__15>(MiniCore.Threading.MTaskAwaiter&,MiniCore.Demo.MiniBomber.LobbyWindowPresenter.<JoinAsync>d__15&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.Demo.MiniBomber.LobbyWindowPresenter.<LogoutAsync>d__17>(MiniCore.Threading.MTaskAwaiter&,MiniCore.Demo.MiniBomber.LobbyWindowPresenter.<LogoutAsync>d__17&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.Demo.MiniBomber.LoginWindowPresenter.<LoginAsync>d__7>(MiniCore.Threading.MTaskAwaiter&,MiniCore.Demo.MiniBomber.LoginWindowPresenter.<LoginAsync>d__7&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.Demo.MiniBomber.MiniBomberBattlePresentationComponent.<ToggleNetworkDebugAsync>d__28>(MiniCore.Threading.MTaskAwaiter&,MiniCore.Demo.MiniBomber.MiniBomberBattlePresentationComponent.<ToggleNetworkDebugAsync>d__28&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<CloseBattleHudAsync>d__48>(MiniCore.Threading.MTaskAwaiter&,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<CloseBattleHudAsync>d__48&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<CloseLoadingAsync>d__51>(MiniCore.Threading.MTaskAwaiter&,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<CloseLoadingAsync>d__51&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<HandleMatchPrepareAsync>d__36>(MiniCore.Threading.MTaskAwaiter&,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<HandleMatchPrepareAsync>d__36&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<HandleNonResumableDisconnectAsync>d__52>(MiniCore.Threading.MTaskAwaiter&,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<HandleNonResumableDisconnectAsync>d__52&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<InitializeAsync>d__34>(MiniCore.Threading.MTaskAwaiter&,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<InitializeAsync>d__34&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<LoadSceneAsync>d__50>(MiniCore.Threading.MTaskAwaiter&,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<LoadSceneAsync>d__50&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<NavigateAsync>d__35>(MiniCore.Threading.MTaskAwaiter&,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<NavigateAsync>d__35&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<NavigateWindowIfAvailableAsync>d__54>(MiniCore.Threading.MTaskAwaiter&,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<NavigateWindowIfAvailableAsync>d__54&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<ReconnectAsync>d__40>(MiniCore.Threading.MTaskAwaiter&,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<ReconnectAsync>d__40&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<ReturnToRoomAsync>d__46>(MiniCore.Threading.MTaskAwaiter&,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<ReturnToRoomAsync>d__46&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.Demo.MiniBomber.MiniBomberClientStartupComponent.<InitializeAsync>d__7>(MiniCore.Threading.MTaskAwaiter&,MiniCore.Demo.MiniBomber.MiniBomberClientStartupComponent.<InitializeAsync>d__7&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.Demo.MiniBomber.MiniBomberRegisterHandler.<HandleAsync>d__0>(MiniCore.Threading.MTaskAwaiter&,MiniCore.Demo.MiniBomber.MiniBomberRegisterHandler.<HandleAsync>d__0&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.Demo.MiniBomber.MiniBomberServerRuntimeComponent.<InitializeAsync>d__34>(MiniCore.Threading.MTaskAwaiter&,MiniCore.Demo.MiniBomber.MiniBomberServerRuntimeComponent.<InitializeAsync>d__34&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.Demo.MiniBomber.MiniBomberServerStartupComponent.<InitializeAsync>d__0>(MiniCore.Threading.MTaskAwaiter&,MiniCore.Demo.MiniBomber.MiniBomberServerStartupComponent.<InitializeAsync>d__0&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.Demo.MiniBomber.NetworkDebugWindowPresenter.<RefreshLoopAsync>d__6>(MiniCore.Threading.MTaskAwaiter&,MiniCore.Demo.MiniBomber.NetworkDebugWindowPresenter.<RefreshLoopAsync>d__6&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.Demo.MiniBomber.RoomWindowPresenter.<LeaveAsync>d__17>(MiniCore.Threading.MTaskAwaiter&,MiniCore.Demo.MiniBomber.RoomWindowPresenter.<LeaveAsync>d__17&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.HotUpdate.DedicatedClientSmokeTestRunner.<RunInternalAsync>d__28>(MiniCore.Threading.MTaskAwaiter&,MiniCore.HotUpdate.DedicatedClientSmokeTestRunner.<RunInternalAsync>d__28&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.HotUpdate.GameStartup.<StartAsync>d__0>(MiniCore.Threading.MTaskAwaiter&,MiniCore.HotUpdate.GameStartup.<StartAsync>d__0&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.HotUpdate.KcpTestWindowPresenter.<DisconnectClientAsync>d__20>(MiniCore.Threading.MTaskAwaiter&,MiniCore.HotUpdate.KcpTestWindowPresenter.<DisconnectClientAsync>d__20&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.HotUpdate.KcpTestWindowPresenter.<SendNormalAsync>d__25>(MiniCore.Threading.MTaskAwaiter&,MiniCore.HotUpdate.KcpTestWindowPresenter.<SendNormalAsync>d__25&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.HotUpdate.KcpTestWindowPresenter.<StartServerAsync>d__17>(MiniCore.Threading.MTaskAwaiter&,MiniCore.HotUpdate.KcpTestWindowPresenter.<StartServerAsync>d__17&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.HotUpdate.KcpTestWindowPresenter.<StopServerAsync>d__21>(MiniCore.Threading.MTaskAwaiter&,MiniCore.HotUpdate.KcpTestWindowPresenter.<StopServerAsync>d__21&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.HotUpdate.MiniCoreStartup.<StartAsync>d__0>(MiniCore.Threading.MTaskAwaiter&,MiniCore.HotUpdate.MiniCoreStartup.<StartAsync>d__0&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.HotUpdate.MiniCoreStartup.<StartConfiguredAsync>d__1>(MiniCore.Threading.MTaskAwaiter&,MiniCore.HotUpdate.MiniCoreStartup.<StartConfiguredAsync>d__1&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.HotUpdate.MultiProtocolTestPanel.<SendNormalAsync>d__51>(MiniCore.Threading.MTaskAwaiter&,MiniCore.HotUpdate.MultiProtocolTestPanel.<SendNormalAsync>d__51&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.HotUpdate.MultiProtocolTestPanel.<StartKcpServerAsync>d__42>(MiniCore.Threading.MTaskAwaiter&,MiniCore.HotUpdate.MultiProtocolTestPanel.<StartKcpServerAsync>d__42&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.HotUpdate.MultiProtocolTestPanel.<StartTcpServerAsync>d__41>(MiniCore.Threading.MTaskAwaiter&,MiniCore.HotUpdate.MultiProtocolTestPanel.<StartTcpServerAsync>d__41&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.HotUpdate.MultiProtocolTestPanel.<StartUdpServerAsync>d__43>(MiniCore.Threading.MTaskAwaiter&,MiniCore.HotUpdate.MultiProtocolTestPanel.<StartUdpServerAsync>d__43&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.HotUpdate.NetworkBenchmarkRunner.<RunInternalAsync>d__101>(MiniCore.Threading.MTaskAwaiter&,MiniCore.HotUpdate.NetworkBenchmarkRunner.<RunInternalAsync>d__101&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.HotUpdate.NetworkBenchmarkRunner.<RunMainThreadHitchBenchmarkAsync>d__108>(MiniCore.Threading.MTaskAwaiter&,MiniCore.HotUpdate.NetworkBenchmarkRunner.<RunMainThreadHitchBenchmarkAsync>d__108&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.HotUpdate.NetworkBenchmarkRunner.<RunNormalBenchmarkAsync>d__106>(MiniCore.Threading.MTaskAwaiter&,MiniCore.HotUpdate.NetworkBenchmarkRunner.<RunNormalBenchmarkAsync>d__106&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.HotUpdate.NetworkBenchmarkRunner.<RunRpcBenchmarkAsync>d__107>(MiniCore.Threading.MTaskAwaiter&,MiniCore.HotUpdate.NetworkBenchmarkRunner.<RunRpcBenchmarkAsync>d__107&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.HotUpdate.NetworkBenchmarkRunner.<RunTransportAsync>d__105>(MiniCore.Threading.MTaskAwaiter&,MiniCore.HotUpdate.NetworkBenchmarkRunner.<RunTransportAsync>d__105&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.HotUpdate.NetworkBenchmarkRunner.<WaitForNormalMessagesAsync>d__123>(MiniCore.Threading.MTaskAwaiter&,MiniCore.HotUpdate.NetworkBenchmarkRunner.<WaitForNormalMessagesAsync>d__123&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.HotUpdate.NetworkBenchmarkRunner.<WaitForWarmupMessagesAsync>d__124>(MiniCore.Threading.MTaskAwaiter&,MiniCore.HotUpdate.NetworkBenchmarkRunner.<WaitForWarmupMessagesAsync>d__124&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.HotUpdate.NetworkSmokeTestRunner.<RunInternalAsync>d__43>(MiniCore.Threading.MTaskAwaiter&,MiniCore.HotUpdate.NetworkSmokeTestRunner.<RunInternalAsync>d__43&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.HotUpdate.NetworkSmokeTestRunner.<RunTransportAsync>d__47>(MiniCore.Threading.MTaskAwaiter&,MiniCore.HotUpdate.NetworkSmokeTestRunner.<RunTransportAsync>d__47&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.HotUpdate.TestHandler.<HandleAsync>d__0>(MiniCore.Threading.MTaskAwaiter&,MiniCore.HotUpdate.TestHandler.<HandleAsync>d__0&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.Service.UIService.<CloseNavigationAsync>d__25>(MiniCore.Threading.MTaskAwaiter&,MiniCore.Service.UIService.<CloseNavigationAsync>d__25&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.Service.UIService.<NavigateAsync>d__23<object>>(MiniCore.Threading.MTaskAwaiter&,MiniCore.Service.UIService.<NavigateAsync>d__23<object>&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.Service.UIService.<NavigateAsync>d__24>(MiniCore.Threading.MTaskAwaiter&,MiniCore.Service.UIService.<NavigateAsync>d__24&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.Service.YooAssetSceneService.<LoadSingleAsync>d__8>(MiniCore.Threading.MTaskAwaiter&,MiniCore.Service.YooAssetSceneService.<LoadSingleAsync>d__8&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.UI.UIWindowSession.<CloseInternalAsync>d__41>(MiniCore.Threading.MTaskAwaiter&,MiniCore.UI.UIWindowSession.<CloseInternalAsync>d__41&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.UI.UIWindowSession.<OpenInternalAsync>d__36>(MiniCore.Threading.MTaskAwaiter&,MiniCore.UI.UIWindowSession.<OpenInternalAsync>d__36&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<MiniCore.Demo.MiniBomber.MiniBomberRegisterResult>,MiniCore.Demo.MiniBomber.MiniBomberServerRuntimeComponent.<RegisterAsync>d__35>(MiniCore.Threading.MTaskAwaiter<MiniCore.Demo.MiniBomber.MiniBomberRegisterResult>&,MiniCore.Demo.MiniBomber.MiniBomberServerRuntimeComponent.<RegisterAsync>d__35&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<byte>,MiniCore.Demo.MiniBomber.LoginWindowPresenter.<LoginAsync>d__7>(MiniCore.Threading.MTaskAwaiter<byte>&,MiniCore.Demo.MiniBomber.LoginWindowPresenter.<LoginAsync>d__7&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<byte>,MiniCore.Demo.MiniBomber.LoginWindowPresenter.<OpenRegisterAsync>d__9>(MiniCore.Threading.MTaskAwaiter<byte>&,MiniCore.Demo.MiniBomber.LoginWindowPresenter.<OpenRegisterAsync>d__9&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<byte>,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<ReconnectAsync>d__40>(MiniCore.Threading.MTaskAwaiter<byte>&,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<ReconnectAsync>d__40&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<byte>,MiniCore.HotUpdate.DedicatedClientSmokeTestRunner.<RunInternalAsync>d__28>(MiniCore.Threading.MTaskAwaiter<byte>&,MiniCore.HotUpdate.DedicatedClientSmokeTestRunner.<RunInternalAsync>d__28&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<byte>,MiniCore.HotUpdate.KcpTestWindowPresenter.<ConnectClientAsync>d__19>(MiniCore.Threading.MTaskAwaiter<byte>&,MiniCore.HotUpdate.KcpTestWindowPresenter.<ConnectClientAsync>d__19&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<byte>,MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectKcpAsync>d__48>(MiniCore.Threading.MTaskAwaiter<byte>&,MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectKcpAsync>d__48&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<byte>,MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectTcpAsync>d__47>(MiniCore.Threading.MTaskAwaiter<byte>&,MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectTcpAsync>d__47&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<byte>,MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectUdpAsync>d__49>(MiniCore.Threading.MTaskAwaiter<byte>&,MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectUdpAsync>d__49&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<byte>,MiniCore.HotUpdate.NetworkBenchmarkRunner.<RunTransportAsync>d__105>(MiniCore.Threading.MTaskAwaiter<byte>&,MiniCore.HotUpdate.NetworkBenchmarkRunner.<RunTransportAsync>d__105&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<byte>,MiniCore.HotUpdate.NetworkSmokeTestRunner.<RunTransportAsync>d__47>(MiniCore.Threading.MTaskAwaiter<byte>&,MiniCore.HotUpdate.NetworkSmokeTestRunner.<RunTransportAsync>d__47&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Core.GameObjectPool.<PrewarmAsync>d__12>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Core.GameObjectPool.<PrewarmAsync>d__12&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Demo.MiniBomber.AccountSessionComponent.<InitializeAsync>d__20>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Demo.MiniBomber.AccountSessionComponent.<InitializeAsync>d__20&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Demo.MiniBomber.BattleClientComponent.<RequestResyncAsync>d__35>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Demo.MiniBomber.BattleClientComponent.<RequestResyncAsync>d__35&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Demo.MiniBomber.CreateRoomPopupPresenter.<SubmitAsync>d__8>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Demo.MiniBomber.CreateRoomPopupPresenter.<SubmitAsync>d__8&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Demo.MiniBomber.LobbyWindowPresenter.<JoinAsync>d__15>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Demo.MiniBomber.LobbyWindowPresenter.<JoinAsync>d__15&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Demo.MiniBomber.LobbyWindowPresenter.<OpenCreateAsync>d__13>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Demo.MiniBomber.LobbyWindowPresenter.<OpenCreateAsync>d__13&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Demo.MiniBomber.LobbyWindowPresenter.<RefreshAsync>d__11>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Demo.MiniBomber.LobbyWindowPresenter.<RefreshAsync>d__11&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Demo.MiniBomber.LoginWindowPresenter.<LoginAsync>d__7>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Demo.MiniBomber.LoginWindowPresenter.<LoginAsync>d__7&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Demo.MiniBomber.LoginWindowPresenter.<OpenRegisterAsync>d__9>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Demo.MiniBomber.LoginWindowPresenter.<OpenRegisterAsync>d__9&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Demo.MiniBomber.MiniBomberAccountRepositoryComponent.<InitializeAsync>d__0>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Demo.MiniBomber.MiniBomberAccountRepositoryComponent.<InitializeAsync>d__0&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Demo.MiniBomber.MiniBomberBattlePresentationComponent.<ToggleNetworkDebugAsync>d__28>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Demo.MiniBomber.MiniBomberBattlePresentationComponent.<ToggleNetworkDebugAsync>d__28&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<HandleMatchPrepareAsync>d__36>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<HandleMatchPrepareAsync>d__36&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<InitializeAsync>d__34>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<InitializeAsync>d__34&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<NavigateAsync>d__35>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<NavigateAsync>d__35&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<OpenBattleHudAsync>d__47>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<OpenBattleHudAsync>d__47&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<OpenReconnectOverlayAsync>d__41>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<OpenReconnectOverlayAsync>d__41&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<OpenResultAsync>d__44>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<OpenResultAsync>d__44&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<ReconnectAsync>d__40>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<ReconnectAsync>d__40&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Demo.MiniBomber.MiniBomberStartupComponentBase.<LoadConfigurationAsync>d__16>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Demo.MiniBomber.MiniBomberStartupComponentBase.<LoadConfigurationAsync>d__16&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Demo.MiniBomber.RegisterWindowPresenter.<SubmitAsync>d__6>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Demo.MiniBomber.RegisterWindowPresenter.<SubmitAsync>d__6&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Demo.MiniBomber.RoomWindowPresenter.<ApplySettingsAsync>d__11>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Demo.MiniBomber.RoomWindowPresenter.<ApplySettingsAsync>d__11&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Demo.MiniBomber.RoomWindowPresenter.<LeaveAsync>d__17>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Demo.MiniBomber.RoomWindowPresenter.<LeaveAsync>d__17&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Demo.MiniBomber.RoomWindowPresenter.<StartMatchAsync>d__15>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Demo.MiniBomber.RoomWindowPresenter.<StartMatchAsync>d__15&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Demo.MiniBomber.RoomWindowPresenter.<ToggleReadyAsync>d__13>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Demo.MiniBomber.RoomWindowPresenter.<ToggleReadyAsync>d__13&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.HotUpdate.DedicatedClientSmokeTestRunner.<RunInternalAsync>d__28>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.HotUpdate.DedicatedClientSmokeTestRunner.<RunInternalAsync>d__28&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.HotUpdate.KcpTestWindowPresenter.<SendRpcAsync>d__26>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.HotUpdate.KcpTestWindowPresenter.<SendRpcAsync>d__26&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.HotUpdate.MultiProtocolTestPanel.<SendRpcAsync>d__52>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.HotUpdate.MultiProtocolTestPanel.<SendRpcAsync>d__52&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.HotUpdate.NetworkBenchmarkRunner.<RunRpcCallAsync>d__110>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.HotUpdate.NetworkBenchmarkRunner.<RunRpcCallAsync>d__110&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.HotUpdate.NetworkSmokeTestRunner.<RunTransportAsync>d__47>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.HotUpdate.NetworkSmokeTestRunner.<RunTransportAsync>d__47&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Service.UIService.<InitializeAsync>d__20>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Service.UIService.<InitializeAsync>d__20&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Service.UIService.<NavigateAsync>d__23<object>>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Service.UIService.<NavigateAsync>d__23<object>&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Service.UIService.<NavigateAsync>d__24>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Service.UIService.<NavigateAsync>d__24&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Service.UIService.<PrefetchAsync>d__26<object>>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Service.UIService.<PrefetchAsync>d__26<object>&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.UI.UIWindowSession.<OpenInternalAsync>d__36>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.UI.UIWindowSession.<OpenInternalAsync>d__36&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskSwitchAwaiter,MiniCore.HotUpdate.KcpTestWindowPresenter.<HandleClientDisconnectedAsync>d__22>(MiniCore.Threading.MTaskSwitchAwaiter&,MiniCore.HotUpdate.KcpTestWindowPresenter.<HandleClientDisconnectedAsync>d__22&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskYieldAwaiter,MiniCore.HotUpdate.NetworkBenchmarkRunner.<SendNormalMessagesForDurationAsync>d__109>(MiniCore.Threading.MTaskYieldAwaiter&,MiniCore.HotUpdate.NetworkBenchmarkRunner.<SendNormalMessagesForDurationAsync>d__109&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskYieldAwaiter,MiniCore.HotUpdate.NetworkBenchmarkRunner.<WaitForConditionAsync>d__126>(MiniCore.Threading.MTaskYieldAwaiter&,MiniCore.HotUpdate.NetworkBenchmarkRunner.<WaitForConditionAsync>d__126&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskYieldAwaiter,MiniCore.HotUpdate.NetworkBenchmarkRunner.<WaitForNormalMessagesAsync>d__123>(MiniCore.Threading.MTaskYieldAwaiter&,MiniCore.HotUpdate.NetworkBenchmarkRunner.<WaitForNormalMessagesAsync>d__123&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskYieldAwaiter,MiniCore.HotUpdate.NetworkBenchmarkRunner.<WaitForQueueToDrainAsync>d__125>(MiniCore.Threading.MTaskYieldAwaiter&,MiniCore.HotUpdate.NetworkBenchmarkRunner.<WaitForQueueToDrainAsync>d__125&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskYieldAwaiter,MiniCore.HotUpdate.NetworkBenchmarkRunner.<WaitForWarmupMessagesAsync>d__124>(MiniCore.Threading.MTaskYieldAwaiter&,MiniCore.HotUpdate.NetworkBenchmarkRunner.<WaitForWarmupMessagesAsync>d__124&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskYieldAwaiter,MiniCore.HotUpdate.NetworkSmokeTestRunner.<WaitForConditionAsync>d__48>(MiniCore.Threading.MTaskYieldAwaiter&,MiniCore.HotUpdate.NetworkSmokeTestRunner.<WaitForConditionAsync>d__48&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskYieldAwaiter,MiniCore.Service.YooAssetSceneService.<LoadSingleAsync>d__8>(MiniCore.Threading.MTaskYieldAwaiter&,MiniCore.Service.YooAssetSceneService.<LoadSingleAsync>d__8&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<MiniCore.Demo.MiniBomber.MiniBomberRegisterResult>.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.Demo.MiniBomber.MiniBomberAccountRepositoryComponent.<RegisterAsync>d__1>(MiniCore.Threading.MTaskAwaiter&,MiniCore.Demo.MiniBomber.MiniBomberAccountRepositoryComponent.<RegisterAsync>d__1&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<byte>.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<byte>,MiniCore.Demo.MiniBomber.AccountSessionComponent.<ConnectAsync>d__21>(MiniCore.Threading.MTaskAwaiter<byte>&,MiniCore.Demo.MiniBomber.AccountSessionComponent.<ConnectAsync>d__21&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<MiniCore.Threading.MSharedTaskAwaiter<object>,MiniCore.UI.UIWindowSession.<WaitUntilActiveAsync>d__30>(MiniCore.Threading.MSharedTaskAwaiter<object>&,MiniCore.UI.UIWindowSession.<WaitUntilActiveAsync>d__30&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.Demo.MiniBomber.AccountSessionComponent.<LoginAsync>d__23>(MiniCore.Threading.MTaskAwaiter&,MiniCore.Demo.MiniBomber.AccountSessionComponent.<LoginAsync>d__23&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.Demo.MiniBomber.AccountSessionComponent.<ResumeAsync>d__24>(MiniCore.Threading.MTaskAwaiter&,MiniCore.Demo.MiniBomber.AccountSessionComponent.<ResumeAsync>d__24&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.Service.UIService.<CreateViewAsync>d__43>(MiniCore.Threading.MTaskAwaiter&,MiniCore.Service.UIService.<CreateViewAsync>d__43&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.Service.UIService.<HandleDuplicateAsync>d__40>(MiniCore.Threading.MTaskAwaiter&,MiniCore.Service.UIService.<HandleDuplicateAsync>d__40&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.Service.UIService.<OpenCoreAsync>d__39>(MiniCore.Threading.MTaskAwaiter&,MiniCore.Service.UIService.<OpenCoreAsync>d__39&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.Service.YooAssetResourceService.<LoadAssetAsync>d__3<object>>(MiniCore.Threading.MTaskAwaiter&,MiniCore.Service.YooAssetResourceService.<LoadAssetAsync>d__3<object>&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.Service.YooAssetResourceService.<PreloadAssetAsync>d__4<object>>(MiniCore.Threading.MTaskAwaiter&,MiniCore.Service.YooAssetResourceService.<PreloadAssetAsync>d__4<object>&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Core.ExcelTool.<LoadCsvFileAsync>d__1<object>>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Core.ExcelTool.<LoadCsvFileAsync>d__1<object>&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Core.GameObjectPool.<CreateAsync>d__16>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Core.GameObjectPool.<CreateAsync>d__16&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Core.GameObjectPool.<RentAsync>d__10<object>>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Core.GameObjectPool.<RentAsync>d__10<object>&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Core.GameObjectPoolMgr.<GeneratePoolObject>d__5<object>>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Core.GameObjectPoolMgr.<GeneratePoolObject>d__5<object>&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Demo.MiniBomber.AccountSessionComponent.<LoginAsync>d__23>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Demo.MiniBomber.AccountSessionComponent.<LoginAsync>d__23&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Demo.MiniBomber.AccountSessionComponent.<ResumeAsync>d__24>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Demo.MiniBomber.AccountSessionComponent.<ResumeAsync>d__24&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Demo.MiniBomber.LobbyComponent.<RefreshAsync>d__17>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Demo.MiniBomber.LobbyComponent.<RefreshAsync>d__17&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<OpenWindowIfAvailableAsync>d__53>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<OpenWindowIfAvailableAsync>d__53&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Demo.MiniBomber.RoomComponent.<CreateAsync>d__12>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Demo.MiniBomber.RoomComponent.<CreateAsync>d__12&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Demo.MiniBomber.RoomComponent.<JoinAsync>d__13>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Demo.MiniBomber.RoomComponent.<JoinAsync>d__13&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Demo.MiniBomber.RoomComponent.<LeaveAsync>d__14>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Demo.MiniBomber.RoomComponent.<LeaveAsync>d__14&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Demo.MiniBomber.RoomComponent.<SetReadyAsync>d__16>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Demo.MiniBomber.RoomComponent.<SetReadyAsync>d__16&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Demo.MiniBomber.RoomComponent.<UpdateSettingsAsync>d__15>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Demo.MiniBomber.RoomComponent.<UpdateSettingsAsync>d__15&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Service.AssetService.<PreloadAssetAsync>d__6<object>>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Service.AssetService.<PreloadAssetAsync>d__6<object>&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Service.UIService.<CreateViewAsync>d__43>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Service.UIService.<CreateViewAsync>d__43&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Service.UIService.<HandleDuplicateAsync>d__40>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Service.UIService.<HandleDuplicateAsync>d__40&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Service.UIService.<OpenCoreAsync>d__39>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Service.UIService.<OpenCoreAsync>d__39&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Service.UIService.<ShowAsync>d__29<object,object,object>>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Service.UIService.<ShowAsync>d__29<object,object,object>&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Service.YooAssetResourceService.<InstantiateAsync>d__2>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Service.YooAssetResourceService.<InstantiateAsync>d__2&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Core.GameObjectPool.<PrewarmAsync>d__12>(MiniCore.Core.GameObjectPool.<PrewarmAsync>d__12&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.AccountSessionComponent.<InitializeAsync>d__20>(MiniCore.Demo.MiniBomber.AccountSessionComponent.<InitializeAsync>d__20&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.AccountSessionComponent.<LogoutAsync>d__25>(MiniCore.Demo.MiniBomber.AccountSessionComponent.<LogoutAsync>d__25&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.BattleClientComponent.<RequestResyncAsync>d__35>(MiniCore.Demo.MiniBomber.BattleClientComponent.<RequestResyncAsync>d__35&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.BattleHudWindowPresenter.<ClearKillFeedAsync>d__10>(MiniCore.Demo.MiniBomber.BattleHudWindowPresenter.<ClearKillFeedAsync>d__10&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.BattleHudWindowPresenter.<RefreshPerformanceLoopAsync>d__11>(MiniCore.Demo.MiniBomber.BattleHudWindowPresenter.<RefreshPerformanceLoopAsync>d__11&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.CreateRoomPopupPresenter.<SubmitAsync>d__8>(MiniCore.Demo.MiniBomber.CreateRoomPopupPresenter.<SubmitAsync>d__8&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.LobbyWindowPresenter.<JoinAsync>d__15>(MiniCore.Demo.MiniBomber.LobbyWindowPresenter.<JoinAsync>d__15&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.LobbyWindowPresenter.<LogoutAsync>d__17>(MiniCore.Demo.MiniBomber.LobbyWindowPresenter.<LogoutAsync>d__17&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.LobbyWindowPresenter.<OpenCreateAsync>d__13>(MiniCore.Demo.MiniBomber.LobbyWindowPresenter.<OpenCreateAsync>d__13&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.LobbyWindowPresenter.<RefreshAsync>d__11>(MiniCore.Demo.MiniBomber.LobbyWindowPresenter.<RefreshAsync>d__11&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.LoginWindowPresenter.<LoginAsync>d__7>(MiniCore.Demo.MiniBomber.LoginWindowPresenter.<LoginAsync>d__7&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.LoginWindowPresenter.<OpenRegisterAsync>d__9>(MiniCore.Demo.MiniBomber.LoginWindowPresenter.<OpenRegisterAsync>d__9&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.MiniBomberAccountRepositoryComponent.<InitializeAsync>d__0>(MiniCore.Demo.MiniBomber.MiniBomberAccountRepositoryComponent.<InitializeAsync>d__0&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.MiniBomberBattlePresentationComponent.<ToggleNetworkDebugAsync>d__28>(MiniCore.Demo.MiniBomber.MiniBomberBattlePresentationComponent.<ToggleNetworkDebugAsync>d__28&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<CloseBattleHudAsync>d__48>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<CloseBattleHudAsync>d__48&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<CloseLoadingAsync>d__51>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<CloseLoadingAsync>d__51&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<HandleMatchPrepareAsync>d__36>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<HandleMatchPrepareAsync>d__36&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<HandleNonResumableDisconnectAsync>d__52>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<HandleNonResumableDisconnectAsync>d__52&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<InitializeAsync>d__34>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<InitializeAsync>d__34&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<LoadSceneAsync>d__50>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<LoadSceneAsync>d__50&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<NavigateAsync>d__35>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<NavigateAsync>d__35&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<NavigateWindowIfAvailableAsync>d__54>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<NavigateWindowIfAvailableAsync>d__54&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<OpenBattleHudAsync>d__47>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<OpenBattleHudAsync>d__47&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<OpenReconnectOverlayAsync>d__41>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<OpenReconnectOverlayAsync>d__41&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<OpenResultAsync>d__44>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<OpenResultAsync>d__44&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<ReconnectAsync>d__40>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<ReconnectAsync>d__40&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<ReturnToRoomAsync>d__46>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<ReturnToRoomAsync>d__46&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.MiniBomberClientStartupComponent.<InitializeAsync>d__7>(MiniCore.Demo.MiniBomber.MiniBomberClientStartupComponent.<InitializeAsync>d__7&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.MiniBomberRegisterHandler.<HandleAsync>d__0>(MiniCore.Demo.MiniBomber.MiniBomberRegisterHandler.<HandleAsync>d__0&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.MiniBomberServerRuntimeComponent.<InitializeAsync>d__34>(MiniCore.Demo.MiniBomber.MiniBomberServerRuntimeComponent.<InitializeAsync>d__34&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.MiniBomberServerRuntimeComponent.<RegisterAsync>d__35>(MiniCore.Demo.MiniBomber.MiniBomberServerRuntimeComponent.<RegisterAsync>d__35&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.MiniBomberServerStartupComponent.<InitializeAsync>d__0>(MiniCore.Demo.MiniBomber.MiniBomberServerStartupComponent.<InitializeAsync>d__0&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.MiniBomberStartupComponentBase.<LoadConfigurationAsync>d__16>(MiniCore.Demo.MiniBomber.MiniBomberStartupComponentBase.<LoadConfigurationAsync>d__16&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.NetworkDebugWindowPresenter.<RefreshLoopAsync>d__6>(MiniCore.Demo.MiniBomber.NetworkDebugWindowPresenter.<RefreshLoopAsync>d__6&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.RegisterWindowPresenter.<SubmitAsync>d__6>(MiniCore.Demo.MiniBomber.RegisterWindowPresenter.<SubmitAsync>d__6&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.RoomWindowPresenter.<ApplySettingsAsync>d__11>(MiniCore.Demo.MiniBomber.RoomWindowPresenter.<ApplySettingsAsync>d__11&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.RoomWindowPresenter.<LeaveAsync>d__17>(MiniCore.Demo.MiniBomber.RoomWindowPresenter.<LeaveAsync>d__17&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.RoomWindowPresenter.<StartMatchAsync>d__15>(MiniCore.Demo.MiniBomber.RoomWindowPresenter.<StartMatchAsync>d__15&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.RoomWindowPresenter.<ToggleReadyAsync>d__13>(MiniCore.Demo.MiniBomber.RoomWindowPresenter.<ToggleReadyAsync>d__13&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.HotUpdate.DedicatedClientSmokeTestRunner.<RunInternalAsync>d__28>(MiniCore.HotUpdate.DedicatedClientSmokeTestRunner.<RunInternalAsync>d__28&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.HotUpdate.GameStartup.<StartAsync>d__0>(MiniCore.HotUpdate.GameStartup.<StartAsync>d__0&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.HotUpdate.KcpTestWindowPresenter.<ConnectClientAsync>d__19>(MiniCore.HotUpdate.KcpTestWindowPresenter.<ConnectClientAsync>d__19&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.HotUpdate.KcpTestWindowPresenter.<DisconnectClientAsync>d__20>(MiniCore.HotUpdate.KcpTestWindowPresenter.<DisconnectClientAsync>d__20&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.HotUpdate.KcpTestWindowPresenter.<HandleClientDisconnectedAsync>d__22>(MiniCore.HotUpdate.KcpTestWindowPresenter.<HandleClientDisconnectedAsync>d__22&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.HotUpdate.KcpTestWindowPresenter.<SendNormalAsync>d__25>(MiniCore.HotUpdate.KcpTestWindowPresenter.<SendNormalAsync>d__25&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.HotUpdate.KcpTestWindowPresenter.<SendRpcAsync>d__26>(MiniCore.HotUpdate.KcpTestWindowPresenter.<SendRpcAsync>d__26&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.HotUpdate.KcpTestWindowPresenter.<StartServerAsync>d__17>(MiniCore.HotUpdate.KcpTestWindowPresenter.<StartServerAsync>d__17&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.HotUpdate.KcpTestWindowPresenter.<StopServerAsync>d__21>(MiniCore.HotUpdate.KcpTestWindowPresenter.<StopServerAsync>d__21&)
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
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.HotUpdate.NetworkBenchmarkRunner.<RunInternalAsync>d__101>(MiniCore.HotUpdate.NetworkBenchmarkRunner.<RunInternalAsync>d__101&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.HotUpdate.NetworkBenchmarkRunner.<RunMainThreadHitchBenchmarkAsync>d__108>(MiniCore.HotUpdate.NetworkBenchmarkRunner.<RunMainThreadHitchBenchmarkAsync>d__108&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.HotUpdate.NetworkBenchmarkRunner.<RunNormalBenchmarkAsync>d__106>(MiniCore.HotUpdate.NetworkBenchmarkRunner.<RunNormalBenchmarkAsync>d__106&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.HotUpdate.NetworkBenchmarkRunner.<RunRpcBenchmarkAsync>d__107>(MiniCore.HotUpdate.NetworkBenchmarkRunner.<RunRpcBenchmarkAsync>d__107&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.HotUpdate.NetworkBenchmarkRunner.<RunRpcCallAsync>d__110>(MiniCore.HotUpdate.NetworkBenchmarkRunner.<RunRpcCallAsync>d__110&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.HotUpdate.NetworkBenchmarkRunner.<RunTransportAsync>d__105>(MiniCore.HotUpdate.NetworkBenchmarkRunner.<RunTransportAsync>d__105&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.HotUpdate.NetworkBenchmarkRunner.<SendNormalMessagesForDurationAsync>d__109>(MiniCore.HotUpdate.NetworkBenchmarkRunner.<SendNormalMessagesForDurationAsync>d__109&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.HotUpdate.NetworkBenchmarkRunner.<WaitForConditionAsync>d__126>(MiniCore.HotUpdate.NetworkBenchmarkRunner.<WaitForConditionAsync>d__126&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.HotUpdate.NetworkBenchmarkRunner.<WaitForNormalMessagesAsync>d__123>(MiniCore.HotUpdate.NetworkBenchmarkRunner.<WaitForNormalMessagesAsync>d__123&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.HotUpdate.NetworkBenchmarkRunner.<WaitForQueueToDrainAsync>d__125>(MiniCore.HotUpdate.NetworkBenchmarkRunner.<WaitForQueueToDrainAsync>d__125&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.HotUpdate.NetworkBenchmarkRunner.<WaitForWarmupMessagesAsync>d__124>(MiniCore.HotUpdate.NetworkBenchmarkRunner.<WaitForWarmupMessagesAsync>d__124&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.HotUpdate.NetworkSmokeTestRunner.<RunInternalAsync>d__43>(MiniCore.HotUpdate.NetworkSmokeTestRunner.<RunInternalAsync>d__43&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.HotUpdate.NetworkSmokeTestRunner.<RunTransportAsync>d__47>(MiniCore.HotUpdate.NetworkSmokeTestRunner.<RunTransportAsync>d__47&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.HotUpdate.NetworkSmokeTestRunner.<WaitForConditionAsync>d__48>(MiniCore.HotUpdate.NetworkSmokeTestRunner.<WaitForConditionAsync>d__48&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.HotUpdate.TestHandler.<HandleAsync>d__0>(MiniCore.HotUpdate.TestHandler.<HandleAsync>d__0&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Service.UIService.<AcquireResourceLeaseAsync>d__44>(MiniCore.Service.UIService.<AcquireResourceLeaseAsync>d__44&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Service.UIService.<CloseNavigationAsync>d__25>(MiniCore.Service.UIService.<CloseNavigationAsync>d__25&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Service.UIService.<InitializeAsync>d__20>(MiniCore.Service.UIService.<InitializeAsync>d__20&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Service.UIService.<NavigateAsync>d__23<object>>(MiniCore.Service.UIService.<NavigateAsync>d__23<object>&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Service.UIService.<NavigateAsync>d__24>(MiniCore.Service.UIService.<NavigateAsync>d__24&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Service.UIService.<PrefetchAsync>d__26<object>>(MiniCore.Service.UIService.<PrefetchAsync>d__26<object>&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Service.YooAssetSceneService.<LoadSingleAsync>d__8>(MiniCore.Service.YooAssetSceneService.<LoadSingleAsync>d__8&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.UI.UIWindowSession.<CloseInternalAsync>d__41>(MiniCore.UI.UIWindowSession.<CloseInternalAsync>d__41&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.UI.UIWindowSession.<OpenInternalAsync>d__36>(MiniCore.UI.UIWindowSession.<OpenInternalAsync>d__36&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.UI.UIWindowSession.<WaitUntilClosedAsync>d__31>(MiniCore.UI.UIWindowSession.<WaitUntilClosedAsync>d__31&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<MiniCore.Demo.MiniBomber.MiniBomberRegisterResult>.Start<MiniCore.Demo.MiniBomber.MiniBomberAccountRepositoryComponent.<RegisterAsync>d__1>(MiniCore.Demo.MiniBomber.MiniBomberAccountRepositoryComponent.<RegisterAsync>d__1&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<byte>.Start<MiniCore.Demo.MiniBomber.AccountSessionComponent.<ConnectAsync>d__21>(MiniCore.Demo.MiniBomber.AccountSessionComponent.<ConnectAsync>d__21&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.Start<MiniCore.Core.ExcelTool.<LoadCsvFileAsync>d__1<object>>(MiniCore.Core.ExcelTool.<LoadCsvFileAsync>d__1<object>&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.Start<MiniCore.Core.GameObjectPool.<CreateAsync>d__16>(MiniCore.Core.GameObjectPool.<CreateAsync>d__16&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.Start<MiniCore.Core.GameObjectPool.<RentAsync>d__10<object>>(MiniCore.Core.GameObjectPool.<RentAsync>d__10<object>&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.Start<MiniCore.Core.GameObjectPoolMgr.<GeneratePoolObject>d__5<object>>(MiniCore.Core.GameObjectPoolMgr.<GeneratePoolObject>d__5<object>&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.Start<MiniCore.Demo.MiniBomber.AccountSessionComponent.<LoginAsync>d__23>(MiniCore.Demo.MiniBomber.AccountSessionComponent.<LoginAsync>d__23&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.Start<MiniCore.Demo.MiniBomber.AccountSessionComponent.<ResumeAsync>d__24>(MiniCore.Demo.MiniBomber.AccountSessionComponent.<ResumeAsync>d__24&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.Start<MiniCore.Demo.MiniBomber.LobbyComponent.<RefreshAsync>d__17>(MiniCore.Demo.MiniBomber.LobbyComponent.<RefreshAsync>d__17&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.Start<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<OpenWindowIfAvailableAsync>d__53>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<OpenWindowIfAvailableAsync>d__53&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.Start<MiniCore.Demo.MiniBomber.RoomComponent.<CreateAsync>d__12>(MiniCore.Demo.MiniBomber.RoomComponent.<CreateAsync>d__12&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.Start<MiniCore.Demo.MiniBomber.RoomComponent.<JoinAsync>d__13>(MiniCore.Demo.MiniBomber.RoomComponent.<JoinAsync>d__13&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.Start<MiniCore.Demo.MiniBomber.RoomComponent.<LeaveAsync>d__14>(MiniCore.Demo.MiniBomber.RoomComponent.<LeaveAsync>d__14&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.Start<MiniCore.Demo.MiniBomber.RoomComponent.<SetReadyAsync>d__16>(MiniCore.Demo.MiniBomber.RoomComponent.<SetReadyAsync>d__16&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.Start<MiniCore.Demo.MiniBomber.RoomComponent.<UpdateSettingsAsync>d__15>(MiniCore.Demo.MiniBomber.RoomComponent.<UpdateSettingsAsync>d__15&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.Start<MiniCore.Serialization.ProtobufSaveServiceExtensions.<LoadProtobufAsync>d__1<object>>(MiniCore.Serialization.ProtobufSaveServiceExtensions.<LoadProtobufAsync>d__1<object>&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.Start<MiniCore.Service.AssetService.<PreloadAssetAsync>d__6<object>>(MiniCore.Service.AssetService.<PreloadAssetAsync>d__6<object>&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.Start<MiniCore.Service.UIService.<CreateViewAsync>d__43>(MiniCore.Service.UIService.<CreateViewAsync>d__43&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.Start<MiniCore.Service.UIService.<HandleDuplicateAsync>d__40>(MiniCore.Service.UIService.<HandleDuplicateAsync>d__40&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.Start<MiniCore.Service.UIService.<OpenCoreAsync>d__39>(MiniCore.Service.UIService.<OpenCoreAsync>d__39&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.Start<MiniCore.Service.UIService.<ShowAsync>d__29<object,object,object>>(MiniCore.Service.UIService.<ShowAsync>d__29<object,object,object>&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.Start<MiniCore.Service.YooAssetResourceService.<InstantiateAsync>d__2>(MiniCore.Service.YooAssetResourceService.<InstantiateAsync>d__2&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.Start<MiniCore.Service.YooAssetResourceService.<LoadAssetAsync>d__3<object>>(MiniCore.Service.YooAssetResourceService.<LoadAssetAsync>d__3<object>&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.Start<MiniCore.Service.YooAssetResourceService.<PreloadAssetAsync>d__4<object>>(MiniCore.Service.YooAssetResourceService.<PreloadAssetAsync>d__4<object>&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.Start<MiniCore.UI.UIWindowSession.<WaitUntilActiveAsync>d__30>(MiniCore.UI.UIWindowSession.<WaitUntilActiveAsync>d__30&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Core.ExcelTool.<LoadCsvFileAsync>d__1<object>>(MiniCore.Core.ExcelTool.<LoadCsvFileAsync>d__1<object>&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Core.GameObjectPool.<CreateAsync>d__16>(MiniCore.Core.GameObjectPool.<CreateAsync>d__16&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Core.GameObjectPool.<PrewarmAsync>d__12>(MiniCore.Core.GameObjectPool.<PrewarmAsync>d__12&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Core.GameObjectPool.<RentAsync>d__10<object>>(MiniCore.Core.GameObjectPool.<RentAsync>d__10<object>&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Core.GameObjectPoolMgr.<GeneratePoolObject>d__5<object>>(MiniCore.Core.GameObjectPoolMgr.<GeneratePoolObject>d__5<object>&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.AccountSessionComponent.<ConnectAsync>d__21>(MiniCore.Demo.MiniBomber.AccountSessionComponent.<ConnectAsync>d__21&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.AccountSessionComponent.<InitializeAsync>d__20>(MiniCore.Demo.MiniBomber.AccountSessionComponent.<InitializeAsync>d__20&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.AccountSessionComponent.<LoginAsync>d__23>(MiniCore.Demo.MiniBomber.AccountSessionComponent.<LoginAsync>d__23&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.AccountSessionComponent.<LogoutAsync>d__25>(MiniCore.Demo.MiniBomber.AccountSessionComponent.<LogoutAsync>d__25&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.AccountSessionComponent.<ResumeAsync>d__24>(MiniCore.Demo.MiniBomber.AccountSessionComponent.<ResumeAsync>d__24&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.BattleClientComponent.<RequestResyncAsync>d__35>(MiniCore.Demo.MiniBomber.BattleClientComponent.<RequestResyncAsync>d__35&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.BattleHudWindowPresenter.<ClearKillFeedAsync>d__10>(MiniCore.Demo.MiniBomber.BattleHudWindowPresenter.<ClearKillFeedAsync>d__10&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.BattleHudWindowPresenter.<RefreshPerformanceLoopAsync>d__11>(MiniCore.Demo.MiniBomber.BattleHudWindowPresenter.<RefreshPerformanceLoopAsync>d__11&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.CreateRoomPopupPresenter.<SubmitAsync>d__8>(MiniCore.Demo.MiniBomber.CreateRoomPopupPresenter.<SubmitAsync>d__8&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.LobbyComponent.<RefreshAsync>d__17>(MiniCore.Demo.MiniBomber.LobbyComponent.<RefreshAsync>d__17&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.LobbyWindowPresenter.<JoinAsync>d__15>(MiniCore.Demo.MiniBomber.LobbyWindowPresenter.<JoinAsync>d__15&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.LobbyWindowPresenter.<LogoutAsync>d__17>(MiniCore.Demo.MiniBomber.LobbyWindowPresenter.<LogoutAsync>d__17&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.LobbyWindowPresenter.<OpenCreateAsync>d__13>(MiniCore.Demo.MiniBomber.LobbyWindowPresenter.<OpenCreateAsync>d__13&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.LobbyWindowPresenter.<RefreshAsync>d__11>(MiniCore.Demo.MiniBomber.LobbyWindowPresenter.<RefreshAsync>d__11&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.LoginWindowPresenter.<LoginAsync>d__7>(MiniCore.Demo.MiniBomber.LoginWindowPresenter.<LoginAsync>d__7&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.LoginWindowPresenter.<OpenRegisterAsync>d__9>(MiniCore.Demo.MiniBomber.LoginWindowPresenter.<OpenRegisterAsync>d__9&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.MiniBomberAccountRepositoryComponent.<InitializeAsync>d__0>(MiniCore.Demo.MiniBomber.MiniBomberAccountRepositoryComponent.<InitializeAsync>d__0&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.MiniBomberAccountRepositoryComponent.<RegisterAsync>d__1>(MiniCore.Demo.MiniBomber.MiniBomberAccountRepositoryComponent.<RegisterAsync>d__1&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.MiniBomberBattlePresentationComponent.<ToggleNetworkDebugAsync>d__28>(MiniCore.Demo.MiniBomber.MiniBomberBattlePresentationComponent.<ToggleNetworkDebugAsync>d__28&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<CloseBattleHudAsync>d__48>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<CloseBattleHudAsync>d__48&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<CloseLoadingAsync>d__51>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<CloseLoadingAsync>d__51&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<HandleMatchPrepareAsync>d__36>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<HandleMatchPrepareAsync>d__36&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<HandleNonResumableDisconnectAsync>d__52>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<HandleNonResumableDisconnectAsync>d__52&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<InitializeAsync>d__34>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<InitializeAsync>d__34&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<LoadSceneAsync>d__50>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<LoadSceneAsync>d__50&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<NavigateAsync>d__35>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<NavigateAsync>d__35&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<NavigateWindowIfAvailableAsync>d__54>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<NavigateWindowIfAvailableAsync>d__54&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<OpenBattleHudAsync>d__47>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<OpenBattleHudAsync>d__47&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<OpenReconnectOverlayAsync>d__41>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<OpenReconnectOverlayAsync>d__41&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<OpenResultAsync>d__44>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<OpenResultAsync>d__44&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<OpenWindowIfAvailableAsync>d__53>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<OpenWindowIfAvailableAsync>d__53&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<ReconnectAsync>d__40>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<ReconnectAsync>d__40&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<ReturnToRoomAsync>d__46>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<ReturnToRoomAsync>d__46&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.MiniBomberClientStartupComponent.<InitializeAsync>d__7>(MiniCore.Demo.MiniBomber.MiniBomberClientStartupComponent.<InitializeAsync>d__7&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.MiniBomberRegisterHandler.<HandleAsync>d__0>(MiniCore.Demo.MiniBomber.MiniBomberRegisterHandler.<HandleAsync>d__0&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.MiniBomberServerRuntimeComponent.<InitializeAsync>d__34>(MiniCore.Demo.MiniBomber.MiniBomberServerRuntimeComponent.<InitializeAsync>d__34&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.MiniBomberServerRuntimeComponent.<RegisterAsync>d__35>(MiniCore.Demo.MiniBomber.MiniBomberServerRuntimeComponent.<RegisterAsync>d__35&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.MiniBomberServerStartupComponent.<InitializeAsync>d__0>(MiniCore.Demo.MiniBomber.MiniBomberServerStartupComponent.<InitializeAsync>d__0&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.MiniBomberStartupComponentBase.<LoadConfigurationAsync>d__16>(MiniCore.Demo.MiniBomber.MiniBomberStartupComponentBase.<LoadConfigurationAsync>d__16&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.NetworkDebugWindowPresenter.<RefreshLoopAsync>d__6>(MiniCore.Demo.MiniBomber.NetworkDebugWindowPresenter.<RefreshLoopAsync>d__6&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.RegisterWindowPresenter.<SubmitAsync>d__6>(MiniCore.Demo.MiniBomber.RegisterWindowPresenter.<SubmitAsync>d__6&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.RoomComponent.<CreateAsync>d__12>(MiniCore.Demo.MiniBomber.RoomComponent.<CreateAsync>d__12&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.RoomComponent.<JoinAsync>d__13>(MiniCore.Demo.MiniBomber.RoomComponent.<JoinAsync>d__13&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.RoomComponent.<LeaveAsync>d__14>(MiniCore.Demo.MiniBomber.RoomComponent.<LeaveAsync>d__14&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.RoomComponent.<SetReadyAsync>d__16>(MiniCore.Demo.MiniBomber.RoomComponent.<SetReadyAsync>d__16&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.RoomComponent.<UpdateSettingsAsync>d__15>(MiniCore.Demo.MiniBomber.RoomComponent.<UpdateSettingsAsync>d__15&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.RoomWindowPresenter.<ApplySettingsAsync>d__11>(MiniCore.Demo.MiniBomber.RoomWindowPresenter.<ApplySettingsAsync>d__11&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.RoomWindowPresenter.<LeaveAsync>d__17>(MiniCore.Demo.MiniBomber.RoomWindowPresenter.<LeaveAsync>d__17&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.RoomWindowPresenter.<StartMatchAsync>d__15>(MiniCore.Demo.MiniBomber.RoomWindowPresenter.<StartMatchAsync>d__15&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.RoomWindowPresenter.<ToggleReadyAsync>d__13>(MiniCore.Demo.MiniBomber.RoomWindowPresenter.<ToggleReadyAsync>d__13&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.HotUpdate.DedicatedClientSmokeTestRunner.<RunInternalAsync>d__28>(MiniCore.HotUpdate.DedicatedClientSmokeTestRunner.<RunInternalAsync>d__28&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.HotUpdate.GameStartup.<StartAsync>d__0>(MiniCore.HotUpdate.GameStartup.<StartAsync>d__0&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.HotUpdate.KcpTestWindowPresenter.<ConnectClientAsync>d__19>(MiniCore.HotUpdate.KcpTestWindowPresenter.<ConnectClientAsync>d__19&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.HotUpdate.KcpTestWindowPresenter.<DisconnectClientAsync>d__20>(MiniCore.HotUpdate.KcpTestWindowPresenter.<DisconnectClientAsync>d__20&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.HotUpdate.KcpTestWindowPresenter.<HandleClientDisconnectedAsync>d__22>(MiniCore.HotUpdate.KcpTestWindowPresenter.<HandleClientDisconnectedAsync>d__22&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.HotUpdate.KcpTestWindowPresenter.<SendNormalAsync>d__25>(MiniCore.HotUpdate.KcpTestWindowPresenter.<SendNormalAsync>d__25&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.HotUpdate.KcpTestWindowPresenter.<SendRpcAsync>d__26>(MiniCore.HotUpdate.KcpTestWindowPresenter.<SendRpcAsync>d__26&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.HotUpdate.KcpTestWindowPresenter.<StartServerAsync>d__17>(MiniCore.HotUpdate.KcpTestWindowPresenter.<StartServerAsync>d__17&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.HotUpdate.KcpTestWindowPresenter.<StopServerAsync>d__21>(MiniCore.HotUpdate.KcpTestWindowPresenter.<StopServerAsync>d__21&)
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
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.HotUpdate.NetworkBenchmarkRunner.<RunInternalAsync>d__101>(MiniCore.HotUpdate.NetworkBenchmarkRunner.<RunInternalAsync>d__101&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.HotUpdate.NetworkBenchmarkRunner.<RunMainThreadHitchBenchmarkAsync>d__108>(MiniCore.HotUpdate.NetworkBenchmarkRunner.<RunMainThreadHitchBenchmarkAsync>d__108&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.HotUpdate.NetworkBenchmarkRunner.<RunNormalBenchmarkAsync>d__106>(MiniCore.HotUpdate.NetworkBenchmarkRunner.<RunNormalBenchmarkAsync>d__106&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.HotUpdate.NetworkBenchmarkRunner.<RunRpcBenchmarkAsync>d__107>(MiniCore.HotUpdate.NetworkBenchmarkRunner.<RunRpcBenchmarkAsync>d__107&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.HotUpdate.NetworkBenchmarkRunner.<RunRpcCallAsync>d__110>(MiniCore.HotUpdate.NetworkBenchmarkRunner.<RunRpcCallAsync>d__110&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.HotUpdate.NetworkBenchmarkRunner.<RunTransportAsync>d__105>(MiniCore.HotUpdate.NetworkBenchmarkRunner.<RunTransportAsync>d__105&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.HotUpdate.NetworkBenchmarkRunner.<SendNormalMessagesForDurationAsync>d__109>(MiniCore.HotUpdate.NetworkBenchmarkRunner.<SendNormalMessagesForDurationAsync>d__109&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.HotUpdate.NetworkBenchmarkRunner.<WaitForConditionAsync>d__126>(MiniCore.HotUpdate.NetworkBenchmarkRunner.<WaitForConditionAsync>d__126&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.HotUpdate.NetworkBenchmarkRunner.<WaitForNormalMessagesAsync>d__123>(MiniCore.HotUpdate.NetworkBenchmarkRunner.<WaitForNormalMessagesAsync>d__123&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.HotUpdate.NetworkBenchmarkRunner.<WaitForQueueToDrainAsync>d__125>(MiniCore.HotUpdate.NetworkBenchmarkRunner.<WaitForQueueToDrainAsync>d__125&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.HotUpdate.NetworkBenchmarkRunner.<WaitForWarmupMessagesAsync>d__124>(MiniCore.HotUpdate.NetworkBenchmarkRunner.<WaitForWarmupMessagesAsync>d__124&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.HotUpdate.NetworkSmokeTestRunner.<RunInternalAsync>d__43>(MiniCore.HotUpdate.NetworkSmokeTestRunner.<RunInternalAsync>d__43&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.HotUpdate.NetworkSmokeTestRunner.<RunTransportAsync>d__47>(MiniCore.HotUpdate.NetworkSmokeTestRunner.<RunTransportAsync>d__47&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.HotUpdate.NetworkSmokeTestRunner.<WaitForConditionAsync>d__48>(MiniCore.HotUpdate.NetworkSmokeTestRunner.<WaitForConditionAsync>d__48&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.HotUpdate.TestHandler.<HandleAsync>d__0>(MiniCore.HotUpdate.TestHandler.<HandleAsync>d__0&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Service.AssetService.<PreloadAssetAsync>d__6<object>>(MiniCore.Service.AssetService.<PreloadAssetAsync>d__6<object>&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Service.UIService.<AcquireResourceLeaseAsync>d__44>(MiniCore.Service.UIService.<AcquireResourceLeaseAsync>d__44&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Service.UIService.<CloseNavigationAsync>d__25>(MiniCore.Service.UIService.<CloseNavigationAsync>d__25&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Service.UIService.<CreateViewAsync>d__43>(MiniCore.Service.UIService.<CreateViewAsync>d__43&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Service.UIService.<HandleDuplicateAsync>d__40>(MiniCore.Service.UIService.<HandleDuplicateAsync>d__40&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Service.UIService.<InitializeAsync>d__20>(MiniCore.Service.UIService.<InitializeAsync>d__20&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Service.UIService.<NavigateAsync>d__23<object>>(MiniCore.Service.UIService.<NavigateAsync>d__23<object>&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Service.UIService.<NavigateAsync>d__24>(MiniCore.Service.UIService.<NavigateAsync>d__24&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Service.UIService.<OpenCoreAsync>d__39>(MiniCore.Service.UIService.<OpenCoreAsync>d__39&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Service.UIService.<PrefetchAsync>d__26<object>>(MiniCore.Service.UIService.<PrefetchAsync>d__26<object>&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Service.UIService.<ShowAsync>d__29<object,object,object>>(MiniCore.Service.UIService.<ShowAsync>d__29<object,object,object>&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Service.YooAssetResourceService.<InstantiateAsync>d__2>(MiniCore.Service.YooAssetResourceService.<InstantiateAsync>d__2&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Service.YooAssetResourceService.<LoadAssetAsync>d__3<object>>(MiniCore.Service.YooAssetResourceService.<LoadAssetAsync>d__3<object>&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Service.YooAssetResourceService.<PreloadAssetAsync>d__4<object>>(MiniCore.Service.YooAssetResourceService.<PreloadAssetAsync>d__4<object>&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Service.YooAssetSceneService.<LoadSingleAsync>d__8>(MiniCore.Service.YooAssetSceneService.<LoadSingleAsync>d__8&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.UI.UIWindowSession.<CloseInternalAsync>d__41>(MiniCore.UI.UIWindowSession.<CloseInternalAsync>d__41&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.UI.UIWindowSession.<OpenInternalAsync>d__36>(MiniCore.UI.UIWindowSession.<OpenInternalAsync>d__36&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.UI.UIWindowSession.<WaitUntilActiveAsync>d__30>(MiniCore.UI.UIWindowSession.<WaitUntilActiveAsync>d__30&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.UI.UIWindowSession.<WaitUntilClosedAsync>d__31>(MiniCore.UI.UIWindowSession.<WaitUntilClosedAsync>d__31&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Core.ExcelTool.<LoadCsvFileAsync>d__1<object>>(MiniCore.Core.ExcelTool.<LoadCsvFileAsync>d__1<object>&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Core.GameObjectPool.<CreateAsync>d__16>(MiniCore.Core.GameObjectPool.<CreateAsync>d__16&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Core.GameObjectPool.<PrewarmAsync>d__12>(MiniCore.Core.GameObjectPool.<PrewarmAsync>d__12&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Core.GameObjectPool.<RentAsync>d__10<object>>(MiniCore.Core.GameObjectPool.<RentAsync>d__10<object>&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Core.GameObjectPoolMgr.<GeneratePoolObject>d__5<object>>(MiniCore.Core.GameObjectPoolMgr.<GeneratePoolObject>d__5<object>&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.AccountSessionComponent.<ConnectAsync>d__21>(MiniCore.Demo.MiniBomber.AccountSessionComponent.<ConnectAsync>d__21&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.AccountSessionComponent.<InitializeAsync>d__20>(MiniCore.Demo.MiniBomber.AccountSessionComponent.<InitializeAsync>d__20&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.AccountSessionComponent.<LoginAsync>d__23>(MiniCore.Demo.MiniBomber.AccountSessionComponent.<LoginAsync>d__23&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.AccountSessionComponent.<LogoutAsync>d__25>(MiniCore.Demo.MiniBomber.AccountSessionComponent.<LogoutAsync>d__25&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.AccountSessionComponent.<ResumeAsync>d__24>(MiniCore.Demo.MiniBomber.AccountSessionComponent.<ResumeAsync>d__24&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.BattleClientComponent.<RequestResyncAsync>d__35>(MiniCore.Demo.MiniBomber.BattleClientComponent.<RequestResyncAsync>d__35&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.BattleHudWindowPresenter.<ClearKillFeedAsync>d__10>(MiniCore.Demo.MiniBomber.BattleHudWindowPresenter.<ClearKillFeedAsync>d__10&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.BattleHudWindowPresenter.<RefreshPerformanceLoopAsync>d__11>(MiniCore.Demo.MiniBomber.BattleHudWindowPresenter.<RefreshPerformanceLoopAsync>d__11&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.CreateRoomPopupPresenter.<SubmitAsync>d__8>(MiniCore.Demo.MiniBomber.CreateRoomPopupPresenter.<SubmitAsync>d__8&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.LobbyComponent.<RefreshAsync>d__17>(MiniCore.Demo.MiniBomber.LobbyComponent.<RefreshAsync>d__17&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.LobbyWindowPresenter.<JoinAsync>d__15>(MiniCore.Demo.MiniBomber.LobbyWindowPresenter.<JoinAsync>d__15&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.LobbyWindowPresenter.<LogoutAsync>d__17>(MiniCore.Demo.MiniBomber.LobbyWindowPresenter.<LogoutAsync>d__17&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.LobbyWindowPresenter.<OpenCreateAsync>d__13>(MiniCore.Demo.MiniBomber.LobbyWindowPresenter.<OpenCreateAsync>d__13&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.LobbyWindowPresenter.<RefreshAsync>d__11>(MiniCore.Demo.MiniBomber.LobbyWindowPresenter.<RefreshAsync>d__11&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.LoginWindowPresenter.<LoginAsync>d__7>(MiniCore.Demo.MiniBomber.LoginWindowPresenter.<LoginAsync>d__7&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.LoginWindowPresenter.<OpenRegisterAsync>d__9>(MiniCore.Demo.MiniBomber.LoginWindowPresenter.<OpenRegisterAsync>d__9&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.MiniBomberAccountRepositoryComponent.<InitializeAsync>d__0>(MiniCore.Demo.MiniBomber.MiniBomberAccountRepositoryComponent.<InitializeAsync>d__0&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.MiniBomberAccountRepositoryComponent.<RegisterAsync>d__1>(MiniCore.Demo.MiniBomber.MiniBomberAccountRepositoryComponent.<RegisterAsync>d__1&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.MiniBomberBattlePresentationComponent.<ToggleNetworkDebugAsync>d__28>(MiniCore.Demo.MiniBomber.MiniBomberBattlePresentationComponent.<ToggleNetworkDebugAsync>d__28&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<CloseBattleHudAsync>d__48>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<CloseBattleHudAsync>d__48&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<CloseLoadingAsync>d__51>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<CloseLoadingAsync>d__51&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<HandleMatchPrepareAsync>d__36>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<HandleMatchPrepareAsync>d__36&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<HandleNonResumableDisconnectAsync>d__52>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<HandleNonResumableDisconnectAsync>d__52&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<InitializeAsync>d__34>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<InitializeAsync>d__34&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<LoadSceneAsync>d__50>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<LoadSceneAsync>d__50&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<NavigateAsync>d__35>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<NavigateAsync>d__35&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<NavigateWindowIfAvailableAsync>d__54>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<NavigateWindowIfAvailableAsync>d__54&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<OpenBattleHudAsync>d__47>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<OpenBattleHudAsync>d__47&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<OpenReconnectOverlayAsync>d__41>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<OpenReconnectOverlayAsync>d__41&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<OpenResultAsync>d__44>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<OpenResultAsync>d__44&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<OpenWindowIfAvailableAsync>d__53>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<OpenWindowIfAvailableAsync>d__53&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<ReconnectAsync>d__40>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<ReconnectAsync>d__40&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<ReturnToRoomAsync>d__46>(MiniCore.Demo.MiniBomber.MiniBomberClientFlowComponent.<ReturnToRoomAsync>d__46&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.MiniBomberClientStartupComponent.<InitializeAsync>d__7>(MiniCore.Demo.MiniBomber.MiniBomberClientStartupComponent.<InitializeAsync>d__7&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.MiniBomberRegisterHandler.<HandleAsync>d__0>(MiniCore.Demo.MiniBomber.MiniBomberRegisterHandler.<HandleAsync>d__0&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.MiniBomberServerRuntimeComponent.<InitializeAsync>d__34>(MiniCore.Demo.MiniBomber.MiniBomberServerRuntimeComponent.<InitializeAsync>d__34&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.MiniBomberServerRuntimeComponent.<RegisterAsync>d__35>(MiniCore.Demo.MiniBomber.MiniBomberServerRuntimeComponent.<RegisterAsync>d__35&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.MiniBomberServerStartupComponent.<InitializeAsync>d__0>(MiniCore.Demo.MiniBomber.MiniBomberServerStartupComponent.<InitializeAsync>d__0&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.MiniBomberStartupComponentBase.<LoadConfigurationAsync>d__16>(MiniCore.Demo.MiniBomber.MiniBomberStartupComponentBase.<LoadConfigurationAsync>d__16&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.NetworkDebugWindowPresenter.<RefreshLoopAsync>d__6>(MiniCore.Demo.MiniBomber.NetworkDebugWindowPresenter.<RefreshLoopAsync>d__6&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.RegisterWindowPresenter.<SubmitAsync>d__6>(MiniCore.Demo.MiniBomber.RegisterWindowPresenter.<SubmitAsync>d__6&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.RoomComponent.<CreateAsync>d__12>(MiniCore.Demo.MiniBomber.RoomComponent.<CreateAsync>d__12&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.RoomComponent.<JoinAsync>d__13>(MiniCore.Demo.MiniBomber.RoomComponent.<JoinAsync>d__13&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.RoomComponent.<LeaveAsync>d__14>(MiniCore.Demo.MiniBomber.RoomComponent.<LeaveAsync>d__14&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.RoomComponent.<SetReadyAsync>d__16>(MiniCore.Demo.MiniBomber.RoomComponent.<SetReadyAsync>d__16&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.RoomComponent.<UpdateSettingsAsync>d__15>(MiniCore.Demo.MiniBomber.RoomComponent.<UpdateSettingsAsync>d__15&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.RoomWindowPresenter.<ApplySettingsAsync>d__11>(MiniCore.Demo.MiniBomber.RoomWindowPresenter.<ApplySettingsAsync>d__11&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.RoomWindowPresenter.<LeaveAsync>d__17>(MiniCore.Demo.MiniBomber.RoomWindowPresenter.<LeaveAsync>d__17&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.RoomWindowPresenter.<StartMatchAsync>d__15>(MiniCore.Demo.MiniBomber.RoomWindowPresenter.<StartMatchAsync>d__15&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.RoomWindowPresenter.<ToggleReadyAsync>d__13>(MiniCore.Demo.MiniBomber.RoomWindowPresenter.<ToggleReadyAsync>d__13&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.HotUpdate.DedicatedClientSmokeTestRunner.<RunInternalAsync>d__28>(MiniCore.HotUpdate.DedicatedClientSmokeTestRunner.<RunInternalAsync>d__28&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.HotUpdate.GameStartup.<StartAsync>d__0>(MiniCore.HotUpdate.GameStartup.<StartAsync>d__0&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.HotUpdate.KcpTestWindowPresenter.<ConnectClientAsync>d__19>(MiniCore.HotUpdate.KcpTestWindowPresenter.<ConnectClientAsync>d__19&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.HotUpdate.KcpTestWindowPresenter.<DisconnectClientAsync>d__20>(MiniCore.HotUpdate.KcpTestWindowPresenter.<DisconnectClientAsync>d__20&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.HotUpdate.KcpTestWindowPresenter.<HandleClientDisconnectedAsync>d__22>(MiniCore.HotUpdate.KcpTestWindowPresenter.<HandleClientDisconnectedAsync>d__22&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.HotUpdate.KcpTestWindowPresenter.<SendNormalAsync>d__25>(MiniCore.HotUpdate.KcpTestWindowPresenter.<SendNormalAsync>d__25&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.HotUpdate.KcpTestWindowPresenter.<SendRpcAsync>d__26>(MiniCore.HotUpdate.KcpTestWindowPresenter.<SendRpcAsync>d__26&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.HotUpdate.KcpTestWindowPresenter.<StartServerAsync>d__17>(MiniCore.HotUpdate.KcpTestWindowPresenter.<StartServerAsync>d__17&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.HotUpdate.KcpTestWindowPresenter.<StopServerAsync>d__21>(MiniCore.HotUpdate.KcpTestWindowPresenter.<StopServerAsync>d__21&)
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
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.HotUpdate.NetworkBenchmarkRunner.<RunInternalAsync>d__101>(MiniCore.HotUpdate.NetworkBenchmarkRunner.<RunInternalAsync>d__101&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.HotUpdate.NetworkBenchmarkRunner.<RunMainThreadHitchBenchmarkAsync>d__108>(MiniCore.HotUpdate.NetworkBenchmarkRunner.<RunMainThreadHitchBenchmarkAsync>d__108&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.HotUpdate.NetworkBenchmarkRunner.<RunNormalBenchmarkAsync>d__106>(MiniCore.HotUpdate.NetworkBenchmarkRunner.<RunNormalBenchmarkAsync>d__106&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.HotUpdate.NetworkBenchmarkRunner.<RunRpcBenchmarkAsync>d__107>(MiniCore.HotUpdate.NetworkBenchmarkRunner.<RunRpcBenchmarkAsync>d__107&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.HotUpdate.NetworkBenchmarkRunner.<RunRpcCallAsync>d__110>(MiniCore.HotUpdate.NetworkBenchmarkRunner.<RunRpcCallAsync>d__110&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.HotUpdate.NetworkBenchmarkRunner.<RunTransportAsync>d__105>(MiniCore.HotUpdate.NetworkBenchmarkRunner.<RunTransportAsync>d__105&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.HotUpdate.NetworkBenchmarkRunner.<SendNormalMessagesForDurationAsync>d__109>(MiniCore.HotUpdate.NetworkBenchmarkRunner.<SendNormalMessagesForDurationAsync>d__109&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.HotUpdate.NetworkBenchmarkRunner.<WaitForConditionAsync>d__126>(MiniCore.HotUpdate.NetworkBenchmarkRunner.<WaitForConditionAsync>d__126&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.HotUpdate.NetworkBenchmarkRunner.<WaitForNormalMessagesAsync>d__123>(MiniCore.HotUpdate.NetworkBenchmarkRunner.<WaitForNormalMessagesAsync>d__123&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.HotUpdate.NetworkBenchmarkRunner.<WaitForQueueToDrainAsync>d__125>(MiniCore.HotUpdate.NetworkBenchmarkRunner.<WaitForQueueToDrainAsync>d__125&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.HotUpdate.NetworkBenchmarkRunner.<WaitForWarmupMessagesAsync>d__124>(MiniCore.HotUpdate.NetworkBenchmarkRunner.<WaitForWarmupMessagesAsync>d__124&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.HotUpdate.NetworkSmokeTestRunner.<RunInternalAsync>d__43>(MiniCore.HotUpdate.NetworkSmokeTestRunner.<RunInternalAsync>d__43&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.HotUpdate.NetworkSmokeTestRunner.<RunTransportAsync>d__47>(MiniCore.HotUpdate.NetworkSmokeTestRunner.<RunTransportAsync>d__47&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.HotUpdate.NetworkSmokeTestRunner.<WaitForConditionAsync>d__48>(MiniCore.HotUpdate.NetworkSmokeTestRunner.<WaitForConditionAsync>d__48&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.HotUpdate.TestHandler.<HandleAsync>d__0>(MiniCore.HotUpdate.TestHandler.<HandleAsync>d__0&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Serialization.ProtobufSaveServiceExtensions.<LoadProtobufAsync>d__1<object>>(MiniCore.Serialization.ProtobufSaveServiceExtensions.<LoadProtobufAsync>d__1<object>&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Service.AssetService.<PreloadAssetAsync>d__6<object>>(MiniCore.Service.AssetService.<PreloadAssetAsync>d__6<object>&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Service.UIService.<AcquireResourceLeaseAsync>d__44>(MiniCore.Service.UIService.<AcquireResourceLeaseAsync>d__44&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Service.UIService.<CloseNavigationAsync>d__25>(MiniCore.Service.UIService.<CloseNavigationAsync>d__25&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Service.UIService.<CreateViewAsync>d__43>(MiniCore.Service.UIService.<CreateViewAsync>d__43&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Service.UIService.<HandleDuplicateAsync>d__40>(MiniCore.Service.UIService.<HandleDuplicateAsync>d__40&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Service.UIService.<InitializeAsync>d__20>(MiniCore.Service.UIService.<InitializeAsync>d__20&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Service.UIService.<NavigateAsync>d__23<object>>(MiniCore.Service.UIService.<NavigateAsync>d__23<object>&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Service.UIService.<NavigateAsync>d__24>(MiniCore.Service.UIService.<NavigateAsync>d__24&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Service.UIService.<OpenCoreAsync>d__39>(MiniCore.Service.UIService.<OpenCoreAsync>d__39&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Service.UIService.<PrefetchAsync>d__26<object>>(MiniCore.Service.UIService.<PrefetchAsync>d__26<object>&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Service.UIService.<ShowAsync>d__29<object,object,object>>(MiniCore.Service.UIService.<ShowAsync>d__29<object,object,object>&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Service.YooAssetResourceService.<InstantiateAsync>d__2>(MiniCore.Service.YooAssetResourceService.<InstantiateAsync>d__2&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Service.YooAssetResourceService.<LoadAssetAsync>d__3<object>>(MiniCore.Service.YooAssetResourceService.<LoadAssetAsync>d__3<object>&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Service.YooAssetResourceService.<PreloadAssetAsync>d__4<object>>(MiniCore.Service.YooAssetResourceService.<PreloadAssetAsync>d__4<object>&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Service.YooAssetSceneService.<LoadSingleAsync>d__8>(MiniCore.Service.YooAssetSceneService.<LoadSingleAsync>d__8&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.UI.UIWindowSession.<CloseInternalAsync>d__41>(MiniCore.UI.UIWindowSession.<CloseInternalAsync>d__41&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.UI.UIWindowSession.<OpenInternalAsync>d__36>(MiniCore.UI.UIWindowSession.<OpenInternalAsync>d__36&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.UI.UIWindowSession.<WaitUntilActiveAsync>d__30>(MiniCore.UI.UIWindowSession.<WaitUntilActiveAsync>d__30&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.UI.UIWindowSession.<WaitUntilClosedAsync>d__31>(MiniCore.UI.UIWindowSession.<WaitUntilClosedAsync>d__31&)
		// object Newtonsoft.Json.JsonConvert.DeserializeObject<object>(string)
		// object Newtonsoft.Json.JsonConvert.DeserializeObject<object>(string,Newtonsoft.Json.JsonSerializerSettings)
		// object System.Activator.CreateInstance<object>()
		// UnityEngine.Vector2Int[] System.Array.Empty<UnityEngine.Vector2Int>()
		// object[] System.Array.Empty<object>()
		// int System.Array.IndexOf<int>(int[],int)
		// int System.Array.IndexOfImpl<int>(int[],int,int,int)
		// System.Void System.Array.Sort<long>(long[],int,int)
		// System.Void System.Array.Sort<long>(long[],int,int,System.Collections.Generic.IComparer<long>)
		// object System.Reflection.CustomAttributeExtensions.GetCustomAttribute<object>(System.Reflection.MemberInfo)
		// object& System.Runtime.CompilerServices.Unsafe.As<object,object>(object&)
		// System.Void* System.Runtime.CompilerServices.Unsafe.AsPointer<object>(object&)
		// System.Void* Unity.Collections.LowLevel.Unsafe.UnsafeUtility.AddressOf<UnityEngine.Vector2>(UnityEngine.Vector2&)
		// int Unity.Collections.LowLevel.Unsafe.UnsafeUtility.SizeOf<UnityEngine.Vector2>()
		// object UnityEngine.Component.GetComponentInChildren<object>()
		// object[] UnityEngine.Component.GetComponentsInChildren<object>()
		// object[] UnityEngine.Component.GetComponentsInChildren<object>(bool)
		// object UnityEngine.GameObject.AddComponent<object>()
		// object UnityEngine.GameObject.GetComponent<object>()
		// object[] UnityEngine.GameObject.GetComponentsInChildren<object>(bool)
		// UnityEngine.Vector2 UnityEngine.InputSystem.InputAction.ReadValue<UnityEngine.Vector2>()
		// UnityEngine.Vector2 UnityEngine.InputSystem.InputActionState.ReadValue<UnityEngine.Vector2>(int,int,bool)
		// object UnityEngine.Object.FindObjectOfType<object>()
		// object UnityEngine.Object.Instantiate<object>(object,UnityEngine.Transform)
		// object UnityEngine.Object.Instantiate<object>(object,UnityEngine.Transform,bool)
		// object UnityEngine.Resources.Load<object>(string)
		// YooAsset.AssetHandle YooAsset.ResourcePackage.LoadAssetAsync<object>(string,uint)
	}
}