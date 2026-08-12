using System;
using System.Collections.Generic;
using System.Threading;
using Google.Protobuf;
using MiniCore.Protocol.Generated;
using MiniCore.Threading;

namespace MiniCore.Demo.MiniBomber
{

    /// <summary>
    /// RoomWorker 命令类型。
    /// </summary>
    internal enum MiniBomberRoomWorkerCommandType
    {
        Create,
        Start,
        Input,
        Online,
        Keyframe,
        Remove,
        Tick
    }
}
