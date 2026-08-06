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
	// MiniCore.Threading.MTaskStateMachineRunner<object>
	// MiniCore.UI.AUIWindowPresenter<object>
	// MiniCore.UI.IUIWindowArgs<object>
	// System.Action<MiniCore.Demo.MiniBomber.MiniBomberBattleParticipant>
	// System.Action<MiniCore.Demo.MiniBomber.MiniBomberSimulationEvent>
	// System.Action<MiniCore.Demo.MiniBomber.Unity.BomberInputFrame>
	// System.Action<float>
	// System.Action<long>
	// System.Action<object>
	// System.ArraySegment.Enumerator<byte>
	// System.ArraySegment<byte>
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
	// System.Collections.Generic.Dictionary.Enumerator<MiniCore.Service.UIService.UIWindowLogicalKey,object>
	// System.Collections.Generic.Dictionary.Enumerator<MiniCore.UI.UIWindowId,object>
	// System.Collections.Generic.Dictionary.Enumerator<MiniCore.UI.UIWindowId,uint>
	// System.Collections.Generic.Dictionary.Enumerator<MiniCore.UI.UIWindowInstanceId,object>
	// System.Collections.Generic.Dictionary.Enumerator<long,int>
	// System.Collections.Generic.Dictionary.Enumerator<long,object>
	// System.Collections.Generic.Dictionary.Enumerator<object,int>
	// System.Collections.Generic.Dictionary.Enumerator<object,object>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<MiniCore.Service.UIService.UIWindowLogicalKey,object>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<MiniCore.UI.UIWindowId,object>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<MiniCore.UI.UIWindowId,uint>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<MiniCore.UI.UIWindowInstanceId,object>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<long,int>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<long,object>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<object,int>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<object,object>
	// System.Collections.Generic.Dictionary.KeyCollection<MiniCore.Service.UIService.UIWindowLogicalKey,object>
	// System.Collections.Generic.Dictionary.KeyCollection<MiniCore.UI.UIWindowId,object>
	// System.Collections.Generic.Dictionary.KeyCollection<MiniCore.UI.UIWindowId,uint>
	// System.Collections.Generic.Dictionary.KeyCollection<MiniCore.UI.UIWindowInstanceId,object>
	// System.Collections.Generic.Dictionary.KeyCollection<long,int>
	// System.Collections.Generic.Dictionary.KeyCollection<long,object>
	// System.Collections.Generic.Dictionary.KeyCollection<object,int>
	// System.Collections.Generic.Dictionary.KeyCollection<object,object>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<MiniCore.Service.UIService.UIWindowLogicalKey,object>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<MiniCore.UI.UIWindowId,object>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<MiniCore.UI.UIWindowId,uint>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<MiniCore.UI.UIWindowInstanceId,object>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<long,int>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<long,object>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<object,int>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<object,object>
	// System.Collections.Generic.Dictionary.ValueCollection<MiniCore.Service.UIService.UIWindowLogicalKey,object>
	// System.Collections.Generic.Dictionary.ValueCollection<MiniCore.UI.UIWindowId,object>
	// System.Collections.Generic.Dictionary.ValueCollection<MiniCore.UI.UIWindowId,uint>
	// System.Collections.Generic.Dictionary.ValueCollection<MiniCore.UI.UIWindowInstanceId,object>
	// System.Collections.Generic.Dictionary.ValueCollection<long,int>
	// System.Collections.Generic.Dictionary.ValueCollection<long,object>
	// System.Collections.Generic.Dictionary.ValueCollection<object,int>
	// System.Collections.Generic.Dictionary.ValueCollection<object,object>
	// System.Collections.Generic.Dictionary<MiniCore.Service.UIService.UIWindowLogicalKey,object>
	// System.Collections.Generic.Dictionary<MiniCore.UI.UIWindowId,object>
	// System.Collections.Generic.Dictionary<MiniCore.UI.UIWindowId,uint>
	// System.Collections.Generic.Dictionary<MiniCore.UI.UIWindowInstanceId,object>
	// System.Collections.Generic.Dictionary<long,int>
	// System.Collections.Generic.Dictionary<long,object>
	// System.Collections.Generic.Dictionary<object,int>
	// System.Collections.Generic.Dictionary<object,object>
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
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<MiniCore.Service.UIService.UIWindowLogicalKey,object>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<MiniCore.UI.UIWindowId,object>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<MiniCore.UI.UIWindowId,uint>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<MiniCore.UI.UIWindowInstanceId,object>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<long,int>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<long,object>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<object,int>>
	// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.ICollection<long>
	// System.Collections.Generic.ICollection<object>
	// System.Collections.Generic.IComparer<MiniCore.Demo.MiniBomber.MiniBomberBattleParticipant>
	// System.Collections.Generic.IComparer<MiniCore.Demo.MiniBomber.MiniBomberSimulationEvent>
	// System.Collections.Generic.IComparer<long>
	// System.Collections.Generic.IComparer<object>
	// System.Collections.Generic.IEnumerable<MiniCore.Demo.MiniBomber.MiniBomberBattleParticipant>
	// System.Collections.Generic.IEnumerable<MiniCore.Demo.MiniBomber.MiniBomberSimulationEvent>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<MiniCore.Service.UIService.UIWindowLogicalKey,object>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<MiniCore.UI.UIWindowId,object>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<MiniCore.UI.UIWindowId,uint>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<MiniCore.UI.UIWindowInstanceId,object>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<long,int>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<long,object>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,int>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.IEnumerable<long>
	// System.Collections.Generic.IEnumerable<object>
	// System.Collections.Generic.IEnumerator<MiniCore.Demo.MiniBomber.MiniBomberBattleParticipant>
	// System.Collections.Generic.IEnumerator<MiniCore.Demo.MiniBomber.MiniBomberSimulationEvent>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<MiniCore.Service.UIService.UIWindowLogicalKey,object>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<MiniCore.UI.UIWindowId,object>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<MiniCore.UI.UIWindowId,uint>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<MiniCore.UI.UIWindowInstanceId,object>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<long,int>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<long,object>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<object,int>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.IEnumerator<long>
	// System.Collections.Generic.IEnumerator<object>
	// System.Collections.Generic.IEqualityComparer<MiniCore.Service.UIService.UIWindowLogicalKey>
	// System.Collections.Generic.IEqualityComparer<MiniCore.UI.UIWindowId>
	// System.Collections.Generic.IEqualityComparer<MiniCore.UI.UIWindowInstanceId>
	// System.Collections.Generic.IEqualityComparer<long>
	// System.Collections.Generic.IEqualityComparer<object>
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
	// System.Collections.Generic.KeyValuePair<MiniCore.Service.UIService.UIWindowLogicalKey,object>
	// System.Collections.Generic.KeyValuePair<MiniCore.UI.UIWindowId,object>
	// System.Collections.Generic.KeyValuePair<MiniCore.UI.UIWindowId,uint>
	// System.Collections.Generic.KeyValuePair<MiniCore.UI.UIWindowInstanceId,object>
	// System.Collections.Generic.KeyValuePair<long,int>
	// System.Collections.Generic.KeyValuePair<long,object>
	// System.Collections.Generic.KeyValuePair<object,int>
	// System.Collections.Generic.KeyValuePair<object,object>
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
	// System.IEquatable<MiniCore.Service.UIService.UIWindowLogicalKey>
	// System.Predicate<MiniCore.Demo.MiniBomber.MiniBomberBattleParticipant>
	// System.Predicate<MiniCore.Demo.MiniBomber.MiniBomberRoomWorkerCommand>
	// System.Predicate<MiniCore.Demo.MiniBomber.MiniBomberSimulationEvent>
	// System.Predicate<long>
	// System.Predicate<object>
	// System.ReadOnlySpan.Enumerator<byte>
	// System.ReadOnlySpan<byte>
	// System.Span.Enumerator<byte>
	// System.Span<byte>
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
		// MiniCore.Threading.MTask<object> MiniCore.Service.INetworkService.CallAsync<object,object>(string,object)
		// MiniCore.Threading.MTask MiniCore.Service.INetworkService.SendAsync<object>(string,object)
		// MiniCore.Model.NetworkSendResult MiniCore.Service.INetworkService.TrySend<object>(string,object)
		// MiniCore.Threading.MTask MiniCore.Service.ISaveService.SaveAsync<object>(string,object)
		// MiniCore.Threading.MTask<object> MiniCore.Threading.MTask.FromResult<object>(object)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MSharedTaskAwaiter<byte>,object>(MiniCore.Threading.MSharedTaskAwaiter<byte>&,object&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MSharedTaskAwaiter<object>,object>(MiniCore.Threading.MSharedTaskAwaiter<object>&,object&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,object>(MiniCore.Threading.MTaskAwaiter&,object&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<MiniCore.Demo.MiniBomber.MiniBomberRegisterResult>,object>(MiniCore.Threading.MTaskAwaiter<MiniCore.Demo.MiniBomber.MiniBomberRegisterResult>&,object&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<byte>,object>(MiniCore.Threading.MTaskAwaiter<byte>&,object&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,object>(MiniCore.Threading.MTaskAwaiter<object>&,object&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskSwitchAwaiter,object>(MiniCore.Threading.MTaskSwitchAwaiter&,object&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskYieldAwaiter,object>(MiniCore.Threading.MTaskYieldAwaiter&,object&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<MiniCore.Demo.MiniBomber.MiniBomberRegisterResult>.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,object>(MiniCore.Threading.MTaskAwaiter&,object&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<byte>.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<byte>,object>(MiniCore.Threading.MTaskAwaiter<byte>&,object&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<MiniCore.Threading.MSharedTaskAwaiter<object>,object>(MiniCore.Threading.MSharedTaskAwaiter<object>&,object&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,object>(MiniCore.Threading.MTaskAwaiter&,object&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,object>(MiniCore.Threading.MTaskAwaiter<object>&,object&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<object>(object&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<MiniCore.Demo.MiniBomber.MiniBomberRegisterResult>.Start<object>(object&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<byte>.Start<object>(object&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.Start<object>(object&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<object>(object&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<object>(object&)
		// object Newtonsoft.Json.JsonConvert.DeserializeObject<object>(string)
		// object Newtonsoft.Json.JsonConvert.DeserializeObject<object>(string,Newtonsoft.Json.JsonSerializerSettings)
		// object System.Activator.CreateInstance<object>()
		// object[] System.Array.Empty<object>()
		// int System.Array.IndexOf<int>(int[],int)
		// int System.Array.IndexOfImpl<int>(int[],int,int,int)
		// System.Void System.Array.Sort<long>(long[],int,int)
		// System.Void System.Array.Sort<long>(long[],int,int,System.Collections.Generic.IComparer<long>)
		// object System.Reflection.CustomAttributeExtensions.GetCustomAttribute<object>(System.Reflection.MemberInfo)
		// object& System.Runtime.CompilerServices.Unsafe.As<object,object>(object&)
		// System.Void* System.Runtime.CompilerServices.Unsafe.AsPointer<object>(object&)
		// object UnityEngine.Component.GetComponentInChildren<object>()
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