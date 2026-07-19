using System.Collections.Generic;
public class AOTGenericReferences : UnityEngine.MonoBehaviour
{

	// {{ AOT assemblies
	public static readonly IReadOnlyList<string> PatchedAOTAssemblyList = new List<string>
	{
		"Google.Protobuf.dll",
		"MiniCore.Network.dll",
		"MiniCore.Runtime.dll",
		"MiniCore.Unity.dll",
		"Newtonsoft.Json.dll",
		"System.Core.dll",
		"UnityEngine.CoreModule.dll",
		"YooAsset.dll",
		"mscorlib.dll",
	};
	// }}

	// {{ constraint implement type
	// }} 

	// {{ AOT generic types
	// Google.Protobuf.IMessage<object>
	// Google.Protobuf.MessageParser.<>c__DisplayClass2_0<object>
	// Google.Protobuf.MessageParser<object>
	// MiniCore.Core.Global.<>c__16<object,object>
	// MiniCore.Core.GlobalServiceRegistry.<>c__DisplayClass2_0<object,object>
	// MiniCore.Model.AMHandler<object>
	// MiniCore.Model.APresenter<object>
	// MiniCore.Model.ARpcHandler<object,object>
	// MiniCore.Model.MonoSingleton<object>
	// MiniCore.Threading.IMTaskSource<System.ValueTuple<object,object>>
	// MiniCore.Threading.IMTaskSource<byte>
	// MiniCore.Threading.IMTaskSource<object>
	// MiniCore.Threading.MSharedTask<System.ValueTuple<object,object>>
	// MiniCore.Threading.MSharedTask<byte>
	// MiniCore.Threading.MSharedTask<object>
	// MiniCore.Threading.MSharedTaskAwaiter<System.ValueTuple<object,object>>
	// MiniCore.Threading.MSharedTaskAwaiter<byte>
	// MiniCore.Threading.MSharedTaskAwaiter<object>
	// MiniCore.Threading.MSharedTaskWaiter<System.ValueTuple<object,object>>
	// MiniCore.Threading.MSharedTaskWaiter<byte>
	// MiniCore.Threading.MSharedTaskWaiter<object>
	// MiniCore.Threading.MTask<System.ValueTuple<object,object>>
	// MiniCore.Threading.MTask<byte>
	// MiniCore.Threading.MTask<object>
	// MiniCore.Threading.MTaskAwaiter<System.ValueTuple<object,object>>
	// MiniCore.Threading.MTaskAwaiter<byte>
	// MiniCore.Threading.MTaskAwaiter<object>
	// MiniCore.Threading.MTaskCompletionSource<byte>
	// MiniCore.Threading.MTaskForgetObserver<System.ValueTuple<object,object>>
	// MiniCore.Threading.MTaskForgetObserver<byte>
	// MiniCore.Threading.MTaskForgetObserver<object>
	// MiniCore.Threading.MTaskMethodBuilder<System.ValueTuple<object,object>>
	// MiniCore.Threading.MTaskMethodBuilder<object>
	// MiniCore.Threading.MTaskPromise<System.ValueTuple<object,object>>
	// MiniCore.Threading.MTaskPromise<object>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Core.ExcelTool.<LoadCsvFileAsync>d__1<object>>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Core.GameObjectPool.<CreateObjectAsync>d__6>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Core.GameObjectPoolMgr.<GeneratePoolObject>d__4<object>>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.HotUpdate.DedicatedClientSmokeTestRunner.<RunInternalAsync>d__28>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.HotUpdate.GameStartup.<StartAsync>d__0>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.HotUpdate.GameStartup.<StartDedicatedServerAsync>d__5>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.HotUpdate.KcpTestWindowPresenter.<ConnectClientAsync>d__14>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.HotUpdate.KcpTestWindowPresenter.<DisconnectClientAsync>d__15>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.HotUpdate.KcpTestWindowPresenter.<HandleClientDisconnectedAsync>d__17>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.HotUpdate.KcpTestWindowPresenter.<SendNormalAsync>d__20>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.HotUpdate.KcpTestWindowPresenter.<SendRpcAsync>d__21>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.HotUpdate.KcpTestWindowPresenter.<StartServerAsync>d__12>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.HotUpdate.KcpTestWindowPresenter.<StopServerAsync>d__16>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.HotUpdate.MiniCoreStartup.<StartAsync>d__0>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.HotUpdate.MiniCoreStartup.<StartClientAsync>d__1>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectKcpAsync>d__46>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectTcpAsync>d__45>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectUdpAsync>d__47>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.HotUpdate.MultiProtocolTestPanel.<SendNormalAsync>d__49>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.HotUpdate.MultiProtocolTestPanel.<SendRpcAsync>d__50>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.HotUpdate.MultiProtocolTestPanel.<StartKcpServerAsync>d__40>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.HotUpdate.MultiProtocolTestPanel.<StartTcpServerAsync>d__39>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.HotUpdate.MultiProtocolTestPanel.<StartUdpServerAsync>d__41>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.HotUpdate.NetworkSmokeTestRunner.<RunInternalAsync>d__38>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.HotUpdate.NetworkSmokeTestRunner.<RunTransportAsync>d__42>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.HotUpdate.NetworkSmokeTestRunner.<WaitForConditionAsync>d__43>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.HotUpdate.TestHandler.<HandleAsync>d__0>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Service.AssetService.<PreloadAssetAsync>d__7<object>>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Service.UIService.<CloseAsync>d__9<object>>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Service.UIService.<CreateWindowInstanceAsync>d__12<object>>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Service.UIService.<OpenAsync>d__8<object,object>>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Service.UIService.<PreloadAsync>d__7<object,object>>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Service.YooAssetResourceService.<InstantiateAsync>d__2>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Service.YooAssetResourceService.<LoadAssetAsync>d__3<object>>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Service.YooAssetResourceService.<PreloadAssetAsync>d__4<object>>
	// System.Action<object>
	// System.Collections.Concurrent.ConcurrentQueue.<Enumerate>d__28<object>
	// System.Collections.Concurrent.ConcurrentQueue.Segment<object>
	// System.Collections.Concurrent.ConcurrentQueue<object>
	// System.Collections.Generic.ArraySortHelper<object>
	// System.Collections.Generic.Comparer<object>
	// System.Collections.Generic.Dictionary.Enumerator<object,int>
	// System.Collections.Generic.Dictionary.Enumerator<object,object>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<object,int>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<object,object>
	// System.Collections.Generic.Dictionary.KeyCollection<object,int>
	// System.Collections.Generic.Dictionary.KeyCollection<object,object>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<object,int>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<object,object>
	// System.Collections.Generic.Dictionary.ValueCollection<object,int>
	// System.Collections.Generic.Dictionary.ValueCollection<object,object>
	// System.Collections.Generic.Dictionary<object,int>
	// System.Collections.Generic.Dictionary<object,object>
	// System.Collections.Generic.EqualityComparer<int>
	// System.Collections.Generic.EqualityComparer<object>
	// System.Collections.Generic.HashSet.Enumerator<object>
	// System.Collections.Generic.HashSet<object>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<object,int>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.ICollection<object>
	// System.Collections.Generic.IComparer<object>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,int>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.IEnumerable<object>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<object,int>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.IEnumerator<object>
	// System.Collections.Generic.IEqualityComparer<object>
	// System.Collections.Generic.IList<object>
	// System.Collections.Generic.KeyValuePair<object,int>
	// System.Collections.Generic.KeyValuePair<object,object>
	// System.Collections.Generic.List.Enumerator<object>
	// System.Collections.Generic.List<object>
	// System.Collections.Generic.ObjectComparer<object>
	// System.Collections.Generic.ObjectEqualityComparer<int>
	// System.Collections.Generic.ObjectEqualityComparer<object>
	// System.Collections.Generic.Stack.Enumerator<object>
	// System.Collections.Generic.Stack<object>
	// System.Collections.ObjectModel.ReadOnlyCollection<object>
	// System.Comparison<object>
	// System.Func<MiniCore.Threading.MTask<byte>>
	// System.Func<MiniCore.Threading.MTask>
	// System.Func<byte>
	// System.Func<object,object>
	// System.Func<object>
	// System.Predicate<object>
	// System.ValueTuple<object,object>
	// }}

