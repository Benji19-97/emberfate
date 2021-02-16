// ReSharper disable InconsistentNaming

namespace Runtime.Registers
{
    public static class EndpointRegister
    {
        private const string Client_FetchCharacterUrl = "http://localhost:3003/api/characters/";
        private const string Client_FetchServerStatusUrl = "http://localhost:3001/api/serverstatus";
#if UNITY_SERVER || UNITY_EDITOR

        //characters
        private const string Server_FetchCharacterUrl = "http://localhost:3000/api/characters/";
        private const string Server_CreateCharacterUrl = "http://localhost:3000/api/characters/create/";
        private const string Server_DeleteCharacterUrl = "http://localhost:3000/api/characters/delete/";
        private const string Server_UpdateCharacterUrl = "http://localhost:3000/api/characters/update/";

        //profiles
        private const string Server_FetchProfileUrl = "http://localhost:3000/api/profiles/";
        private const string Server_UpsertProfileUrl = "http://localhost:3000/api/profiles/upsert/";

        //auth
        private const string Server_FetchAuthTokenUrl = "http://localhost:3000/api/authentication/token";
        private const string Server_SteamApiUserAuthUrl = "https://partner.steam-api.com/ISteamUserAuth/AuthenticateUserTicket/v1/";

        //status
        private const string Server_UpdateServerStatusUrl = "http://localhost:3000/api/serverstatus/update/";
#endif

#if UNITY_SERVER || UNITY_EDITOR

        #region Server Urls

        public static string GetServerFetchCharacterUrl(string characterId, string serverAuthToken)
        {
            return Server_FetchCharacterUrl + characterId + "/" + serverAuthToken;
        }

        public static string GetServerCreateCharacterUrl(string characterName, string profileSteamId, string serverAuthToken)
        {
            return Server_CreateCharacterUrl + characterName + "/" + profileSteamId + "/" + serverAuthToken;
        }

        public static string GetServerDeleteCharacterUrl(string characterId, string steamId ,string serverAuthToken)
        {
            return Server_DeleteCharacterUrl + characterId + "/" + steamId + "/" + serverAuthToken;
        }
        
        public static string GetServerUpdateCharacterUrl(string characterId, string serverAuthToken)
        {
            return Server_UpdateCharacterUrl + characterId + "/" + serverAuthToken;
        }

        public static string GetServerFetchProfileUrl(string profileSteamId, string serverAuthToken)
        {
            return Server_FetchProfileUrl + profileSteamId + "/" + serverAuthToken;
        }

        public static string GetServerUpsertProfileUrl(string profileSteamId, string serverAuthToken)
        {
            return Server_UpsertProfileUrl + profileSteamId + "/" + serverAuthToken;
        }

        public static string GetServerFetchAuthTokenUrl()
        {
            return Server_FetchAuthTokenUrl;
        }

        public static string GetServerSteamApiUserAuthUrl(string webApiKey, string ticket, string appId)
        {
            return Server_SteamApiUserAuthUrl + $"?key={webApiKey}&ticket={ticket}&appid={appId}";
        }

        public static string GetServerUpdateServerStatusUrl(string serverAuthToken)
        {
            return Server_UpdateServerStatusUrl + serverAuthToken;
        }

        #endregion

#endif

        #region Client Urls

        public static string GetClientFetchCharacterUrl(string profileSteamId, string characterId, string steamAuthToken)
        {
            return Client_FetchCharacterUrl + profileSteamId + "/" + characterId + $"/?token={steamAuthToken}";
        }

        public static string GetClientFetchAllCharactersUrl(string profileSteamId, string steamAuthToken)
        {
            return Client_FetchCharacterUrl + profileSteamId + "/all" + $"/?token={steamAuthToken}";
        }

        public static string GetClientFetchServerStatusUrl()
        {
            return Client_FetchServerStatusUrl;
        }

        #endregion
    }
}