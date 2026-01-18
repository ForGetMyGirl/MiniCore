using UnityEngine;

namespace MiniCore.Model
{
    public static class LogSwitch
    {
        public static bool EnableLog = false;
        public static bool EnablePayloadLog = false;

        public static void Info(string message)
        {
            if (!EnableLog) return;
            EventCenter.Broadcast(GameEvent.LogInfo, message);
            Debug.Log(message);
        }

        public static void Warning(string message)
        {
            if (!EnableLog) return;
            EventCenter.Broadcast(GameEvent.LogWarning, message);
            Debug.LogWarning(message);
        }

        public static void Error(string message)
        {
            if (!EnableLog) return;
            EventCenter.Broadcast(GameEvent.LogError, message);
            Debug.LogError(message);
        }
    }
}
