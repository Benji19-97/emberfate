using System;

namespace Runtime
{
    public static class ServerLogger
    {
        public enum LogType
        {
            Message = ConsoleColor.White,
            Info  = ConsoleColor.DarkYellow,
            Error = ConsoleColor.Red,
            Success = ConsoleColor.Green
        }
        
        public static void LogMessage(string message, LogType type)
        {
            Console.ForegroundColor = (ConsoleColor) type;
            Console.WriteLine(DateTime.Now.ToString("HH:mm:ss") + " " + message);
            Console.ForegroundColor = ConsoleColor.Gray;
        }
    }
}