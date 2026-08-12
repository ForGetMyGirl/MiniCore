using System;
using System.Collections.Generic;
using MiniCore.Model;
using MiniCore.Service;
using MiniCore.Threading;

namespace MiniCore.Eventing
{

    internal interface IEventSubscriptionOwner
    {
        void RemoveSubscription(int slotId, uint generation, byte kind);
    }
}
