#define ENABLE_LOGGING
using System;
using Runtime.Core.Server;
using UnityEngine;

namespace Runtime.Helpers
{
#if UNITY_SERVER || UNITY_EDITOR
    public static class ServerLogger
    {
#if ENABLE_LOGGING
        private const ConsoleColor DefaultColor = ConsoleColor.White;
        private const ConsoleColor InfoColor = ConsoleColor.Gray;
        private const ConsoleColor WarningColor = ConsoleColor.DarkYellow;
        private const ConsoleColor ErrorColor = ConsoleColor.Red;
        private const ConsoleColor SuccessColor = ConsoleColor.Green;
#endif
        public static void Log(string message)
        {
#if ENABLE_LOGGING
#if UNITY_SERVER
            Console.ForegroundColor = InfoColor;
            Console.WriteLine(DateTime.Now.ToString("HH:mm:ss") + " " + message);
            Console.ForegroundColor = DefaultColor;
#elif UNITY_EDITOR
            if (GameServer.START_SERVER_IN_UNITY_EDITOR) Debug.Log(message);
#endif
#endif
        }

        public static void LogWarning(string message)
        {
#if ENABLE_LOGGING
#if UNITY_SERVER
            Console.ForegroundColor = WarningColor;
            Console.WriteLine(DateTime.Now.ToString("HH:mm:ss") + " Warning: " + message);
            Console.ForegroundColor = DefaultColor;

#elif UNITY_EDITOR
            if (GameServer.START_SERVER_IN_UNITY_EDITOR) Debug.LogWarning(message);
#endif
#endif
        }

        public static void LogError(string message)
        {
#if ENABLE_LOGGING
#if UNITY_SERVER
            Console.ForegroundColor = ErrorColor;
            Console.WriteLine(DateTime.Now.ToString("HH:mm:ss") + " Error: " + message);
            Console.ForegroundColor = DefaultColor;

#elif UNITY_EDITOR
            if (GameServer.START_SERVER_IN_UNITY_EDITOR) Debug.LogError(message);
#endif
#endif
        }

        public static void LogSuccess(string message)
        {
#if ENABLE_LOGGING
#if UNITY_SERVER
            Console.ForegroundColor = SuccessColor;
            Console.WriteLine(DateTime.Now.ToString("HH:mm:ss") + " Success: " + message);
            Console.ForegroundColor = DefaultColor;

#elif UNITY_EDITOR
            if (GameServer.START_SERVER_IN_UNITY_EDITOR) Debug.Log(message);
#endif
#endif
        }
    }
#endif
}