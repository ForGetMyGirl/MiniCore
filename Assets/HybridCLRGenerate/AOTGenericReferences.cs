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
		"UniTask.dll",
		"UnityEngine.CoreModule.dll",
		"YooAsset.dll",
		"mscorlib.dll",
	};
	// }}

	// {{ constraint implement type
	// }} 

	// {{ AOT generic types
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask.<>c<MiniCore.Core.ExcelTool.<LoadCsvFileAsync>d__1<object>,object>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask.<>c<MiniCore.Core.GameObjectPool.<CreateObjectAsync>d__6,object>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask.<>c<MiniCore.Core.GameObjectPoolMgr.<GeneratePoolObject>d__4<object>,object>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask.<>c<MiniCore.HotUpdate.NetworkSmokeTestRunner.<RunTransportAsync>d__42>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask.<>c<MiniCore.HotUpdate.NetworkSmokeTestRunner.<WaitForConditionAsync>d__43>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask.<>c<MiniCore.HotUpdate.TestHandler.<HandleAsync>d__0>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask.<>c<MiniCore.Service.AssetService.<PreloadAssetAsync>d__7<object>,object>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask.<>c<MiniCore.Service.UIService.<CloseAsync>d__9<object>>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask.<>c<MiniCore.Service.UIService.<CreateWindowInstanceAsync>d__12<object>,object>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask.<>c<MiniCore.Service.UIService.<OpenAsync>d__8<object,object>,System.ValueTuple<object,object>>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask.<>c<MiniCore.Service.UIService.<PreloadAsync>d__7<object,object>>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask.<>c<MiniCore.Service.YooAssetResourceService.<InstantiateAsync>d__2,object>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask.<>c<MiniCore.Service.YooAssetResourceService.<LoadAssetAsync>d__3<object>,object>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask.<>c<MiniCore.Service.YooAssetResourceService.<PreloadAssetAsync>d__4<object>,object>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask<MiniCore.Core.ExcelTool.<LoadCsvFileAsync>d__1<object>,object>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask<MiniCore.Core.GameObjectPool.<CreateObjectAsync>d__6,object>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask<MiniCore.Core.GameObjectPoolMgr.<GeneratePoolObject>d__4<object>,object>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask<MiniCore.HotUpdate.NetworkSmokeTestRunner.<RunTransportAsync>d__42>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask<MiniCore.HotUpdate.NetworkSmokeTestRunner.<WaitForConditionAsync>d__43>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask<MiniCore.HotUpdate.TestHandler.<HandleAsync>d__0>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask<MiniCore.Service.AssetService.<PreloadAssetAsync>d__7<object>,object>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask<MiniCore.Service.UIService.<CloseAsync>d__9<object>>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask<MiniCore.Service.UIService.<CreateWindowInstanceAsync>d__12<object>,object>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask<MiniCore.Service.UIService.<OpenAsync>d__8<object,object>,System.ValueTuple<object,object>>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask<MiniCore.Service.UIService.<PreloadAsync>d__7<object,object>>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask<MiniCore.Service.YooAssetResourceService.<InstantiateAsync>d__2,object>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask<MiniCore.Service.YooAssetResourceService.<LoadAssetAsync>d__3<object>,object>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTask<MiniCore.Service.YooAssetResourceService.<PreloadAssetAsync>d__4<object>,object>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<System.ValueTuple<object,object>>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<object>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoid.<>c<MiniCore.HotUpdate.DedicatedClientSmokeTestRunner.<RunInternalAsync>d__28>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoid.<>c<MiniCore.HotUpdate.KcpTestWindowPresenter.<<ConnectClientAsync>b__14_1>d>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoid.<>c<MiniCore.HotUpdate.KcpTestWindowPresenter.<ConnectClientAsync>d__14>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoid.<>c<MiniCore.HotUpdate.KcpTestWindowPresenter.<DisconnectClientAsync>d__15>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoid.<>c<MiniCore.HotUpdate.KcpTestWindowPresenter.<SendNormalAsync>d__19>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoid.<>c<MiniCore.HotUpdate.KcpTestWindowPresenter.<SendRpcAsync>d__20>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoid.<>c<MiniCore.HotUpdate.KcpTestWindowPresenter.<StartServerAsync>d__12>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoid.<>c<MiniCore.HotUpdate.KcpTestWindowPresenter.<StopServerAsync>d__16>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoid.<>c<MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectKcpAsync>d__46>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoid.<>c<MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectTcpAsync>d__45>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoid.<>c<MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectUdpAsync>d__47>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoid.<>c<MiniCore.HotUpdate.MultiProtocolTestPanel.<SendNormalAsync>d__49>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoid.<>c<MiniCore.HotUpdate.MultiProtocolTestPanel.<SendRpcAsync>d__50>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoid.<>c<MiniCore.HotUpdate.MultiProtocolTestPanel.<StartKcpServerAsync>d__40>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoid.<>c<MiniCore.HotUpdate.MultiProtocolTestPanel.<StartTcpServerAsync>d__39>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoid.<>c<MiniCore.HotUpdate.MultiProtocolTestPanel.<StartUdpServerAsync>d__41>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoid.<>c<MiniCore.HotUpdate.NetworkSmokeTestRunner.<RunInternalAsync>d__38>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoid<MiniCore.HotUpdate.DedicatedClientSmokeTestRunner.<RunInternalAsync>d__28>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoid<MiniCore.HotUpdate.KcpTestWindowPresenter.<<ConnectClientAsync>b__14_1>d>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoid<MiniCore.HotUpdate.KcpTestWindowPresenter.<ConnectClientAsync>d__14>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoid<MiniCore.HotUpdate.KcpTestWindowPresenter.<DisconnectClientAsync>d__15>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoid<MiniCore.HotUpdate.KcpTestWindowPresenter.<SendNormalAsync>d__19>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoid<MiniCore.HotUpdate.KcpTestWindowPresenter.<SendRpcAsync>d__20>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoid<MiniCore.HotUpdate.KcpTestWindowPresenter.<StartServerAsync>d__12>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoid<MiniCore.HotUpdate.KcpTestWindowPresenter.<StopServerAsync>d__16>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoid<MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectKcpAsync>d__46>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoid<MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectTcpAsync>d__45>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoid<MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectUdpAsync>d__47>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoid<MiniCore.HotUpdate.MultiProtocolTestPanel.<SendNormalAsync>d__49>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoid<MiniCore.HotUpdate.MultiProtocolTestPanel.<SendRpcAsync>d__50>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoid<MiniCore.HotUpdate.MultiProtocolTestPanel.<StartKcpServerAsync>d__40>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoid<MiniCore.HotUpdate.MultiProtocolTestPanel.<StartTcpServerAsync>d__39>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoid<MiniCore.HotUpdate.MultiProtocolTestPanel.<StartUdpServerAsync>d__41>
	// Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoid<MiniCore.HotUpdate.NetworkSmokeTestRunner.<RunInternalAsync>d__38>
	// Cysharp.Threading.Tasks.CompilerServices.IStateMachineRunnerPromise<System.ValueTuple<object,object>>
	// Cysharp.Threading.Tasks.CompilerServices.IStateMachineRunnerPromise<object>
	// Cysharp.Threading.Tasks.ITaskPoolNode<object>
	// Cysharp.Threading.Tasks.IUniTaskSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>>>>>
	// Cysharp.Threading.Tasks.IUniTaskSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>>>
	// Cysharp.Threading.Tasks.IUniTaskSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>>>>
	// Cysharp.Threading.Tasks.IUniTaskSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>>
	// Cysharp.Threading.Tasks.IUniTaskSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<object,object>>>>>>>>>
	// Cysharp.Threading.Tasks.IUniTaskSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>>>
	// Cysharp.Threading.Tasks.IUniTaskSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>
	// Cysharp.Threading.Tasks.IUniTaskSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<object,object>>>>>>>>
	// Cysharp.Threading.Tasks.IUniTaskSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>>
	// Cysharp.Threading.Tasks.IUniTaskSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>
	// Cysharp.Threading.Tasks.IUniTaskSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<object,object>>>>>>>
	// Cysharp.Threading.Tasks.IUniTaskSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>
	// Cysharp.Threading.Tasks.IUniTaskSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>
	// Cysharp.Threading.Tasks.IUniTaskSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<object,object>>>>>>
	// Cysharp.Threading.Tasks.IUniTaskSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>
	// Cysharp.Threading.Tasks.IUniTaskSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>
	// Cysharp.Threading.Tasks.IUniTaskSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<object,object>>>>>
	// Cysharp.Threading.Tasks.IUniTaskSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>
	// Cysharp.Threading.Tasks.IUniTaskSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>
	// Cysharp.Threading.Tasks.IUniTaskSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<object,object>>>>
	// Cysharp.Threading.Tasks.IUniTaskSource<System.ValueTuple<byte,System.ValueTuple<byte,byte>>>
	// Cysharp.Threading.Tasks.IUniTaskSource<System.ValueTuple<byte,System.ValueTuple<byte,object>>>
	// Cysharp.Threading.Tasks.IUniTaskSource<System.ValueTuple<byte,System.ValueTuple<object,object>>>
	// Cysharp.Threading.Tasks.IUniTaskSource<System.ValueTuple<byte,byte>>
	// Cysharp.Threading.Tasks.IUniTaskSource<System.ValueTuple<byte,object>>
	// Cysharp.Threading.Tasks.IUniTaskSource<System.ValueTuple<object,object>>
	// Cysharp.Threading.Tasks.IUniTaskSource<byte>
	// Cysharp.Threading.Tasks.IUniTaskSource<object>
	// Cysharp.Threading.Tasks.Internal.StatePool<Cysharp.Threading.Tasks.UniTask.Awaiter<byte>>
	// Cysharp.Threading.Tasks.Internal.StateTuple<Cysharp.Threading.Tasks.UniTask.Awaiter<byte>>
	// Cysharp.Threading.Tasks.UniTask.Awaiter<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.Awaiter<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.Awaiter<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.Awaiter<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.Awaiter<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<object,object>>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.Awaiter<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.Awaiter<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.Awaiter<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<object,object>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.Awaiter<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.Awaiter<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.Awaiter<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<object,object>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.Awaiter<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>
	// Cysharp.Threading.Tasks.UniTask.Awaiter<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>
	// Cysharp.Threading.Tasks.UniTask.Awaiter<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<object,object>>>>>>
	// Cysharp.Threading.Tasks.UniTask.Awaiter<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>
	// Cysharp.Threading.Tasks.UniTask.Awaiter<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>
	// Cysharp.Threading.Tasks.UniTask.Awaiter<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<object,object>>>>>
	// Cysharp.Threading.Tasks.UniTask.Awaiter<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>
	// Cysharp.Threading.Tasks.UniTask.Awaiter<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>
	// Cysharp.Threading.Tasks.UniTask.Awaiter<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<object,object>>>>
	// Cysharp.Threading.Tasks.UniTask.Awaiter<System.ValueTuple<byte,System.ValueTuple<byte,byte>>>
	// Cysharp.Threading.Tasks.UniTask.Awaiter<System.ValueTuple<byte,System.ValueTuple<byte,object>>>
	// Cysharp.Threading.Tasks.UniTask.Awaiter<System.ValueTuple<byte,System.ValueTuple<object,object>>>
	// Cysharp.Threading.Tasks.UniTask.Awaiter<System.ValueTuple<byte,byte>>
	// Cysharp.Threading.Tasks.UniTask.Awaiter<System.ValueTuple<byte,object>>
	// Cysharp.Threading.Tasks.UniTask.Awaiter<System.ValueTuple<object,object>>
	// Cysharp.Threading.Tasks.UniTask.Awaiter<byte>
	// Cysharp.Threading.Tasks.UniTask.Awaiter<object>
	// Cysharp.Threading.Tasks.UniTask.IsCanceledSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.IsCanceledSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.IsCanceledSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.IsCanceledSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.IsCanceledSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<object,object>>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.IsCanceledSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.IsCanceledSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.IsCanceledSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<object,object>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.IsCanceledSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.IsCanceledSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.IsCanceledSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<object,object>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.IsCanceledSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>
	// Cysharp.Threading.Tasks.UniTask.IsCanceledSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>
	// Cysharp.Threading.Tasks.UniTask.IsCanceledSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<object,object>>>>>>
	// Cysharp.Threading.Tasks.UniTask.IsCanceledSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>
	// Cysharp.Threading.Tasks.UniTask.IsCanceledSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>
	// Cysharp.Threading.Tasks.UniTask.IsCanceledSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<object,object>>>>>
	// Cysharp.Threading.Tasks.UniTask.IsCanceledSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>
	// Cysharp.Threading.Tasks.UniTask.IsCanceledSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>
	// Cysharp.Threading.Tasks.UniTask.IsCanceledSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<object,object>>>>
	// Cysharp.Threading.Tasks.UniTask.IsCanceledSource<System.ValueTuple<byte,System.ValueTuple<byte,byte>>>
	// Cysharp.Threading.Tasks.UniTask.IsCanceledSource<System.ValueTuple<byte,System.ValueTuple<byte,object>>>
	// Cysharp.Threading.Tasks.UniTask.IsCanceledSource<System.ValueTuple<byte,System.ValueTuple<object,object>>>
	// Cysharp.Threading.Tasks.UniTask.IsCanceledSource<System.ValueTuple<byte,byte>>
	// Cysharp.Threading.Tasks.UniTask.IsCanceledSource<System.ValueTuple<byte,object>>
	// Cysharp.Threading.Tasks.UniTask.IsCanceledSource<System.ValueTuple<object,object>>
	// Cysharp.Threading.Tasks.UniTask.IsCanceledSource<byte>
	// Cysharp.Threading.Tasks.UniTask.IsCanceledSource<object>
	// Cysharp.Threading.Tasks.UniTask.MemoizeSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.MemoizeSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.MemoizeSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.MemoizeSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.MemoizeSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<object,object>>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.MemoizeSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.MemoizeSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.MemoizeSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<object,object>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.MemoizeSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.MemoizeSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.MemoizeSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<object,object>>>>>>>
	// Cysharp.Threading.Tasks.UniTask.MemoizeSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>
	// Cysharp.Threading.Tasks.UniTask.MemoizeSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>
	// Cysharp.Threading.Tasks.UniTask.MemoizeSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<object,object>>>>>>
	// Cysharp.Threading.Tasks.UniTask.MemoizeSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>
	// Cysharp.Threading.Tasks.UniTask.MemoizeSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>
	// Cysharp.Threading.Tasks.UniTask.MemoizeSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<object,object>>>>>
	// Cysharp.Threading.Tasks.UniTask.MemoizeSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>
	// Cysharp.Threading.Tasks.UniTask.MemoizeSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>
	// Cysharp.Threading.Tasks.UniTask.MemoizeSource<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<object,object>>>>
	// Cysharp.Threading.Tasks.UniTask.MemoizeSource<System.ValueTuple<byte,System.ValueTuple<byte,byte>>>
	// Cysharp.Threading.Tasks.UniTask.MemoizeSource<System.ValueTuple<byte,System.ValueTuple<byte,object>>>
	// Cysharp.Threading.Tasks.UniTask.MemoizeSource<System.ValueTuple<byte,System.ValueTuple<object,object>>>
	// Cysharp.Threading.Tasks.UniTask.MemoizeSource<System.ValueTuple<byte,byte>>
	// Cysharp.Threading.Tasks.UniTask.MemoizeSource<System.ValueTuple<byte,object>>
	// Cysharp.Threading.Tasks.UniTask.MemoizeSource<System.ValueTuple<object,object>>
	// Cysharp.Threading.Tasks.UniTask.MemoizeSource<byte>
	// Cysharp.Threading.Tasks.UniTask.MemoizeSource<object>
	// Cysharp.Threading.Tasks.UniTask<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<object,object>>>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<object,object>>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<object,object>>>>>>>>
	// Cysharp.Threading.Tasks.UniTask<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>>
	// Cysharp.Threading.Tasks.UniTask<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>
	// Cysharp.Threading.Tasks.UniTask<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<object,object>>>>>>>
	// Cysharp.Threading.Tasks.UniTask<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>
	// Cysharp.Threading.Tasks.UniTask<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>
	// Cysharp.Threading.Tasks.UniTask<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<object,object>>>>>>
	// Cysharp.Threading.Tasks.UniTask<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>
	// Cysharp.Threading.Tasks.UniTask<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>
	// Cysharp.Threading.Tasks.UniTask<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<object,object>>>>>
	// Cysharp.Threading.Tasks.UniTask<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>
	// Cysharp.Threading.Tasks.UniTask<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>
	// Cysharp.Threading.Tasks.UniTask<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<object,object>>>>
	// Cysharp.Threading.Tasks.UniTask<System.ValueTuple<byte,System.ValueTuple<byte,byte>>>
	// Cysharp.Threading.Tasks.UniTask<System.ValueTuple<byte,System.ValueTuple<byte,object>>>
	// Cysharp.Threading.Tasks.UniTask<System.ValueTuple<byte,System.ValueTuple<object,object>>>
	// Cysharp.Threading.Tasks.UniTask<System.ValueTuple<byte,byte>>
	// Cysharp.Threading.Tasks.UniTask<System.ValueTuple<byte,object>>
	// Cysharp.Threading.Tasks.UniTask<System.ValueTuple<object,object>>
	// Cysharp.Threading.Tasks.UniTask<byte>
	// Cysharp.Threading.Tasks.UniTask<object>
	// Cysharp.Threading.Tasks.UniTaskCompletionSource<byte>
	// Cysharp.Threading.Tasks.UniTaskCompletionSourceCore<Cysharp.Threading.Tasks.AsyncUnit>
	// Cysharp.Threading.Tasks.UniTaskCompletionSourceCore<System.ValueTuple<object,object>>
	// Cysharp.Threading.Tasks.UniTaskCompletionSourceCore<object>
	// Cysharp.Threading.Tasks.UniTaskExtensions.<>c__19<byte>
	// Google.Protobuf.IMessage<object>
	// Google.Protobuf.MessageParser.<>c__DisplayClass2_0<object>
	// Google.Protobuf.MessageParser<object>
	// MiniCore.Core.Global.<>c__16<object,object>
	// MiniCore.Core.GlobalServiceRegistry.<>c__DisplayClass2_0<object,object>
	// MiniCore.Model.AMHandler<object>
	// MiniCore.Model.APresenter<object>
	// MiniCore.Model.ARpcHandler<object,object>
	// MiniCore.Model.MonoSingleton<object>
	// System.Action<System.ValueTuple<object,object>>
	// System.Action<object,object>
	// System.Action<object>
	// System.Collections.Concurrent.ConcurrentQueue.<Enumerate>d__28<object>
	// System.Collections.Concurrent.ConcurrentQueue.Segment<object>
	// System.Collections.Concurrent.ConcurrentQueue<object>
	// System.Collections.Generic.ArraySortHelper<System.ValueTuple<object,object>>
	// System.Collections.Generic.ArraySortHelper<object>
	// System.Collections.Generic.Comparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>>>>
	// System.Collections.Generic.Comparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>>
	// System.Collections.Generic.Comparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>>>
	// System.Collections.Generic.Comparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>
	// System.Collections.Generic.Comparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<object,object>>>>>>>>
	// System.Collections.Generic.Comparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>>
	// System.Collections.Generic.Comparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>
	// System.Collections.Generic.Comparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<object,object>>>>>>>
	// System.Collections.Generic.Comparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>
	// System.Collections.Generic.Comparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>
	// System.Collections.Generic.Comparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<object,object>>>>>>
	// System.Collections.Generic.Comparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>
	// System.Collections.Generic.Comparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>
	// System.Collections.Generic.Comparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<object,object>>>>>
	// System.Collections.Generic.Comparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>
	// System.Collections.Generic.Comparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>
	// System.Collections.Generic.Comparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<object,object>>>>
	// System.Collections.Generic.Comparer<System.ValueTuple<byte,System.ValueTuple<byte,byte>>>
	// System.Collections.Generic.Comparer<System.ValueTuple<byte,System.ValueTuple<byte,object>>>
	// System.Collections.Generic.Comparer<System.ValueTuple<byte,System.ValueTuple<object,object>>>
	// System.Collections.Generic.Comparer<System.ValueTuple<byte,byte>>
	// System.Collections.Generic.Comparer<System.ValueTuple<byte,object>>
	// System.Collections.Generic.Comparer<System.ValueTuple<object,object>>
	// System.Collections.Generic.Comparer<byte>
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
	// System.Collections.Generic.EqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>>>>
	// System.Collections.Generic.EqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>>
	// System.Collections.Generic.EqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>>>
	// System.Collections.Generic.EqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>
	// System.Collections.Generic.EqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<object,object>>>>>>>>
	// System.Collections.Generic.EqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>>
	// System.Collections.Generic.EqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>
	// System.Collections.Generic.EqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<object,object>>>>>>>
	// System.Collections.Generic.EqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>
	// System.Collections.Generic.EqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>
	// System.Collections.Generic.EqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<object,object>>>>>>
	// System.Collections.Generic.EqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>
	// System.Collections.Generic.EqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>
	// System.Collections.Generic.EqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<object,object>>>>>
	// System.Collections.Generic.EqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>
	// System.Collections.Generic.EqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>
	// System.Collections.Generic.EqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<object,object>>>>
	// System.Collections.Generic.EqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,byte>>>
	// System.Collections.Generic.EqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,object>>>
	// System.Collections.Generic.EqualityComparer<System.ValueTuple<byte,System.ValueTuple<object,object>>>
	// System.Collections.Generic.EqualityComparer<System.ValueTuple<byte,byte>>
	// System.Collections.Generic.EqualityComparer<System.ValueTuple<byte,object>>
	// System.Collections.Generic.EqualityComparer<System.ValueTuple<object,object>>
	// System.Collections.Generic.EqualityComparer<byte>
	// System.Collections.Generic.EqualityComparer<int>
	// System.Collections.Generic.EqualityComparer<object>
	// System.Collections.Generic.HashSet.Enumerator<object>
	// System.Collections.Generic.HashSet<object>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<object,int>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.ICollection<System.ValueTuple<object,object>>
	// System.Collections.Generic.ICollection<object>
	// System.Collections.Generic.IComparer<System.ValueTuple<object,object>>
	// System.Collections.Generic.IComparer<object>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,int>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.IEnumerable<System.ValueTuple<object,object>>
	// System.Collections.Generic.IEnumerable<object>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<object,int>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.IEnumerator<System.ValueTuple<object,object>>
	// System.Collections.Generic.IEnumerator<object>
	// System.Collections.Generic.IEqualityComparer<object>
	// System.Collections.Generic.IList<System.ValueTuple<object,object>>
	// System.Collections.Generic.IList<object>
	// System.Collections.Generic.KeyValuePair<object,int>
	// System.Collections.Generic.KeyValuePair<object,object>
	// System.Collections.Generic.List.Enumerator<System.ValueTuple<object,object>>
	// System.Collections.Generic.List.Enumerator<object>
	// System.Collections.Generic.List<System.ValueTuple<object,object>>
	// System.Collections.Generic.List<object>
	// System.Collections.Generic.ObjectComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>>>
	// System.Collections.Generic.ObjectComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>
	// System.Collections.Generic.ObjectComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>>
	// System.Collections.Generic.ObjectComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>
	// System.Collections.Generic.ObjectComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<object,object>>>>>>>
	// System.Collections.Generic.ObjectComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>
	// System.Collections.Generic.ObjectComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>
	// System.Collections.Generic.ObjectComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<object,object>>>>>>
	// System.Collections.Generic.ObjectComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>
	// System.Collections.Generic.ObjectComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>
	// System.Collections.Generic.ObjectComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<object,object>>>>>
	// System.Collections.Generic.ObjectComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>
	// System.Collections.Generic.ObjectComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>
	// System.Collections.Generic.ObjectComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<object,object>>>>
	// System.Collections.Generic.ObjectComparer<System.ValueTuple<byte,System.ValueTuple<byte,byte>>>
	// System.Collections.Generic.ObjectComparer<System.ValueTuple<byte,System.ValueTuple<byte,object>>>
	// System.Collections.Generic.ObjectComparer<System.ValueTuple<byte,System.ValueTuple<object,object>>>
	// System.Collections.Generic.ObjectComparer<System.ValueTuple<byte,byte>>
	// System.Collections.Generic.ObjectComparer<System.ValueTuple<byte,object>>
	// System.Collections.Generic.ObjectComparer<System.ValueTuple<object,object>>
	// System.Collections.Generic.ObjectComparer<byte>
	// System.Collections.Generic.ObjectComparer<object>
	// System.Collections.Generic.ObjectEqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>>>
	// System.Collections.Generic.ObjectEqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>
	// System.Collections.Generic.ObjectEqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>>
	// System.Collections.Generic.ObjectEqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>
	// System.Collections.Generic.ObjectEqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<object,object>>>>>>>
	// System.Collections.Generic.ObjectEqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>
	// System.Collections.Generic.ObjectEqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>
	// System.Collections.Generic.ObjectEqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<object,object>>>>>>
	// System.Collections.Generic.ObjectEqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>
	// System.Collections.Generic.ObjectEqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>
	// System.Collections.Generic.ObjectEqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<object,object>>>>>
	// System.Collections.Generic.ObjectEqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>
	// System.Collections.Generic.ObjectEqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>
	// System.Collections.Generic.ObjectEqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<object,object>>>>
	// System.Collections.Generic.ObjectEqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,byte>>>
	// System.Collections.Generic.ObjectEqualityComparer<System.ValueTuple<byte,System.ValueTuple<byte,object>>>
	// System.Collections.Generic.ObjectEqualityComparer<System.ValueTuple<byte,System.ValueTuple<object,object>>>
	// System.Collections.Generic.ObjectEqualityComparer<System.ValueTuple<byte,byte>>
	// System.Collections.Generic.ObjectEqualityComparer<System.ValueTuple<byte,object>>
	// System.Collections.Generic.ObjectEqualityComparer<System.ValueTuple<object,object>>
	// System.Collections.Generic.ObjectEqualityComparer<byte>
	// System.Collections.Generic.ObjectEqualityComparer<int>
	// System.Collections.Generic.ObjectEqualityComparer<object>
	// System.Collections.Generic.Stack.Enumerator<object>
	// System.Collections.Generic.Stack<object>
	// System.Collections.ObjectModel.ReadOnlyCollection<System.ValueTuple<object,object>>
	// System.Collections.ObjectModel.ReadOnlyCollection<object>
	// System.Comparison<System.ValueTuple<object,object>>
	// System.Comparison<object>
	// System.Func<Cysharp.Threading.Tasks.UniTask<byte>>
	// System.Func<Cysharp.Threading.Tasks.UniTask>
	// System.Func<Cysharp.Threading.Tasks.UniTaskVoid>
	// System.Func<System.Threading.Tasks.VoidTaskResult>
	// System.Func<byte>
	// System.Func<int>
	// System.Func<object,System.Threading.Tasks.VoidTaskResult>
	// System.Func<object,object,object>
	// System.Func<object,object>
	// System.Func<object>
	// System.Predicate<System.ValueTuple<object,object>>
	// System.Predicate<object>
	// System.Runtime.CompilerServices.AsyncTaskMethodBuilder<System.Threading.Tasks.VoidTaskResult>
	// System.Runtime.CompilerServices.ConfiguredTaskAwaitable.ConfiguredTaskAwaiter<System.Threading.Tasks.VoidTaskResult>
	// System.Runtime.CompilerServices.ConfiguredTaskAwaitable<System.Threading.Tasks.VoidTaskResult>
	// System.Runtime.CompilerServices.TaskAwaiter<System.Threading.Tasks.VoidTaskResult>
	// System.Threading.Tasks.ContinuationTaskFromResultTask<System.Threading.Tasks.VoidTaskResult>
	// System.Threading.Tasks.Task<System.Threading.Tasks.VoidTaskResult>
	// System.Threading.Tasks.TaskFactory.<>c__DisplayClass35_0<System.Threading.Tasks.VoidTaskResult>
	// System.Threading.Tasks.TaskFactory<System.Threading.Tasks.VoidTaskResult>
	// System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>>>>>
	// System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>>>
	// System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>>>>
	// System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>>
	// System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<object,object>>>>>>>>>
	// System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>>>
	// System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>>
	// System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<object,object>>>>>>>>
	// System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>>
	// System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>>
	// System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<object,object>>>>>>>
	// System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>>
	// System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>>
	// System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<object,object>>>>>>
	// System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>>
	// System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>>
	// System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<object,object>>>>>
	// System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>>
	// System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>>
	// System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<object,object>>>>
	// System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,byte>>>
	// System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<byte,object>>>
	// System.ValueTuple<byte,System.ValueTuple<byte,System.ValueTuple<object,object>>>
	// System.ValueTuple<byte,System.ValueTuple<byte,byte>>
	// System.ValueTuple<byte,System.ValueTuple<byte,object>>
	// System.ValueTuple<byte,System.ValueTuple<object,object>>
	// System.ValueTuple<byte,byte>
	// System.ValueTuple<byte,object>
	// System.ValueTuple<object,object>
	// }}

	public void RefMethods()
	{
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter,MiniCore.HotUpdate.NetworkSmokeTestRunner.<RunTransportAsync>d__42>(Cysharp.Threading.Tasks.UniTask.Awaiter&,MiniCore.HotUpdate.NetworkSmokeTestRunner.<RunTransportAsync>d__42&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter,MiniCore.HotUpdate.TestHandler.<HandleAsync>d__0>(Cysharp.Threading.Tasks.UniTask.Awaiter&,MiniCore.HotUpdate.TestHandler.<HandleAsync>d__0&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter,MiniCore.Service.UIService.<CloseAsync>d__9<object>>(Cysharp.Threading.Tasks.UniTask.Awaiter&,MiniCore.Service.UIService.<CloseAsync>d__9<object>&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter<byte>,MiniCore.HotUpdate.NetworkSmokeTestRunner.<RunTransportAsync>d__42>(Cysharp.Threading.Tasks.UniTask.Awaiter<byte>&,MiniCore.HotUpdate.NetworkSmokeTestRunner.<RunTransportAsync>d__42&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter<object>,MiniCore.HotUpdate.NetworkSmokeTestRunner.<RunTransportAsync>d__42>(Cysharp.Threading.Tasks.UniTask.Awaiter<object>&,MiniCore.HotUpdate.NetworkSmokeTestRunner.<RunTransportAsync>d__42&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter<object>,MiniCore.Service.UIService.<PreloadAsync>d__7<object,object>>(Cysharp.Threading.Tasks.UniTask.Awaiter<object>&,MiniCore.Service.UIService.<PreloadAsync>d__7<object,object>&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.YieldAwaitable.Awaiter,MiniCore.HotUpdate.NetworkSmokeTestRunner.<WaitForConditionAsync>d__43>(Cysharp.Threading.Tasks.YieldAwaitable.Awaiter&,MiniCore.HotUpdate.NetworkSmokeTestRunner.<WaitForConditionAsync>d__43&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<System.ValueTuple<object,object>>.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter,MiniCore.Service.UIService.<OpenAsync>d__8<object,object>>(Cysharp.Threading.Tasks.UniTask.Awaiter&,MiniCore.Service.UIService.<OpenAsync>d__8<object,object>&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<System.ValueTuple<object,object>>.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter<object>,MiniCore.Service.UIService.<OpenAsync>d__8<object,object>>(Cysharp.Threading.Tasks.UniTask.Awaiter<object>&,MiniCore.Service.UIService.<OpenAsync>d__8<object,object>&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter<object>,MiniCore.Core.ExcelTool.<LoadCsvFileAsync>d__1<object>>(Cysharp.Threading.Tasks.UniTask.Awaiter<object>&,MiniCore.Core.ExcelTool.<LoadCsvFileAsync>d__1<object>&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter<object>,MiniCore.Core.GameObjectPool.<CreateObjectAsync>d__6>(Cysharp.Threading.Tasks.UniTask.Awaiter<object>&,MiniCore.Core.GameObjectPool.<CreateObjectAsync>d__6&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter<object>,MiniCore.Core.GameObjectPoolMgr.<GeneratePoolObject>d__4<object>>(Cysharp.Threading.Tasks.UniTask.Awaiter<object>&,MiniCore.Core.GameObjectPoolMgr.<GeneratePoolObject>d__4<object>&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter<object>,MiniCore.Service.AssetService.<PreloadAssetAsync>d__7<object>>(Cysharp.Threading.Tasks.UniTask.Awaiter<object>&,MiniCore.Service.AssetService.<PreloadAssetAsync>d__7<object>&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter<object>,MiniCore.Service.UIService.<CreateWindowInstanceAsync>d__12<object>>(Cysharp.Threading.Tasks.UniTask.Awaiter<object>&,MiniCore.Service.UIService.<CreateWindowInstanceAsync>d__12<object>&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter<object>,MiniCore.Service.YooAssetResourceService.<InstantiateAsync>d__2>(Cysharp.Threading.Tasks.UniTask.Awaiter<object>&,MiniCore.Service.YooAssetResourceService.<InstantiateAsync>d__2&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter,MiniCore.Service.YooAssetResourceService.<LoadAssetAsync>d__3<object>>(System.Runtime.CompilerServices.TaskAwaiter&,MiniCore.Service.YooAssetResourceService.<LoadAssetAsync>d__3<object>&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter,MiniCore.Service.YooAssetResourceService.<PreloadAssetAsync>d__4<object>>(System.Runtime.CompilerServices.TaskAwaiter&,MiniCore.Service.YooAssetResourceService.<PreloadAssetAsync>d__4<object>&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder.Start<MiniCore.HotUpdate.DemoNormalHandler.<HandleAsync>d__0>(MiniCore.HotUpdate.DemoNormalHandler.<HandleAsync>d__0&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder.Start<MiniCore.HotUpdate.NetworkSmokeTestRunner.<RunTransportAsync>d__42>(MiniCore.HotUpdate.NetworkSmokeTestRunner.<RunTransportAsync>d__42&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder.Start<MiniCore.HotUpdate.NetworkSmokeTestRunner.<WaitForConditionAsync>d__43>(MiniCore.HotUpdate.NetworkSmokeTestRunner.<WaitForConditionAsync>d__43&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder.Start<MiniCore.HotUpdate.TestHandler.<HandleAsync>d__0>(MiniCore.HotUpdate.TestHandler.<HandleAsync>d__0&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder.Start<MiniCore.Service.UIService.<CloseAsync>d__9<object>>(MiniCore.Service.UIService.<CloseAsync>d__9<object>&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder.Start<MiniCore.Service.UIService.<PreloadAsync>d__7<object,object>>(MiniCore.Service.UIService.<PreloadAsync>d__7<object,object>&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<System.ValueTuple<object,object>>.Start<MiniCore.Service.UIService.<OpenAsync>d__8<object,object>>(MiniCore.Service.UIService.<OpenAsync>d__8<object,object>&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<object>.Start<MiniCore.Core.ExcelTool.<LoadCsvFileAsync>d__1<object>>(MiniCore.Core.ExcelTool.<LoadCsvFileAsync>d__1<object>&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<object>.Start<MiniCore.Core.GameObjectPool.<CreateObjectAsync>d__6>(MiniCore.Core.GameObjectPool.<CreateObjectAsync>d__6&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<object>.Start<MiniCore.Core.GameObjectPoolMgr.<GeneratePoolObject>d__4<object>>(MiniCore.Core.GameObjectPoolMgr.<GeneratePoolObject>d__4<object>&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<object>.Start<MiniCore.Service.AssetService.<PreloadAssetAsync>d__7<object>>(MiniCore.Service.AssetService.<PreloadAssetAsync>d__7<object>&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<object>.Start<MiniCore.Service.UIService.<CreateWindowInstanceAsync>d__12<object>>(MiniCore.Service.UIService.<CreateWindowInstanceAsync>d__12<object>&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<object>.Start<MiniCore.Service.YooAssetResourceService.<InstantiateAsync>d__2>(MiniCore.Service.YooAssetResourceService.<InstantiateAsync>d__2&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<object>.Start<MiniCore.Service.YooAssetResourceService.<LoadAssetAsync>d__3<object>>(MiniCore.Service.YooAssetResourceService.<LoadAssetAsync>d__3<object>&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskMethodBuilder<object>.Start<MiniCore.Service.YooAssetResourceService.<PreloadAssetAsync>d__4<object>>(MiniCore.Service.YooAssetResourceService.<PreloadAssetAsync>d__4<object>&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.SwitchToMainThreadAwaitable.Awaiter,MiniCore.HotUpdate.KcpTestWindowPresenter.<<ConnectClientAsync>b__14_1>d>(Cysharp.Threading.Tasks.SwitchToMainThreadAwaitable.Awaiter&,MiniCore.HotUpdate.KcpTestWindowPresenter.<<ConnectClientAsync>b__14_1>d&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter,MiniCore.HotUpdate.DedicatedClientSmokeTestRunner.<RunInternalAsync>d__28>(Cysharp.Threading.Tasks.UniTask.Awaiter&,MiniCore.HotUpdate.DedicatedClientSmokeTestRunner.<RunInternalAsync>d__28&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter,MiniCore.HotUpdate.KcpTestWindowPresenter.<DisconnectClientAsync>d__15>(Cysharp.Threading.Tasks.UniTask.Awaiter&,MiniCore.HotUpdate.KcpTestWindowPresenter.<DisconnectClientAsync>d__15&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter,MiniCore.HotUpdate.KcpTestWindowPresenter.<SendNormalAsync>d__19>(Cysharp.Threading.Tasks.UniTask.Awaiter&,MiniCore.HotUpdate.KcpTestWindowPresenter.<SendNormalAsync>d__19&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter,MiniCore.HotUpdate.KcpTestWindowPresenter.<StartServerAsync>d__12>(Cysharp.Threading.Tasks.UniTask.Awaiter&,MiniCore.HotUpdate.KcpTestWindowPresenter.<StartServerAsync>d__12&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter,MiniCore.HotUpdate.KcpTestWindowPresenter.<StopServerAsync>d__16>(Cysharp.Threading.Tasks.UniTask.Awaiter&,MiniCore.HotUpdate.KcpTestWindowPresenter.<StopServerAsync>d__16&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter,MiniCore.HotUpdate.MultiProtocolTestPanel.<SendNormalAsync>d__49>(Cysharp.Threading.Tasks.UniTask.Awaiter&,MiniCore.HotUpdate.MultiProtocolTestPanel.<SendNormalAsync>d__49&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter,MiniCore.HotUpdate.MultiProtocolTestPanel.<StartKcpServerAsync>d__40>(Cysharp.Threading.Tasks.UniTask.Awaiter&,MiniCore.HotUpdate.MultiProtocolTestPanel.<StartKcpServerAsync>d__40&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter,MiniCore.HotUpdate.MultiProtocolTestPanel.<StartTcpServerAsync>d__39>(Cysharp.Threading.Tasks.UniTask.Awaiter&,MiniCore.HotUpdate.MultiProtocolTestPanel.<StartTcpServerAsync>d__39&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter,MiniCore.HotUpdate.MultiProtocolTestPanel.<StartUdpServerAsync>d__41>(Cysharp.Threading.Tasks.UniTask.Awaiter&,MiniCore.HotUpdate.MultiProtocolTestPanel.<StartUdpServerAsync>d__41&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter,MiniCore.HotUpdate.NetworkSmokeTestRunner.<RunInternalAsync>d__38>(Cysharp.Threading.Tasks.UniTask.Awaiter&,MiniCore.HotUpdate.NetworkSmokeTestRunner.<RunInternalAsync>d__38&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter<byte>,MiniCore.HotUpdate.DedicatedClientSmokeTestRunner.<RunInternalAsync>d__28>(Cysharp.Threading.Tasks.UniTask.Awaiter<byte>&,MiniCore.HotUpdate.DedicatedClientSmokeTestRunner.<RunInternalAsync>d__28&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter<byte>,MiniCore.HotUpdate.KcpTestWindowPresenter.<ConnectClientAsync>d__14>(Cysharp.Threading.Tasks.UniTask.Awaiter<byte>&,MiniCore.HotUpdate.KcpTestWindowPresenter.<ConnectClientAsync>d__14&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter<byte>,MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectKcpAsync>d__46>(Cysharp.Threading.Tasks.UniTask.Awaiter<byte>&,MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectKcpAsync>d__46&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter<byte>,MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectTcpAsync>d__45>(Cysharp.Threading.Tasks.UniTask.Awaiter<byte>&,MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectTcpAsync>d__45&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter<byte>,MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectUdpAsync>d__47>(Cysharp.Threading.Tasks.UniTask.Awaiter<byte>&,MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectUdpAsync>d__47&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter<object>,MiniCore.HotUpdate.DedicatedClientSmokeTestRunner.<RunInternalAsync>d__28>(Cysharp.Threading.Tasks.UniTask.Awaiter<object>&,MiniCore.HotUpdate.DedicatedClientSmokeTestRunner.<RunInternalAsync>d__28&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter<object>,MiniCore.HotUpdate.KcpTestWindowPresenter.<SendRpcAsync>d__20>(Cysharp.Threading.Tasks.UniTask.Awaiter<object>&,MiniCore.HotUpdate.KcpTestWindowPresenter.<SendRpcAsync>d__20&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.AwaitUnsafeOnCompleted<Cysharp.Threading.Tasks.UniTask.Awaiter<object>,MiniCore.HotUpdate.MultiProtocolTestPanel.<SendRpcAsync>d__50>(Cysharp.Threading.Tasks.UniTask.Awaiter<object>&,MiniCore.HotUpdate.MultiProtocolTestPanel.<SendRpcAsync>d__50&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.Start<MiniCore.HotUpdate.DedicatedClientSmokeTestRunner.<RunInternalAsync>d__28>(MiniCore.HotUpdate.DedicatedClientSmokeTestRunner.<RunInternalAsync>d__28&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.Start<MiniCore.HotUpdate.KcpTestWindowPresenter.<<ConnectClientAsync>b__14_1>d>(MiniCore.HotUpdate.KcpTestWindowPresenter.<<ConnectClientAsync>b__14_1>d&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.Start<MiniCore.HotUpdate.KcpTestWindowPresenter.<ConnectClientAsync>d__14>(MiniCore.HotUpdate.KcpTestWindowPresenter.<ConnectClientAsync>d__14&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.Start<MiniCore.HotUpdate.KcpTestWindowPresenter.<DisconnectClientAsync>d__15>(MiniCore.HotUpdate.KcpTestWindowPresenter.<DisconnectClientAsync>d__15&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.Start<MiniCore.HotUpdate.KcpTestWindowPresenter.<SendNormalAsync>d__19>(MiniCore.HotUpdate.KcpTestWindowPresenter.<SendNormalAsync>d__19&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.Start<MiniCore.HotUpdate.KcpTestWindowPresenter.<SendRpcAsync>d__20>(MiniCore.HotUpdate.KcpTestWindowPresenter.<SendRpcAsync>d__20&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.Start<MiniCore.HotUpdate.KcpTestWindowPresenter.<StartServerAsync>d__12>(MiniCore.HotUpdate.KcpTestWindowPresenter.<StartServerAsync>d__12&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.Start<MiniCore.HotUpdate.KcpTestWindowPresenter.<StopServerAsync>d__16>(MiniCore.HotUpdate.KcpTestWindowPresenter.<StopServerAsync>d__16&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.Start<MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectKcpAsync>d__46>(MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectKcpAsync>d__46&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.Start<MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectTcpAsync>d__45>(MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectTcpAsync>d__45&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.Start<MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectUdpAsync>d__47>(MiniCore.HotUpdate.MultiProtocolTestPanel.<ConnectUdpAsync>d__47&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.Start<MiniCore.HotUpdate.MultiProtocolTestPanel.<SendNormalAsync>d__49>(MiniCore.HotUpdate.MultiProtocolTestPanel.<SendNormalAsync>d__49&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.Start<MiniCore.HotUpdate.MultiProtocolTestPanel.<SendRpcAsync>d__50>(MiniCore.HotUpdate.MultiProtocolTestPanel.<SendRpcAsync>d__50&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.Start<MiniCore.HotUpdate.MultiProtocolTestPanel.<StartKcpServerAsync>d__40>(MiniCore.HotUpdate.MultiProtocolTestPanel.<StartKcpServerAsync>d__40&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.Start<MiniCore.HotUpdate.MultiProtocolTestPanel.<StartTcpServerAsync>d__39>(MiniCore.HotUpdate.MultiProtocolTestPanel.<StartTcpServerAsync>d__39&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.Start<MiniCore.HotUpdate.MultiProtocolTestPanel.<StartUdpServerAsync>d__41>(MiniCore.HotUpdate.MultiProtocolTestPanel.<StartUdpServerAsync>d__41&)
		// System.Void Cysharp.Threading.Tasks.CompilerServices.AsyncUniTaskVoidMethodBuilder.Start<MiniCore.HotUpdate.NetworkSmokeTestRunner.<RunInternalAsync>d__38>(MiniCore.HotUpdate.NetworkSmokeTestRunner.<RunInternalAsync>d__38&)
		// Cysharp.Threading.Tasks.Internal.StateTuple<Cysharp.Threading.Tasks.UniTask.Awaiter<byte>> Cysharp.Threading.Tasks.Internal.StateTuple.Create<Cysharp.Threading.Tasks.UniTask.Awaiter<byte>>(Cysharp.Threading.Tasks.UniTask.Awaiter<byte>)
		// Cysharp.Threading.Tasks.UniTask<object> Cysharp.Threading.Tasks.UniTask.FromResult<object>(object)
		// System.Void Cysharp.Threading.Tasks.UniTaskExtensions.Forget<byte>(Cysharp.Threading.Tasks.UniTask<byte>)
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
		// Cysharp.Threading.Tasks.UniTask<object> MiniCore.Service.INetworkService.CallAsync<object,object>(string,object,System.Threading.CancellationToken)
		// Cysharp.Threading.Tasks.UniTask MiniCore.Service.INetworkService.SendAsync<object>(string,object,System.Threading.CancellationToken)
		// object Newtonsoft.Json.JsonConvert.DeserializeObject<object>(string)
		// object Newtonsoft.Json.JsonConvert.DeserializeObject<object>(string,Newtonsoft.Json.JsonSerializerSettings)
		// object System.Activator.CreateInstance<object>()
		// object[] System.Array.Empty<object>()
		// object System.Reflection.CustomAttributeExtensions.GetCustomAttribute<object>(System.Reflection.MemberInfo)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter,MiniCore.HotUpdate.GameStartup.<StartAsync>d__0>(System.Runtime.CompilerServices.TaskAwaiter&,MiniCore.HotUpdate.GameStartup.<StartAsync>d__0&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter,MiniCore.HotUpdate.GameStartup.<StartDedicatedServerAsync>d__5>(System.Runtime.CompilerServices.TaskAwaiter&,MiniCore.HotUpdate.GameStartup.<StartDedicatedServerAsync>d__5&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter,MiniCore.HotUpdate.MiniCoreStartup.<StartAsync>d__0>(System.Runtime.CompilerServices.TaskAwaiter&,MiniCore.HotUpdate.MiniCoreStartup.<StartAsync>d__0&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter,MiniCore.HotUpdate.MiniCoreStartup.<StartClientAsync>d__1>(System.Runtime.CompilerServices.TaskAwaiter&,MiniCore.HotUpdate.MiniCoreStartup.<StartClientAsync>d__1&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<System.Threading.Tasks.VoidTaskResult>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter,MiniCore.HotUpdate.GameStartup.<StartAsync>d__0>(System.Runtime.CompilerServices.TaskAwaiter&,MiniCore.HotUpdate.GameStartup.<StartAsync>d__0&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<System.Threading.Tasks.VoidTaskResult>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter,MiniCore.HotUpdate.GameStartup.<StartDedicatedServerAsync>d__5>(System.Runtime.CompilerServices.TaskAwaiter&,MiniCore.HotUpdate.GameStartup.<StartDedicatedServerAsync>d__5&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<System.Threading.Tasks.VoidTaskResult>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter,MiniCore.HotUpdate.MiniCoreStartup.<StartAsync>d__0>(System.Runtime.CompilerServices.TaskAwaiter&,MiniCore.HotUpdate.MiniCoreStartup.<StartAsync>d__0&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder<System.Threading.Tasks.VoidTaskResult>.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.TaskAwaiter,MiniCore.HotUpdate.MiniCoreStartup.<StartClientAsync>d__1>(System.Runtime.CompilerServices.TaskAwaiter&,MiniCore.HotUpdate.MiniCoreStartup.<StartClientAsync>d__1&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.Start<MiniCore.HotUpdate.GameStartup.<StartAsync>d__0>(MiniCore.HotUpdate.GameStartup.<StartAsync>d__0&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.Start<MiniCore.HotUpdate.GameStartup.<StartDedicatedServerAsync>d__5>(MiniCore.HotUpdate.GameStartup.<StartDedicatedServerAsync>d__5&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.Start<MiniCore.HotUpdate.MiniCoreStartup.<StartAsync>d__0>(MiniCore.HotUpdate.MiniCoreStartup.<StartAsync>d__0&)
		// System.Void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.Start<MiniCore.HotUpdate.MiniCoreStartup.<StartClientAsync>d__1>(MiniCore.HotUpdate.MiniCoreStartup.<StartClientAsync>d__1&)
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