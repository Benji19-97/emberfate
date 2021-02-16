// ReSharper disable InconsistentNaming
namespace Runtime.Endpoints
{
    public static class PathRegister
    {
        public const string Server_ConfigPath = "config.json";
        public const string Server_PasswordPath = "password.txt";
        public const string Server_SteamWebApiKeyPath = "web_api_key.txt";
        public const string SteamAppIdPath = "steam_appid.txt";

        public const string Server_ConfigPath_UnityEditor = "./Build/Server/config.json";
        public const string Server_PasswordPath_UnityEditor = "./Build/Server/password.txt";
        public const string Server_SteamWebApiKeyPath_UnityEditor = "./Build/Server/web_api_key.txt";
        public const string SteamAppIdPath_UnityEditor = "./Build/Server/steam_appid.txt";
    }
}