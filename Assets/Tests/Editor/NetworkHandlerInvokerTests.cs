using Cysharp.Threading.Tasks;
using MiniCore.Model;
using NUnit.Framework;

namespace MiniCore.EditorTests
{
    /// <summary>验证泛型 Handler 的无反射运行时派发适配器。</summary>
    public sealed class NetworkHandlerInvokerTests
    {
        /// <summary>普通消息适配器应直接调用具体 Handler。</summary>
        [Test]
        public void NormalHandlerInvoker_DispatchesTypedMessage()
        {
            var handler = new TestMessageHandler();
            var message = new TestMessage { Value = 7 };

            ((INetworkMessageHandlerInvoker)handler).HandleAsync(null, message).GetAwaiter().GetResult();

            Assert.AreSame(message, handler.ReceivedMessage);
        }

        /// <summary>RPC 适配器应创建强类型响应并直接调用具体 Handler。</summary>
        [Test]
        public void RpcHandlerInvoker_CreatesAndDispatchesTypedResponse()
        {
            var handler = new TestRpcHandler();
            var request = new TestRequest { RpcId = 9 };
            IRpcResponse response = ((INetworkRpcHandlerInvoker)handler).CreateResponse();

            ((INetworkRpcHandlerInvoker)handler).HandleAsync(null, request, response).GetAwaiter().GetResult();

            Assert.IsInstanceOf<TestResponse>(response);
            Assert.AreSame(request, handler.ReceivedRequest);
            Assert.AreEqual(9, response.RpcId);
            Assert.AreEqual("handled", response.Msg);
        }

        private sealed class TestMessageHandler : AMHandler<TestMessage>
        {
            /// <summary>最近一次由适配器派发的测试消息。</summary>
            public TestMessage ReceivedMessage { get; private set; }

            /// <summary>记录测试消息以验证无反射派发结果。</summary>
            public override UniTask HandleAsync(NetworkSession session, TestMessage message)
            {
                ReceivedMessage = message;
                return UniTask.CompletedTask;
            }
        }

        private sealed class TestRpcHandler : ARpcHandler<TestRequest, TestResponse>
        {
            /// <summary>最近一次由适配器派发的测试请求。</summary>
            public TestRequest ReceivedRequest { get; private set; }

            /// <summary>记录请求并填充测试响应。</summary>
            public override UniTask HandleAsync(NetworkSession session, TestRequest request, TestResponse response)
            {
                ReceivedRequest = request;
                response.RpcId = request.RpcId;
                response.Msg = "handled";
                return UniTask.CompletedTask;
            }
        }

        private sealed class TestMessage : INormalMessage
        {
            /// <summary>用于断言派发结果的测试值。</summary>
            public int Value { get; set; }
        }

        private sealed class TestRequest : IRpcRequest
        {
            /// <summary>测试 RPC 请求标识。</summary>
            public long RpcId { get; set; }
        }

        private sealed class TestResponse : IRpcResponse
        {
            /// <summary>创建满足 RPC Handler 构造约束的测试响应。</summary>
            public TestResponse()
            {
            }

            /// <summary>与测试请求对应的标识。</summary>
            public long RpcId { get; set; }
            /// <summary>测试业务错误码。</summary>
            public int Code { get; set; }
            /// <summary>测试业务结果文本。</summary>
            public string Msg { get; set; }
        }
    }
}
