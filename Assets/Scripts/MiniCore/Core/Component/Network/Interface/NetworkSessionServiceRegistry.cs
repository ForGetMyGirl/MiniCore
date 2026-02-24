using System;

namespace MiniCore.Core
{
    public static class NetworkSessionServiceRegistry
    {
        private static readonly object SyncRoot = new object();
        private static Func<INetworkSessionService> resolver;

        public static void RegisterResolver(Func<INetworkSessionService> serviceResolver)
        {
            lock (SyncRoot)
            {
                resolver = serviceResolver;
            }
        }

        public static void ClearResolver()
        {
            lock (SyncRoot)
            {
                resolver = null;
            }
        }

        public static bool TryResolve(out INetworkSessionService service)
        {
            Func<INetworkSessionService> current;
            lock (SyncRoot)
            {
                current = resolver;
            }

            if (current == null)
            {
                service = null;
                return false;
            }

            service = current.Invoke();
            return service != null;
        }
    }
}
