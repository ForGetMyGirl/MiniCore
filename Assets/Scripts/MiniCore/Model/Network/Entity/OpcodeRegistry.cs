using System;
using System.Collections.Generic;

namespace MiniCore.Model
{
    /// <summary>
    /// Central opcode registry fed by the generated partial class.
    /// Maps handler types and protocol types to their opcodes.
    /// </summary>
    public static partial class OpcodeRegistry
    {
        private class HandlerInfo
        {
            public string HandlerType;
            public string RequestType;
            public string ResponseType;
            public bool IsRpc;
        }

        private static readonly Dictionary<string, uint> HandlerToOpcode = new Dictionary<string, uint>();
        private static readonly Dictionary<uint, HandlerInfo> OpcodeToHandler = new Dictionary<uint, HandlerInfo>();
        private static readonly Dictionary<string, uint> MessageToOpcode = new Dictionary<string, uint>();

        static OpcodeRegistry()
        {
            HandlerToOpcode.Clear();
            OpcodeToHandler.Clear();
            MessageToOpcode.Clear();
            RegisterGenerated(HandlerToOpcode, OpcodeToHandler, MessageToOpcode);
        }

        public static bool TryGetOpcodeByHandler(Type handlerType, out uint opcode)
        {
            if (handlerType == null)
            {
                opcode = 0;
                return false;
            }
            return HandlerToOpcode.TryGetValue(handlerType.FullName, out opcode);
        }

        public static bool TryGetOpcodeByMessage(Type msgType, out uint opcode)
        {
            if (msgType == null)
            {
                opcode = 0;
                return false;
            }
            return MessageToOpcode.TryGetValue(msgType.FullName, out opcode);
        }

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
        /// Implemented in the generated partial file.
        /// </summary>
        static partial void RegisterGenerated(Dictionary<string, uint> handlerToOpcode, Dictionary<uint, HandlerInfo> opcodeToHandler, Dictionary<string, uint> messageToOpcode);
    }
}
