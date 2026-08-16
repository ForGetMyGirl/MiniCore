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
		"System.Core.dll",
		"mscorlib.dll",
	};
	// }}

	// {{ constraint implement type
	// }} 

	// {{ AOT generic types
	// Google.Protobuf.Collections.RepeatedField.<GetEnumerator>d__28<long>
	// Google.Protobuf.Collections.RepeatedField.<GetEnumerator>d__28<object>
	// Google.Protobuf.Collections.RepeatedField<long>
	// Google.Protobuf.Collections.RepeatedField<object>
	// Google.Protobuf.FieldCodec.<>c<long>
	// Google.Protobuf.FieldCodec.<>c<object>
	// Google.Protobuf.FieldCodec.<>c__DisplayClass38_0<long>
	// Google.Protobuf.FieldCodec.<>c__DisplayClass38_0<object>
	// Google.Protobuf.FieldCodec.<>c__DisplayClass39_0<long>
	// Google.Protobuf.FieldCodec.<>c__DisplayClass39_0<object>
	// Google.Protobuf.FieldCodec.InputMerger<long>
	// Google.Protobuf.FieldCodec.InputMerger<object>
	// Google.Protobuf.FieldCodec.ValuesMerger<long>
	// Google.Protobuf.FieldCodec.ValuesMerger<object>
	// Google.Protobuf.FieldCodec<long>
	// Google.Protobuf.FieldCodec<object>
	// Google.Protobuf.IDeepCloneable<long>
	// Google.Protobuf.IDeepCloneable<object>
	// Google.Protobuf.IMessage<object>
	// Google.Protobuf.MessageParser.<>c__DisplayClass2_0<object>
	// Google.Protobuf.MessageParser<object>
	// Google.Protobuf.ValueReader<long>
	// Google.Protobuf.ValueReader<object>
	// Google.Protobuf.ValueWriter<long>
	// Google.Protobuf.ValueWriter<object>
	// MiniCore.Model.AMHandler<object>
	// MiniCore.Model.ARpcHandler<object,object>
	// MiniCore.Threading.IMTaskSource<byte>
	// MiniCore.Threading.IMTaskSource<object>
	// MiniCore.Threading.MSharedTask<byte>
	// MiniCore.Threading.MSharedTask<object>
	// MiniCore.Threading.MSharedTaskAwaiter<byte>
	// MiniCore.Threading.MSharedTaskAwaiter<object>
	// MiniCore.Threading.MSharedTaskWaiter<byte>
	// MiniCore.Threading.MSharedTaskWaiter<object>
	// MiniCore.Threading.MTask<byte>
	// MiniCore.Threading.MTask<object>
	// MiniCore.Threading.MTaskAwaiter<byte>
	// MiniCore.Threading.MTaskAwaiter<object>
	// MiniCore.Threading.MTaskForgetObserver<byte>
	// MiniCore.Threading.MTaskForgetObserver<object>
	// MiniCore.Threading.MTaskMethodBuilder<byte>
	// MiniCore.Threading.MTaskMethodBuilder<object>
	// MiniCore.Threading.MTaskPromise<byte>
	// MiniCore.Threading.MTaskPromise<object>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.MiniBomberDatabaseComponent.<ConnectDatabaseAsync>d__11>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.MiniBomberDatabaseComponent.<EnsureConnectedAsync>d__10>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.MiniBomberDatabaseComponent.<InitializeAsync>d__7>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.MiniBomberDatabaseComponent.<LoadOrCreateAsync>d__8>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.MiniBomberDatabaseComponent.<LoadWithRecoveryAsync>d__12>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.MiniBomberDatabaseComponent.<ResolveUnknownSaveResultAsync>d__15>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.MiniBomberDedicatedServerApplication.<StartAsync>d__1>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.MiniBomberResumeSessionHandler.<HandleAsync>d__0>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.MiniBomberServerRuntimeComponent.<InitializeAsync>d__34>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.MiniBomberServerRuntimeComponent.<ResumeSessionAsync>d__35>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.MiniBomberServerStartupComponent.<InitializeAsync>d__0>
	// MiniCore.Threading.MTaskStateMachineRunner<MiniCore.Demo.MiniBomber.MiniBomberStartupComponentBase.<LoadConfigurationAsync>d__16>
	// System.Action<MiniCore.Demo.MiniBomber.MiniBomberBattleParticipant>
	// System.Action<MiniCore.Demo.MiniBomber.MiniBomberSimulationEvent>
	// System.Action<long>
	// System.Action<object>
	// System.ArraySegment.Enumerator<byte>
	// System.ArraySegment<byte>
	// System.Buffers.MemoryManager<byte>
	// System.ByReference<byte>
	// System.Collections.Generic.ArraySortHelper<MiniCore.Demo.MiniBomber.MiniBomberBattleParticipant>
	// System.Collections.Generic.ArraySortHelper<MiniCore.Demo.MiniBomber.MiniBomberSimulationEvent>
	// System.Collections.Generic.ArraySortHelper<long>
	// System.Collections.Generic.ArraySortHelper<object>
	// System.Collections.Generic.Comparer<MiniCore.Demo.MiniBomber.MiniBomberBattleParticipant>
	// System.Collections.Generic.Comparer<MiniCore.Demo.MiniBomber.MiniBomberSimulationEvent>
	// System.Collections.Generic.Comparer<long>
	// System.Collections.Generic.Comparer<object>
	// System.Collections.Generic.Dictionary.Enumerator<long,int>
	// System.Collections.Generic.Dictionary.Enumerator<long,object>
	// System.Collections.Generic.Dictionary.Enumerator<object,int>
	// System.Collections.Generic.Dictionary.Enumerator<object,object>
	// System.Collections.Generic.Dictionary.Enumerator<uint,object>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<long,int>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<long,object>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<object,int>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<object,object>
	// System.Collections.Generic.Dictionary.KeyCollection.Enumerator<uint,object>
	// System.Collections.Generic.Dictionary.KeyCollection<long,int>
	// System.Collections.Generic.Dictionary.KeyCollection<long,object>
	// System.Collections.Generic.Dictionary.KeyCollection<object,int>
	// System.Collections.Generic.Dictionary.KeyCollection<object,object>
	// System.Collections.Generic.Dictionary.KeyCollection<uint,object>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<long,int>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<long,object>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<object,int>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<object,object>
	// System.Collections.Generic.Dictionary.ValueCollection.Enumerator<uint,object>
	// System.Collections.Generic.Dictionary.ValueCollection<long,int>
	// System.Collections.Generic.Dictionary.ValueCollection<long,object>
	// System.Collections.Generic.Dictionary.ValueCollection<object,int>
	// System.Collections.Generic.Dictionary.ValueCollection<object,object>
	// System.Collections.Generic.Dictionary.ValueCollection<uint,object>
	// System.Collections.Generic.Dictionary<long,int>
	// System.Collections.Generic.Dictionary<long,object>
	// System.Collections.Generic.Dictionary<object,int>
	// System.Collections.Generic.Dictionary<object,object>
	// System.Collections.Generic.Dictionary<uint,object>
	// System.Collections.Generic.EqualityComparer<int>
	// System.Collections.Generic.EqualityComparer<long>
	// System.Collections.Generic.EqualityComparer<object>
	// System.Collections.Generic.EqualityComparer<uint>
	// System.Collections.Generic.HashSet.Enumerator<long>
	// System.Collections.Generic.HashSet<long>
	// System.Collections.Generic.HashSetEqualityComparer<long>
	// System.Collections.Generic.ICollection<MiniCore.Demo.MiniBomber.MiniBomberBattleParticipant>
	// System.Collections.Generic.ICollection<MiniCore.Demo.MiniBomber.MiniBomberSimulationEvent>
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
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<long,int>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<long,object>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,int>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<uint,object>>
	// System.Collections.Generic.IEnumerable<long>
	// System.Collections.Generic.IEnumerable<object>
	// System.Collections.Generic.IEnumerator<MiniCore.Demo.MiniBomber.MiniBomberBattleParticipant>
	// System.Collections.Generic.IEnumerator<MiniCore.Demo.MiniBomber.MiniBomberSimulationEvent>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<long,int>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<long,object>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<object,int>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<object,object>>
	// System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<uint,object>>
	// System.Collections.Generic.IEnumerator<long>
	// System.Collections.Generic.IEnumerator<object>
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
	// System.Func<long,int>
	// System.Func<object,int>
	// System.Func<object,object,object>
	// System.Func<object,object>
	// System.Func<object>
	// System.IEquatable<object>
	// System.Memory<byte>
	// System.Predicate<MiniCore.Demo.MiniBomber.MiniBomberBattleParticipant>
	// System.Predicate<MiniCore.Demo.MiniBomber.MiniBomberRoomWorkerCommand>
	// System.Predicate<MiniCore.Demo.MiniBomber.MiniBomberSimulationEvent>
	// System.Predicate<long>
	// System.Predicate<object>
	// System.ReadOnlyMemory<byte>
	// System.ReadOnlySpan<byte>
	// System.Span<byte>
	// }}

	public void RefMethods()
	{
		// object MiniCore.Core.Global.Get<object>(object)
		// object MiniCore.Core.Global.GetOrAdd<object>(object)
		// object MiniCore.Core.Global.GetOrAddModule<object>(object,MiniCore.Model.ComponentInitArgs)
		// object MiniCore.Core.Global.GetOrAddModule<object>(string,object,MiniCore.Model.ComponentInitArgs)
		// object MiniCore.Core.Global.GetService<object>(object)
		// object MiniCore.Core.Global.Pin<object>()
		// System.Void MiniCore.Core.Global.ThrowIfDirectAppServiceAccess<object>()
		// object MiniCore.Core.GlobalModuleRegistry.GetOrAdd<object>(string,object,MiniCore.Model.ComponentInitArgs)
		// object MiniCore.Core.GlobalRuntime.Get<object>(object)
		// object MiniCore.Core.GlobalRuntime.GetOrAdd<object>(object)
		// object MiniCore.Core.GlobalRuntime.GetOrCreate<object>(object,MiniCore.Model.ComponentInitArgs)
		// object MiniCore.Core.GlobalRuntime.Pin<object>()
		// object MiniCore.Core.GlobalRuntime.PinInternal<object>(MiniCore.Model.ComponentInitArgs)
		// object MiniCore.Core.GlobalServiceRegistry.Get<object>(object)
		// System.Void MiniCore.Eventing.IEventBus.Publish<object>(object)
		// object MiniCore.Model.AComponent.AddComponent<object>()
		// System.Void MiniCore.Model.NetworkProtocolBuilder.RegisterMessage<object>(uint,MiniCore.Model.NetworkMessageRole,MiniCore.Serialization.IMessageParser)
		// MiniCore.Threading.MTask<object> MiniCore.Service.INetworkService.CallAsync<object,object>(string,object,int)
		// MiniCore.Model.NetworkSendResult MiniCore.Service.INetworkService.TrySend<object>(string,object)
		// MiniCore.Threading.MTask<object> MiniCore.Service.IResourceService.PreloadAssetAsync<object>(string)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.Demo.MiniBomber.MiniBomberDedicatedServerApplication.<StartAsync>d__1>(MiniCore.Threading.MTaskAwaiter&,MiniCore.Demo.MiniBomber.MiniBomberDedicatedServerApplication.<StartAsync>d__1&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.Demo.MiniBomber.MiniBomberResumeSessionHandler.<HandleAsync>d__0>(MiniCore.Threading.MTaskAwaiter&,MiniCore.Demo.MiniBomber.MiniBomberResumeSessionHandler.<HandleAsync>d__0&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.Demo.MiniBomber.MiniBomberServerRuntimeComponent.<InitializeAsync>d__34>(MiniCore.Threading.MTaskAwaiter&,MiniCore.Demo.MiniBomber.MiniBomberServerRuntimeComponent.<InitializeAsync>d__34&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter,MiniCore.Demo.MiniBomber.MiniBomberServerStartupComponent.<InitializeAsync>d__0>(MiniCore.Threading.MTaskAwaiter&,MiniCore.Demo.MiniBomber.MiniBomberServerStartupComponent.<InitializeAsync>d__0&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<byte>,MiniCore.Demo.MiniBomber.MiniBomberDatabaseComponent.<InitializeAsync>d__7>(MiniCore.Threading.MTaskAwaiter<byte>&,MiniCore.Demo.MiniBomber.MiniBomberDatabaseComponent.<InitializeAsync>d__7&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Demo.MiniBomber.MiniBomberServerRuntimeComponent.<ResumeSessionAsync>d__35>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Demo.MiniBomber.MiniBomberServerRuntimeComponent.<ResumeSessionAsync>d__35&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Demo.MiniBomber.MiniBomberStartupComponentBase.<LoadConfigurationAsync>d__16>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Demo.MiniBomber.MiniBomberStartupComponentBase.<LoadConfigurationAsync>d__16&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<byte>.AwaitUnsafeOnCompleted<MiniCore.Threading.MSharedTaskAwaiter<byte>,MiniCore.Demo.MiniBomber.MiniBomberDatabaseComponent.<EnsureConnectedAsync>d__10>(MiniCore.Threading.MSharedTaskAwaiter<byte>&,MiniCore.Demo.MiniBomber.MiniBomberDatabaseComponent.<EnsureConnectedAsync>d__10&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<byte>.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<byte>,MiniCore.Demo.MiniBomber.MiniBomberDatabaseComponent.<ConnectDatabaseAsync>d__11>(MiniCore.Threading.MTaskAwaiter<byte>&,MiniCore.Demo.MiniBomber.MiniBomberDatabaseComponent.<ConnectDatabaseAsync>d__11&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<byte>,MiniCore.Demo.MiniBomber.MiniBomberDatabaseComponent.<LoadOrCreateAsync>d__8>(MiniCore.Threading.MTaskAwaiter<byte>&,MiniCore.Demo.MiniBomber.MiniBomberDatabaseComponent.<LoadOrCreateAsync>d__8&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<byte>,MiniCore.Demo.MiniBomber.MiniBomberDatabaseComponent.<LoadWithRecoveryAsync>d__12>(MiniCore.Threading.MTaskAwaiter<byte>&,MiniCore.Demo.MiniBomber.MiniBomberDatabaseComponent.<LoadWithRecoveryAsync>d__12&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<byte>,MiniCore.Demo.MiniBomber.MiniBomberDatabaseComponent.<ResolveUnknownSaveResultAsync>d__15>(MiniCore.Threading.MTaskAwaiter<byte>&,MiniCore.Demo.MiniBomber.MiniBomberDatabaseComponent.<ResolveUnknownSaveResultAsync>d__15&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Demo.MiniBomber.MiniBomberDatabaseComponent.<LoadOrCreateAsync>d__8>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Demo.MiniBomber.MiniBomberDatabaseComponent.<LoadOrCreateAsync>d__8&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Demo.MiniBomber.MiniBomberDatabaseComponent.<LoadWithRecoveryAsync>d__12>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Demo.MiniBomber.MiniBomberDatabaseComponent.<LoadWithRecoveryAsync>d__12&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.AwaitUnsafeOnCompleted<MiniCore.Threading.MTaskAwaiter<object>,MiniCore.Demo.MiniBomber.MiniBomberDatabaseComponent.<ResolveUnknownSaveResultAsync>d__15>(MiniCore.Threading.MTaskAwaiter<object>&,MiniCore.Demo.MiniBomber.MiniBomberDatabaseComponent.<ResolveUnknownSaveResultAsync>d__15&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.MiniBomberDatabaseComponent.<InitializeAsync>d__7>(MiniCore.Demo.MiniBomber.MiniBomberDatabaseComponent.<InitializeAsync>d__7&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.MiniBomberDedicatedServerApplication.<StartAsync>d__1>(MiniCore.Demo.MiniBomber.MiniBomberDedicatedServerApplication.<StartAsync>d__1&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.MiniBomberResumeSessionHandler.<HandleAsync>d__0>(MiniCore.Demo.MiniBomber.MiniBomberResumeSessionHandler.<HandleAsync>d__0&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.MiniBomberServerRuntimeComponent.<InitializeAsync>d__34>(MiniCore.Demo.MiniBomber.MiniBomberServerRuntimeComponent.<InitializeAsync>d__34&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.MiniBomberServerRuntimeComponent.<ResumeSessionAsync>d__35>(MiniCore.Demo.MiniBomber.MiniBomberServerRuntimeComponent.<ResumeSessionAsync>d__35&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.MiniBomberServerStartupComponent.<InitializeAsync>d__0>(MiniCore.Demo.MiniBomber.MiniBomberServerStartupComponent.<InitializeAsync>d__0&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder.Start<MiniCore.Demo.MiniBomber.MiniBomberStartupComponentBase.<LoadConfigurationAsync>d__16>(MiniCore.Demo.MiniBomber.MiniBomberStartupComponentBase.<LoadConfigurationAsync>d__16&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<byte>.Start<MiniCore.Demo.MiniBomber.MiniBomberDatabaseComponent.<ConnectDatabaseAsync>d__11>(MiniCore.Demo.MiniBomber.MiniBomberDatabaseComponent.<ConnectDatabaseAsync>d__11&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<byte>.Start<MiniCore.Demo.MiniBomber.MiniBomberDatabaseComponent.<EnsureConnectedAsync>d__10>(MiniCore.Demo.MiniBomber.MiniBomberDatabaseComponent.<EnsureConnectedAsync>d__10&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.Start<MiniCore.Demo.MiniBomber.MiniBomberDatabaseComponent.<LoadOrCreateAsync>d__8>(MiniCore.Demo.MiniBomber.MiniBomberDatabaseComponent.<LoadOrCreateAsync>d__8&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.Start<MiniCore.Demo.MiniBomber.MiniBomberDatabaseComponent.<LoadWithRecoveryAsync>d__12>(MiniCore.Demo.MiniBomber.MiniBomberDatabaseComponent.<LoadWithRecoveryAsync>d__12&)
		// System.Void MiniCore.Threading.MTaskMethodBuilder<object>.Start<MiniCore.Demo.MiniBomber.MiniBomberDatabaseComponent.<ResolveUnknownSaveResultAsync>d__15>(MiniCore.Demo.MiniBomber.MiniBomberDatabaseComponent.<ResolveUnknownSaveResultAsync>d__15&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.MiniBomberDatabaseComponent.<ConnectDatabaseAsync>d__11>(MiniCore.Demo.MiniBomber.MiniBomberDatabaseComponent.<ConnectDatabaseAsync>d__11&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.MiniBomberDatabaseComponent.<EnsureConnectedAsync>d__10>(MiniCore.Demo.MiniBomber.MiniBomberDatabaseComponent.<EnsureConnectedAsync>d__10&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.MiniBomberDatabaseComponent.<InitializeAsync>d__7>(MiniCore.Demo.MiniBomber.MiniBomberDatabaseComponent.<InitializeAsync>d__7&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.MiniBomberDatabaseComponent.<LoadOrCreateAsync>d__8>(MiniCore.Demo.MiniBomber.MiniBomberDatabaseComponent.<LoadOrCreateAsync>d__8&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.MiniBomberDatabaseComponent.<LoadWithRecoveryAsync>d__12>(MiniCore.Demo.MiniBomber.MiniBomberDatabaseComponent.<LoadWithRecoveryAsync>d__12&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.MiniBomberDatabaseComponent.<ResolveUnknownSaveResultAsync>d__15>(MiniCore.Demo.MiniBomber.MiniBomberDatabaseComponent.<ResolveUnknownSaveResultAsync>d__15&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.MiniBomberDedicatedServerApplication.<StartAsync>d__1>(MiniCore.Demo.MiniBomber.MiniBomberDedicatedServerApplication.<StartAsync>d__1&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.MiniBomberResumeSessionHandler.<HandleAsync>d__0>(MiniCore.Demo.MiniBomber.MiniBomberResumeSessionHandler.<HandleAsync>d__0&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.MiniBomberServerRuntimeComponent.<InitializeAsync>d__34>(MiniCore.Demo.MiniBomber.MiniBomberServerRuntimeComponent.<InitializeAsync>d__34&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.MiniBomberServerRuntimeComponent.<ResumeSessionAsync>d__35>(MiniCore.Demo.MiniBomber.MiniBomberServerRuntimeComponent.<ResumeSessionAsync>d__35&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.MiniBomberServerStartupComponent.<InitializeAsync>d__0>(MiniCore.Demo.MiniBomber.MiniBomberServerStartupComponent.<InitializeAsync>d__0&)
		// System.Action MiniCore.Threading.MTaskPromiseBase.GetStateMachineContinuation<MiniCore.Demo.MiniBomber.MiniBomberStartupComponentBase.<LoadConfigurationAsync>d__16>(MiniCore.Demo.MiniBomber.MiniBomberStartupComponentBase.<LoadConfigurationAsync>d__16&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.MiniBomberDatabaseComponent.<ConnectDatabaseAsync>d__11>(MiniCore.Demo.MiniBomber.MiniBomberDatabaseComponent.<ConnectDatabaseAsync>d__11&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.MiniBomberDatabaseComponent.<EnsureConnectedAsync>d__10>(MiniCore.Demo.MiniBomber.MiniBomberDatabaseComponent.<EnsureConnectedAsync>d__10&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.MiniBomberDatabaseComponent.<InitializeAsync>d__7>(MiniCore.Demo.MiniBomber.MiniBomberDatabaseComponent.<InitializeAsync>d__7&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.MiniBomberDatabaseComponent.<LoadOrCreateAsync>d__8>(MiniCore.Demo.MiniBomber.MiniBomberDatabaseComponent.<LoadOrCreateAsync>d__8&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.MiniBomberDatabaseComponent.<LoadWithRecoveryAsync>d__12>(MiniCore.Demo.MiniBomber.MiniBomberDatabaseComponent.<LoadWithRecoveryAsync>d__12&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.MiniBomberDatabaseComponent.<ResolveUnknownSaveResultAsync>d__15>(MiniCore.Demo.MiniBomber.MiniBomberDatabaseComponent.<ResolveUnknownSaveResultAsync>d__15&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.MiniBomberDedicatedServerApplication.<StartAsync>d__1>(MiniCore.Demo.MiniBomber.MiniBomberDedicatedServerApplication.<StartAsync>d__1&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.MiniBomberResumeSessionHandler.<HandleAsync>d__0>(MiniCore.Demo.MiniBomber.MiniBomberResumeSessionHandler.<HandleAsync>d__0&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.MiniBomberServerRuntimeComponent.<InitializeAsync>d__34>(MiniCore.Demo.MiniBomber.MiniBomberServerRuntimeComponent.<InitializeAsync>d__34&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.MiniBomberServerRuntimeComponent.<ResumeSessionAsync>d__35>(MiniCore.Demo.MiniBomber.MiniBomberServerRuntimeComponent.<ResumeSessionAsync>d__35&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.MiniBomberServerStartupComponent.<InitializeAsync>d__0>(MiniCore.Demo.MiniBomber.MiniBomberServerStartupComponent.<InitializeAsync>d__0&)
		// System.Void MiniCore.Threading.MTaskPromiseBase.Start<MiniCore.Demo.MiniBomber.MiniBomberStartupComponentBase.<LoadConfigurationAsync>d__16>(MiniCore.Demo.MiniBomber.MiniBomberStartupComponentBase.<LoadConfigurationAsync>d__16&)
		// object System.Activator.CreateInstance<object>()
		// UnityEngine.Vector2Int[] System.Array.Empty<UnityEngine.Vector2Int>()
	}
}