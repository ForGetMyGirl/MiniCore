using Cysharp.Threading.Tasks;
using UnityEngine;
using MiniCore.Model;
using MiniCore.Core;

namespace MiniCore.HotUpdate
{
    /// <summary>
    /// 示例：处理 TestNetworkData 消息。
    /// 运行 Opcode 生成器后会为 TestNetworkData 分配 opcode，NetworkMessageComponent 会自动绑定。
    /// </summary>
    public class TestHandler : AMHandler<TestNetworkData>
    {
        public override async UniTask HandleAsync(NetworkSession session, TestNetworkData message)
        {
            EventCenter.Broadcast(GameEvent.LogInfo, $"[TestHandler] 收到消息 -> Id:{message.Id}, Content:{message.Content}");
            await UniTask.CompletedTask;
        }
    }
}