	public void RefMethods()
	{
		// System.Void MiniCore.Core.Global.BindAppService<object,object>()
		// object MiniCore.Core.Global.GetService<object>(object)
		// object MiniCore.Core.Global.Pin<object>()
		// object MiniCore.Core.Global.RegisterAppService<object,object>(MiniCore.Model.ComponentInitArgs)
		// System.Void MiniCore.Core.Global.ThrowIfDirectAppServiceAccess<object>()
		// object MiniCore.Core.GlobalRuntime.GetOrCreate<object>(object,MiniCore.Model.ComponentInitArgs)
		// object MiniCore.Core.GlobalRuntime.Pin<object>()
		// object MiniCore.Core.GlobalRuntime.Pin<object>(MiniCore.Model.ComponentInitArgs)
		// object MiniCore.Core.GlobalRuntime.PinInternal<object>(MiniCore.Model.ComponentInitArgs)
		// object MiniCore.Core.GlobalServiceRegistry.Get<object>(object)
		// System.Void MiniCore.Core.GlobalServiceRegistry.Register<object,object>(System.Func<object,object>)
		// System.Void MiniCore.Model.EventCenter.AddListener<object>(string,System.Action<object>)
		// System.Void MiniCore.Model.EventCenter.Broadcast<object>(string,object)
		// System.Void MiniCore.Model.EventCenter.RemoveListener<object>(string,System.Action<object>)
		// MiniCore.Threading.MTask<object> MiniCore.Service.INetworkService.CallAsync<object,object>(string,object)
		// MiniCore.Threading.MTask MiniCore.Service.INetworkService.SendAsync<object>(string,object)
		// MiniCore.Threading.MTask<object> MiniCore.Threading.MTask.FromResult<object>(object)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.HotUpdate.DedicatedClientSmokeTestRunner.<RunInternalAsync>d__28>(MiniCore.Threading.MTaskAwaiter&,MiniCore.HotUpdate.DedicatedClientSmokeTestRunner.<RunInternalAsync>d__28&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.HotUpdate.GameStartup.<StartAsync>d__0>(MiniCore.Threading.MTaskAwaiter&,MiniCore.HotUpdate.GameStartup.<StartAsync>d__0&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.HotUpdate.GameStartup.<StartDedicatedServerAsync>d__5>(MiniCore.Threading.MTaskAwaiter&,MiniCore.HotUpdate.GameStartup.<StartDedicatedServerAsync>d__5&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.HotUpdate.KcpTestWindowPresenter.<DisconnectClientAsync>d__15>(MiniCore.Threading.MTaskAwaiter&,MiniCore.HotUpdate.KcpTestWindowPresenter.<DisconnectClientAsync>d__15&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.HotUpdate.KcpTestWindowPresenter.<SendNormalAsync>d__20>(MiniCore.Threading.MTaskAwaiter&,MiniCore.HotUpdate.KcpTestWindowPresenter.<SendNormalAsync>d__20&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.HotUpdate.KcpTestWindowPresenter.<StartServerAsync>d__12>(MiniCore.Threading.MTaskAwaiter&,MiniCore.HotUpdate.KcpTestWindowPresenter.<StartServerAsync>d__12&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.HotUpdate.KcpTestWindowPresenter.<StopServerAsync>d__16>(MiniCore.Threading.MTaskAwaiter&,MiniCore.HotUpdate.KcpTestWindowPresenter.<StopServerAsync>d__16&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.HotUpdate.MiniCoreStartup.<StartAsync>d__0>(MiniCore.Threading.MTaskAwaiter&,MiniCore.HotUpdate.MiniCoreStartup.<StartAsync>d__0&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.HotUpdate.MiniCoreStartup.<StartClientAsync>d__1>(MiniCore.Threading.MTaskAwaiter&,MiniCore.HotUpdate.MiniCoreStartup.<StartClientAsync>d__1&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.HotUpdate.MultiProtocolTestPanel.<SendNormalAsync>d__49>(MiniCore.Threading.MTaskAwaiter&,MiniCore.HotUpdate.MultiProtocolTestPanel.<SendNormalAsync>d__49&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.HotUpdate.MultiProtocolTestPanel.<StartKcpServerAsync>d__40>(MiniCore.Threading.MTaskAwaiter&,MiniCore.HotUpdate.MultiProtocolTestPanel.<StartKcpServerAsync>d__40&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.HotUpdate.MultiProtocolTestPanel.<StartTcpServerAsync>d__39>(MiniCore.Threading.MTaskAwaiter&,MiniCore.HotUpdate.MultiProtocolTestPanel.<StartTcpServerAsync>d__39&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.HotUpdate.MultiProtocolTestPanel.<StartUdpServerAsync>d__41>(MiniCore.Threading.MTaskAwaiter&,MiniCore.HotUpdate.MultiProtocolTestPanel.<StartUdpServerAsync>d__41&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.HotUpdate.NetworkSmokeTestRunner.<RunInternalAsync>d__38>(MiniCore.Threading.MTaskAwaiter&,MiniCore.HotUpdate.NetworkSmokeTestRunner.<RunInternalAsync>d__38&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.HotUpdate.NetworkSmokeTestRunner.<RunTransportAsync>d__42>(MiniCore.Threading.MTaskAwaiter&,MiniCore.HotUpdate.NetworkSmokeTestRunner.<RunTransportAsync>d__42&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.HotUpdate.TestHandler.<HandleAsync>d__0>(MiniCore.Threading.MTaskAwaiter&,MiniCore.HotUpdate.TestHandler.<HandleAsync>d__0&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.Service.UIService.<CloseAsync>d__9<object>>(MiniCore.Threading.MTaskAwaiter&,MiniCore.Service.UIService.<CloseAsync>d__9<object>&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<byte>,MiniCore.HotUpdate.DedicatedClientSmokeTestRunner.<RunInternalAsync>d__28>(MiniCore.Threading.MTaskAwaiter<byte>&,MiniCore.HotUpdate.DedicatedClientSmokeTestRunner.<RunInternalAsync>d__28&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<byte>,MiniCore.HotUpdate.KcpTestWindowPresenter.<ConnectClientAsync>d__14>(MiniCore.Threading.MTaskAwaiter<byte>&,MiniCore.HotUpdate.KcpTestWindowPresenter.<ConnectClientAsync>d__14&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<byte>,MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectKcpAsync>d__46>(MiniCore.Threading.MTaskAwaiter<byte>&,MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectKcpAsync>d__46&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<byte>,MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectTcpAsync>d__45>(MiniCore.Threading.MTaskAwaiter<byte>&,MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectTcpAsync>d__45&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<byte>,MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectUdpAsync>d__47>(MiniCore.Threading.MTaskAwaiter<byte>&,MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectUdpAsync>d__47&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<byte>,MiniCore.HotUpdate.NetworkSmokeTestRunner.<RunTransportAsync>d__42>(MiniCore.Threading.MTaskAwaiter<byte>&,MiniCore.HotUpdate.NetworkSmokeTestRunner.<RunTransportAsync>d__42&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.HotUpdate.DedicatedClientSmokeTestRunner.<RunInternalAsync>d__28>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.HotUpdate.DedicatedClientSmokeTestRunner.<RunInternalAsync>d__28&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.HotUpdate.KcpTestWindowPresenter.<SendRpcAsync>d__21>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.HotUpdate.KcpTestWindowPresenter.<SendRpcAsync>d__21&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.HotUpdate.MultiProtocolTestPanel.<SendRpcAsync>d__50>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.HotUpdate.MultiProtocolTestPanel.<SendRpcAsync>d__50&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.HotUpdate.NetworkSmokeTestRunner.<RunTransportAsync>d__42>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.HotUpdate.NetworkSmokeTestRunner.<RunTransportAsync>d__42&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Service.UIService.<PreloadAsync>d__7<object,object>>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Service.UIService.<PreloadAsync>d__7<object,object>&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskSwitchAwaiter,MiniCore.HotUpdate.KcpTestWindowPresenter.<HandleClientDisconnectedAsync>d__17>(MiniCore.Threading.MTaskSwitchAwaiter&,MiniCore.HotUpdate.KcpTestWindowPresenter.<HandleClientDisconnectedAsync>d__17&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskYieldAwaiter,MiniCore.HotUpdate.NetworkSmokeTestRunner.<WaitForConditionAsync>d__43>(MiniCore.Threading.MTaskYieldAwaiter&,MiniCore.HotUpdate.NetworkSmokeTestRunner.<WaitForConditionAsync>d__43&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<System.ValueTuple<object,object>>.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.Service.UIService.<OpenAsync>d__8<object,object>>(MiniCore.Threading.MTaskAwaiter&,MiniCore.Service.UIService.<OpenAsync>d__8<object,object>&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<System.ValueTuple<object,object>>.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Service.UIService.<OpenAsync>d__8<object,object>>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Service.UIService.<OpenAsync>d__8<object,object>&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.Service.YooAssetResourceService.<LoadAssetAsync>d__3<object>>(MiniCore.Threading.MTaskAwaiter&,MiniCore.Service.YooAssetResourceService.<LoadAssetAsync>d__3<object>&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.Service.YooAssetResourceService.<PreloadAssetAsync>d__4<object>>(MiniCore.Threading.MTaskAwaiter&,MiniCore.Service.YooAssetResourceService.<PreloadAssetAsync>d__4<object>&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Core.ExcelTool.<LoadCsvFileAsync>d__1<object>>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Core.ExcelTool.<LoadCsvFileAsync>d__1<object>&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Core.GameObjectPool.<CreateObjectAsync>d__6>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Core.GameObjectPool.<CreateObjectAsync>d__6&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Core.GameObjectPoolMgr.<GeneratePoolObject>d__4<object>>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Core.GameObjectPoolMgr.<GeneratePoolObject>d__4<object>&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Service.AssetService.<PreloadAssetAsync>d__7<object>>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Service.AssetService.<PreloadAssetAsync>d__7<object>&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Service.UIService.<CreateWindowInstanceAsync>d__12<object>>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Service.UIService.<CreateWindowInstanceAsync>d__12<object>&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Service.YooAssetResourceService.<InstantiateAsync>d__2>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Service.YooAssetResourceService.<InstantiateAsync>d__2&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.HotUpdate.DedicatedClientSmokeTestRunner.<RunInternalAsync>d__28>(MiniCore.HotUpdate.DedicatedClientSmokeTestRunner.<RunInternalAsync>d__28&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.HotUpdate.GameStartup.<StartAsync>d__0>(MiniCore.HotUpdate.GameStartup.<StartAsync>d__0&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.HotUpdate.GameStartup.<StartDedicatedServerAsync>d__5>(MiniCore.HotUpdate.GameStartup.<StartDedicatedServerAsync>d__5&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.HotUpdate.KcpTestWindowPresenter.<ConnectClientAsync>d__14>(MiniCore.HotUpdate.KcpTestWindowPresenter.<ConnectClientAsync>d__14&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.HotUpdate.KcpTestWindowPresenter.<DisconnectClientAsync>d__15>(MiniCore.HotUpdate.KcpTestWindowPresenter.<DisconnectClientAsync>d__15&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.HotUpdate.KcpTestWindowPresenter.<HandleClientDisconnectedAsync>d__17>(MiniCore.HotUpdate.KcpTestWindowPresenter.<HandleClientDisconnectedAsync>d__17&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.HotUpdate.KcpTestWindowPresenter.<SendNormalAsync>d__20>(MiniCore.HotUpdate.KcpTestWindowPresenter.<SendNormalAsync>d__20&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.HotUpdate.KcpTestWindowPresenter.<SendRpcAsync>d__21>(MiniCore.HotUpdate.KcpTestWindowPresenter.<SendRpcAsync>d__21&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.HotUpdate.KcpTestWindowPresenter.<StartServerAsync>d__12>(MiniCore.HotUpdate.KcpTestWindowPresenter.<StartServerAsync>d__12&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.HotUpdate.KcpTestWindowPresenter.<StopServerAsync>d__16>(MiniCore.HotUpdate.KcpTestWindowPresenter.<StopServerAsync>d__16&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.HotUpdate.MiniCoreStartup.<StartAsync>d__0>(MiniCore.HotUpdate.MiniCoreStartup.<StartAsync>d__0&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.HotUpdate.MiniCoreStartup.<StartClientAsync>d__1>(MiniCore.HotUpdate.MiniCoreStartup.<StartClientAsync>d__1&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectKcpAsync>d__46>(MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectKcpAsync>d__46&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectTcpAsync>d__45>(MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectTcpAsync>d__45&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectUdpAsync>d__47>(MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectUdpAsync>d__47&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.HotUpdate.MultiProtocolTestPanel.<SendNormalAsync>d__49>(MiniCore.HotUpdate.MultiProtocolTestPanel.<SendNormalAsync>d__49&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.HotUpdate.MultiProtocolTestPanel.<SendRpcAsync>d__50>(MiniCore.HotUpdate.MultiProtocolTestPanel.<SendRpcAsync>d__50&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.HotUpdate.MultiProtocolTestPanel.<StartKcpServerAsync>d__40>(MiniCore.HotUpdate.MultiProtocolTestPanel.<StartKcpServerAsync>d__40&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.HotUpdate.MultiProtocolTestPanel.<StartTcpServerAsync>d__39>(MiniCore.HotUpdate.MultiProtocolTestPanel.<StartTcpServerAsync>d__39&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.HotUpdate.MultiProtocolTestPanel.<StartUdpServerAsync>d__41>(MiniCore.HotUpdate.MultiProtocolTestPanel.<StartUdpServerAsync>d__41&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.HotUpdate.NetworkSmokeTestRunner.<RunInternalAsync>d__38>(MiniCore.HotUpdate.NetworkSmokeTestRunner.<RunInternalAsync>d__38&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.HotUpdate.NetworkSmokeTestRunner.<RunTransportAsync>d__42>(MiniCore.HotUpdate.NetworkSmokeTestRunner.<RunTransportAsync>d__42&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.HotUpdate.NetworkSmokeTestRunner.<WaitForConditionAsync>d__43>(MiniCore.HotUpdate.NetworkSmokeTestRunner.<WaitForConditionAsync>d__43&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.HotUpdate.TestHandler.<HandleAsync>d__0>(MiniCore.HotUpdate.TestHandler.<HandleAsync>d__0&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Service.UIService.<CloseAsync>d__9<object>>(MiniCore.Service.UIService.<CloseAsync>d__9<object>&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Service.UIService.<PreloadAsync>d__7<object,object>>(MiniCore.Service.UIService.<PreloadAsync>d__7<object,object>&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<System.ValueTuple<object,object>>.Start<MiniCore.Service.UIService.<OpenAsync>d__8<object,object>>(MiniCore.Service.UIService.<OpenAsync>d__8<object,object>&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.Start<MiniCore.Core.ExcelTool.<LoadCsvFileAsync>d__1<object>>(MiniCore.Core.ExcelTool.<LoadCsvFileAsync>d__1<object>&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.Start<MiniCore.Core.GameObjectPool.<CreateObjectAsync>d__6>(MiniCore.Core.GameObjectPool.<CreateObjectAsync>d__6&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.Start<MiniCore.Core.GameObjectPoolMgr.<GeneratePoolObject>d__4<object>>(MiniCore.Core.GameObjectPoolMgr.<GeneratePoolObject>d__4<object>&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.Start<MiniCore.Service.AssetService.<PreloadAssetAsync>d__7<object>>(MiniCore.Service.AssetService.<PreloadAssetAsync>d__7<object>&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.Start<MiniCore.Service.UIService.<CreateWindowInstanceAsync>d__12<object>>(MiniCore.Service.UIService.<CreateWindowInstanceAsync>d__12<object>&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.Start<MiniCore.Service.YooAssetResourceService.<InstantiateAsync>d__2>(MiniCore.Service.YooAssetResourceService.<InstantiateAsync>d__2&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.Start<MiniCore.Service.YooAssetResourceService.<LoadAssetAsync>d__3<object>>(MiniCore.Service.YooAssetResourceService.<LoadAssetAsync>d__3<object>&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.Start<MiniCore.Service.YooAssetResourceService.<PreloadAssetAsync>d__4<object>>(MiniCore.Service.YooAssetResourceService.<PreloadAssetAsync>d__4<object>&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Core.ExcelTool.<LoadCsvFileAsync>d__1<object>>(MiniCore.Core.ExcelTool.<LoadCsvFileAsync>d__1<object>&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Core.GameObjectPool.<CreateObjectAsync>d__6>(MiniCore.Core.GameObjectPool.<CreateObjectAsync>d__6&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Core.GameObjectPoolMgr.<GeneratePoolObject>d__4<object>>(MiniCore.Core.GameObjectPoolMgr.<GeneratePoolObject>d__4<object>&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.HotUpdate.DedicatedClientSmokeTestRunner.<RunInternalAsync>d__28>(MiniCore.HotUpdate.DedicatedClientSmokeTestRunner.<RunInternalAsync>d__28&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.HotUpdate.GameStartup.<StartAsync>d__0>(MiniCore.HotUpdate.GameStartup.<StartAsync>d__0&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.HotUpdate.GameStartup.<StartDedicatedServerAsync>d__5>(MiniCore.HotUpdate.GameStartup.<StartDedicatedServerAsync>d__5&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.HotUpdate.KcpTestWindowPresenter.<ConnectClientAsync>d__14>(MiniCore.HotUpdate.KcpTestWindowPresenter.<ConnectClientAsync>d__14&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.HotUpdate.KcpTestWindowPresenter.<DisconnectClientAsync>d__15>(MiniCore.HotUpdate.KcpTestWindowPresenter.<DisconnectClientAsync>d__15&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.HotUpdate.KcpTestWindowPresenter.<HandleClientDisconnectedAsync>d__17>(MiniCore.HotUpdate.KcpTestWindowPresenter.<HandleClientDisconnectedAsync>d__17&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.HotUpdate.KcpTestWindowPresenter.<SendNormalAsync>d__20>(MiniCore.HotUpdate.KcpTestWindowPresenter.<SendNormalAsync>d__20&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.HotUpdate.KcpTestWindowPresenter.<SendRpcAsync>d__21>(MiniCore.HotUpdate.KcpTestWindowPresenter.<SendRpcAsync>d__21&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.HotUpdate.KcpTestWindowPresenter.<StartServerAsync>d__12>(MiniCore.HotUpdate.KcpTestWindowPresenter.<StartServerAsync>d__12&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.HotUpdate.KcpTestWindowPresenter.<StopServerAsync>d__16>(MiniCore.HotUpdate.KcpTestWindowPresenter.<StopServerAsync>d__16&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.HotUpdate.MiniCoreStartup.<StartAsync>d__0>(MiniCore.HotUpdate.MiniCoreStartup.<StartAsync>d__0&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.HotUpdate.MiniCoreStartup.<StartClientAsync>d__1>(MiniCore.HotUpdate.MiniCoreStartup.<StartClientAsync>d__1&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectKcpAsync>d__46>(MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectKcpAsync>d__46&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectTcpAsync>d__45>(MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectTcpAsync>d__45&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectUdpAsync>d__47>(MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectUdpAsync>d__47&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.HotUpdate.MultiProtocolTestPanel.<SendNormalAsync>d__49>(MiniCore.HotUpdate.MultiProtocolTestPanel.<SendNormalAsync>d__49&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.HotUpdate.MultiProtocolTestPanel.<SendRpcAsync>d__50>(MiniCore.HotUpdate.MultiProtocolTestPanel.<SendRpcAsync>d__50&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.HotUpdate.MultiProtocolTestPanel.<StartKcpServerAsync>d__40>(MiniCore.HotUpdate.MultiProtocolTestPanel.<StartKcpServerAsync>d__40&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.HotUpdate.MultiProtocolTestPanel.<StartTcpServerAsync>d__39>(MiniCore.HotUpdate.MultiProtocolTestPanel.<StartTcpServerAsync>d__39&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.HotUpdate.MultiProtocolTestPanel.<StartUdpServerAsync>d__41>(MiniCore.HotUpdate.MultiProtocolTestPanel.<StartUdpServerAsync>d__41&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.HotUpdate.NetworkSmokeTestRunner.<RunInternalAsync>d__38>(MiniCore.HotUpdate.NetworkSmokeTestRunner.<RunInternalAsync>d__38&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.HotUpdate.NetworkSmokeTestRunner.<RunTransportAsync>d__42>(MiniCore.HotUpdate.NetworkSmokeTestRunner.<RunTransportAsync>d__42&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.HotUpdate.NetworkSmokeTestRunner.<WaitForConditionAsync>d__43>(MiniCore.HotUpdate.NetworkSmokeTestRunner.<WaitForConditionAsync>d__43&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.HotUpdate.TestHandler.<HandleAsync>d__0>(MiniCore.HotUpdate.TestHandler.<HandleAsync>d__0&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Service.AssetService.<PreloadAssetAsync>d__7<object>>(MiniCore.Service.AssetService.<PreloadAssetAsync>d__7<object>&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Service.UIService.<CloseAsync>d__9<object>>(MiniCore.Service.UIService.<CloseAsync>d__9<object>&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Service.UIService.<CreateWindowInstanceAsync>d__12<object>>(MiniCore.Service.UIService.<CreateWindowInstanceAsync>d__12<object>&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Service.UIService.<OpenAsync>d__8<object,object>>(MiniCore.Service.UIService.<OpenAsync>d__8<object,object>&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Service.UIService.<PreloadAsync>d__7<object,object>>(MiniCore.Service.UIService.<PreloadAsync>d__7<object,object>&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Service.YooAssetResourceService.<InstantiateAsync>d__2>(MiniCore.Service.YooAssetResourceService.<InstantiateAsync>d__2&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Service.YooAssetResourceService.<LoadAssetAsync>d__3<object>>(MiniCore.Service.YooAssetResourceService.<LoadAssetAsync>d__3<object>&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Service.YooAssetResourceService.<PreloadAssetAsync>d__4<object>>(MiniCore.Service.YooAssetResourceService.<PreloadAssetAsync>d__4<object>&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Core.ExcelTool.<LoadCsvFileAsync>d__1<object>>(MiniCore.Core.ExcelTool.<LoadCsvFileAsync>d__1<object>&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Core.GameObjectPool.<CreateObjectAsync>d__6>(MiniCore.Core.GameObjectPool.<CreateObjectAsync>d__6&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Core.GameObjectPoolMgr.<GeneratePoolObject>d__4<object>>(MiniCore.Core.GameObjectPoolMgr.<GeneratePoolObject>d__4<object>&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.HotUpdate.DedicatedClientSmokeTestRunner.<RunInternalAsync>d__28>(MiniCore.HotUpdate.DedicatedClientSmokeTestRunner.<RunInternalAsync>d__28&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.HotUpdate.GameStartup.<StartAsync>d__0>(MiniCore.HotUpdate.GameStartup.<StartAsync>d__0&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.HotUpdate.GameStartup.<StartDedicatedServerAsync>d__5>(MiniCore.HotUpdate.GameStartup.<StartDedicatedServerAsync>d__5&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.HotUpdate.KcpTestWindowPresenter.<ConnectClientAsync>d__14>(MiniCore.HotUpdate.KcpTestWindowPresenter.<ConnectClientAsync>d__14&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.HotUpdate.KcpTestWindowPresenter.<DisconnectClientAsync>d__15>(MiniCore.HotUpdate.KcpTestWindowPresenter.<DisconnectClientAsync>d__15&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.HotUpdate.KcpTestWindowPresenter.<HandleClientDisconnectedAsync>d__17>(MiniCore.HotUpdate.KcpTestWindowPresenter.<HandleClientDisconnectedAsync>d__17&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.HotUpdate.KcpTestWindowPresenter.<SendNormalAsync>d__20>(MiniCore.HotUpdate.KcpTestWindowPresenter.<SendNormalAsync>d__20&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.HotUpdate.KcpTestWindowPresenter.<SendRpcAsync>d__21>(MiniCore.HotUpdate.KcpTestWindowPresenter.<SendRpcAsync>d__21&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.HotUpdate.KcpTestWindowPresenter.<StartServerAsync>d__12>(MiniCore.HotUpdate.KcpTestWindowPresenter.<StartServerAsync>d__12&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.HotUpdate.KcpTestWindowPresenter.<StopServerAsync>d__16>(MiniCore.HotUpdate.KcpTestWindowPresenter.<StopServerAsync>d__16&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.HotUpdate.MiniCoreStartup.<StartAsync>d__0>(MiniCore.HotUpdate.MiniCoreStartup.<StartAsync>d__0&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.HotUpdate.MiniCoreStartup.<StartClientAsync>d__1>(MiniCore.HotUpdate.MiniCoreStartup.<StartClientAsync>d__1&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectKcpAsync>d__46>(MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectKcpAsync>d__46&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectTcpAsync>d__45>(MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectTcpAsync>d__45&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectUdpAsync>d__47>(MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectUdpAsync>d__47&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.HotUpdate.MultiProtocolTestPanel.<SendNormalAsync>d__49>(MiniCore.HotUpdate.MultiProtocolTestPanel.<SendNormalAsync>d__49&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.HotUpdate.MultiProtocolTestPanel.<SendRpcAsync>d__50>(MiniCore.HotUpdate.MultiProtocolTestPanel.<SendRpcAsync>d__50&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.HotUpdate.MultiProtocolTestPanel.<StartKcpServerAsync>d__40>(MiniCore.HotUpdate.MultiProtocolTestPanel.<StartKcpServerAsync>d__40&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.HotUpdate.MultiProtocolTestPanel.<StartTcpServerAsync>d__39>(MiniCore.HotUpdate.MultiProtocolTestPanel.<StartTcpServerAsync>d__39&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.HotUpdate.MultiProtocolTestPanel.<StartUdpServerAsync>d__41>(MiniCore.HotUpdate.MultiProtocolTestPanel.<StartUdpServerAsync>d__41&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.HotUpdate.NetworkSmokeTestRunner.<RunInternalAsync>d__38>(MiniCore.HotUpdate.NetworkSmokeTestRunner.<RunInternalAsync>d__38&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.HotUpdate.NetworkSmokeTestRunner.<RunTransportAsync>d__42>(MiniCore.HotUpdate.NetworkSmokeTestRunner.<RunTransportAsync>d__42&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.HotUpdate.NetworkSmokeTestRunner.<WaitForConditionAsync>d__43>(MiniCore.HotUpdate.NetworkSmokeTestRunner.<WaitForConditionAsync>d__43&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.HotUpdate.TestHandler.<HandleAsync>d__0>(MiniCore.HotUpdate.TestHandler.<HandleAsync>d__0&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Service.AssetService.<PreloadAssetAsync>d__7<object>>(MiniCore.Service.AssetService.<PreloadAssetAsync>d__7<object>&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Service.UIService.<CloseAsync>d__9<object>>(MiniCore.Service.UIService.<CloseAsync>d__9<object>&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Service.UIService.<CreateWindowInstanceAsync>d__12<object>>(MiniCore.Service.UIService.<CreateWindowInstanceAsync>d__12<object>&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Service.UIService.<OpenAsync>d__8<object,object>>(MiniCore.Service.UIService.<OpenAsync>d__8<object,object>&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Service.UIService.<PreloadAsync>d__7<object,object>>(MiniCore.Service.UIService.<PreloadAsync>d__7<object,object>&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Service.YooAssetResourceService.<InstantiateAsync>d__2>(MiniCore.Service.YooAssetResourceService.<InstantiateAsync>d__2&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Service.YooAssetResourceService.<LoadAssetAsync>d__3<object>>(MiniCore.Service.YooAssetResourceService.<LoadAssetAsync>d__3<object>&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Service.YooAssetResourceService.<PreloadAssetAsync>d__4<object>>(MiniCore.Service.YooAssetResourceService.<PreloadAssetAsync>d__4<object>&)
		// object Newtonsoft.Json.JsonConvert.DeserializeObject<object>(string)
		// object Newtonsoft.Json.JsonConvert.DeserializeObject<object>(string,Newtonsoft.Json.JsonSerializerSettings)
		// object System.Activator.CreateInstance<object>()
		// object[] System.Array.Empty<object>()
		// object System.Reflection.CustomAttributeExtensions.GetCustomAttribute<object>(System.Reflection.MemberInfo)
		// object& System.Runtime.CompilerServices.Unsafe.As<object,object>(object&)
		// System.Void* System.Runtime.CompilerServices.Unsafe.AsPointer<object>(object&)
		// object[] UnityEngine.Component.GetComponentsInChildren<object>()
		// object[] UnityEngine.Component.GetComponentsInChildren<object>(bool)
		// object UnityEngine.GameObject.AddComponent<object>()
		// object UnityEngine.GameObject.GetComponent<object>()
		// object[] UnityEngine.GameObject.GetComponentsInChildren<object>(bool)
		// object UnityEngine.Object.FindObjectOfType<object>()
		// object UnityEngine.Resources.Load<object>(string)
		// YooAsset.AssetHandle YooAsset.ResourcePackage.LoadAssetAsync<object>(string,uint)
	}
}