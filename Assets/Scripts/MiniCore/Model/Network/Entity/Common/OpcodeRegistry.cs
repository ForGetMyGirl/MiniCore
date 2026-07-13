using System;
using System.Collections.Generic;

namespace MiniCore.Model
{
    /// <summary>
    /// 由生成的分部类填充的 opcode 注册表，维护处理器和协议类型映射。
    /// </summary>
    public static partial class OpcodeRegistry
    {
        private class HandlerInfo
        {
            /// <summary>
            /// 网络模块公开成员 HandlerType 的说明。
            /// </summary>
            public string HandlerType;
            public string RequestType;
            public string ResponseType;
            public bool IsRpc;
        }

        private static readonly Dictionary<string, uint> HandlerToOpcode = new Dictionary<string, uint>(); // 处理器类型到 opcode 映射。
        private static readonly Dictionary<uint, HandlerInfo> OpcodeToHandler = new Dictionary<uint, HandlerInfo>(); // opcode 到处理器元数据映射。
        private static readonly Dictionary<string, uint> MessageToOpcode = new Dictionary<string, uint>(); // 协议类型到 opcode 映射。

        static OpcodeRegistry()
        {
            HandlerToOpcode.Clear();
            OpcodeToHandler.Clear();
            MessageToOpcode.Clear();
            RegisterGenerated(HandlerToOpcode, OpcodeToHandler, MessageToOpcode);
        }

        /// <summary>
        /// 尝试按处理器类型获取其注册 opcode。
        /// </summary>
        /// <param name="handlerType">执行该方法所需的 handlerType 参数。</param>
        /// <param name="opcode">执行该方法所需的 opcode 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public static bool TryGetOpcodeByHandler(Type handlerType, out uint opcode)
        {
            if (handlerType == null)
            {
                opcode = 0;
                return false;
            }
            return HandlerToOpcode.TryGetValue(handlerType.FullName, out opcode);
        }

        /// <summary>
        /// 尝试按协议类型获取其注册 opcode。
        /// </summary>
        /// <param name="msgType">执行该方法所需的 msgType 参数。</param>
        /// <param name="opcode">执行该方法所需的 opcode 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public static bool TryGetOpcodeByMessage(Type msgType, out uint opcode)
        {
            if (msgType == null)
            {
                opcode = 0;
                return false;
            }
            return MessageToOpcode.TryGetValue(msgType.FullName, out opcode);
        }

        /// <summary>
        /// 尝试按 opcode 获取处理器、请求、响应类型及 RPC 标记。
        /// </summary>
        /// <param name="opcode">执行该方法所需的 opcode 参数。</param>
        /// <param name="handlerType">执行该方法所需的 handlerType 参数。</param>
        /// <param name="requestType">执行该方法所需的 requestType 参数。</param>
        /// <param name="responseType">执行该方法所需的 responseType 参数。</param>
        /// <param name="isRpc">执行该方法所需的 isRpc 参数。</param>
        /// <returns>执行处理后的结果。</returns>
        public static bool TryGetHandlerInfo(uint opcode, out string handlerType, out string requestType, out string responseType, out bool isRpc)
        {
            if (OpcodeToHandler.TryGetValue(opcode, out var info))
            {
                handlerType = info.HandlerType;
                requestType = info.RequestType;
                responseType = info.ResponseType;
                isRpc = info.IsRpc;
                return true;
            }
            handlerType = requestType = responseType = null;
            isRpc = false;
            return false;
        }

        /// <summary>
        /// 由生成的分部文件实现，用于填充 opcode 映射。
        /// </summary>
        static partial void RegisterGenerated(Dictionary<string, uint> handlerToOpcode, Dictionary<uint, HandlerInfo> opcodeToHandler, Dictionary<string, uint> messageToOpcode);
    }
}
