using System;
using UnityEngine;

namespace Runtime
{
    public static class ServerLogger
    {
#if UNITY_SERVER || UNITY_EDITOR
        public enum LogType
        {
            Message = ConsoleColor.White,
            Info = ConsoleColor.DarkYellow,
            Error = ConsoleColor.Red,
            Success = ConsoleColor.Green
        }

        public static void LogMessage(string message, LogType type)
        {
#if UNITY_SERVER
            Console.ForegroundColor = (ConsoleColor) type;
            Console.WriteLine(DateTime.Now.ToString("HH:mm:ss") + " " + message);
            Console.ForegroundColor = ConsoleColor.Gray;
#endif
#if UNITY_EDITOR
            if (GameServer.START_SERVER_IN_UNITY_EDITOR)
            {
                Debug.Log(message);
            }
#endif
        }
#endif
    }
}