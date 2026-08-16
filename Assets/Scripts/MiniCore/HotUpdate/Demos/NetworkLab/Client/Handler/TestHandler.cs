using MiniCore.Threading;
using UnityEngine;
using MiniCore.Model;
using MiniCore.Protocol.Generated;
using MiniCore.Core;

namespace MiniCore.HotUpdate
{
    /// <summary>
    /// 示例：处理 TestNetworkData 消息。
    /// 运行 Opcode 生成器后会为 TestNetworkData 分配 opcode，NetworkService 会自动绑定。
    /// </summary>
    public class TestHandler : AMHandler<TestNetworkData>
    {
        /// <summary>
        /// 处理并记录测试网络消息。
        /// </summary>
        /// <param name="session">执行该方法所需的 session 参数。</param>
        /// <param name="message">执行该方法所需的 message 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public override async MTask HandleAsync(NetworkSession session, TestNetworkData message)
        {
            LogSwitch.Info($"[TestHandler] 收到消息 -> Id:{message.Id}, Content:{message.Content}");
            await MTask.CompletedTask;
        }
    }
}
