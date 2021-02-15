using System;
using System.Collections;
using System.IO;
using Mirror;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Runtime.Endpoints;
using Runtime.Models;
using Telepathy;
using UnityEngine;
using UnityEngine.Networking;

namespace Runtime
{
    public class SteamTokenAuthenticator : NetworkAuthenticator
    {
        public static string AuthTicket;

        private string _webAPIKey;
        private string _appID;

        private const int OkResponseCode = 200;
        private const string WebApiKeyPath = "data/web_api_key.txt";
        private const string SteamAppIdPath = "data/steam_appid.txt";


        private struct AuthRequestMessage : NetworkMessage
        {
            public string Ticket;
            public string SteamId;
            public string PersonaName;
        }

        private struct AuthResponseMessage : NetworkMessage
        {
#pragma warning disable 649
            public string FailReason;
#pragma warning restore 649
        }

        [Serializable]
        public class SteamAuthResponse
        {
            public string result;
            public string steamid;
            public string ownersteamid;
            public bool vacbanned;
            public bool publisherbanned;
        }

        public override void OnServerAuthenticate(NetworkConnection conn)
        {
        }

        public override void OnClientAuthenticate(NetworkConnection conn)
        {
            AuthRequestMessage authRequestMessage = new AuthRequestMessage()
            {
                Ticket = AuthTicket,
                SteamId = Steamworks.SteamUser.GetSteamID().m_SteamID.ToString(),
                PersonaName = Steamworks.SteamFriends.GetPersonaName()
            };
            NetworkClient.Send(authRequestMessage);
        }

        #region Server

#if UNITY_SERVER || UNITY_EDITOR
        public override void OnStartServer()
        {
            NetworkServer.RegisterHandler<AuthRequestMessage>(OnAuthRequestMessage, false);
        }

        private void OnAuthRequestMessage(NetworkConnection conn, AuthRequestMessage msg)
        {
            ServerLogger.LogMessage("Received auth request. Name: " + msg.PersonaName + " SteamId: " + msg.SteamId, ServerLogger.LogType.Info);
            StartCoroutine(__ValidateToken(conn, msg.Ticket, msg.SteamId, msg.PersonaName));
        }

        private IEnumerator __ValidateToken(NetworkConnection conn, string ticket, string steamid, string steamName)
        {
            string uri;
            UnityWebRequest webRequest;
        
            try
            {
                if (string.IsNullOrEmpty(_webAPIKey))
                {
                    FetchWebApiToken();
                }

                if (string.IsNullOrEmpty(_appID))
                {
                    FetchAppId();
                }

                //uri = SteamApiUserAuthUri +  $"?key={_webAPIKey}&ticket={ticket}&appid={_appID}";
                uri = EndpointRegister.GetServerSteamApiUserAuthUrl(_webAPIKey, ticket, _appID);
                webRequest = UnityWebRequest.Get(uri);
            }
            catch (Exception e)
            {
                ServerLogger.LogMessage(e.Message, ServerLogger.LogType.Error);
                ValidateTokenFailed(conn, "Server Exception");
                yield break;
            }

            if (string.IsNullOrEmpty(uri) || webRequest == null)
            { 
                ServerLogger.LogMessage("Error building web request for user authentication. Yielding break.", ServerLogger.LogType.Error);
                ValidateTokenFailed(conn, "Server Exception");
                yield break;
            }

            yield return webRequest.SendWebRequest();

            try
            {
                if (webRequest.isNetworkError || webRequest.isHttpError || webRequest.responseCode != OkResponseCode)
                {
                    ValidateTokenFailed(conn, "Network or HTTP error occurred.");
                }
                else
                {
                    HandleTokenValidation(conn, webRequest.downloadHandler.text, steamid, steamName);
                }
            }
            catch (Exception e)
            {
                ServerLogger.LogMessage(e.Message, ServerLogger.LogType.Error);
                ValidateTokenFailed(conn, "Server Exception");
            }
        }

        private void FetchWebApiToken()
        {
            string path = WebApiKeyPath;
            StreamReader reader = new StreamReader(path);
            _webAPIKey = reader.ReadToEnd();
            reader.Close();
        }

        private void FetchAppId()
        {
            string path = SteamAppIdPath;
            StreamReader reader = new StreamReader(path);
            _appID = reader.ReadToEnd();
            reader.Close();
        }


        private void HandleTokenValidation(NetworkConnection conn, string text, string steamId, string steamName)
        {
            try
            {
                var jObject = JObject.Parse(text);
                var jToken = jObject["response"]?["params"];

                if (jToken != null)
                {
                    var authResponse = jToken.ToObject<SteamAuthResponse>();

                    if (authResponse != null && (!authResponse.vacbanned || !authResponse.publisherbanned || authResponse.result != "OK" ||
                                                 authResponse.steamid != steamId))
                    {
                        ValidateTokenSucceeded(conn, steamId, steamName);
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                ServerLogger.LogMessage(e.Message, ServerLogger.LogType.Error);
            }

            ValidateTokenFailed(conn, "Authentication failed: " + text);
        }

        private void ValidateTokenSucceeded(NetworkConnection conn, string steamId, string steamName)
        {
            StartCoroutine(GetPlayerDataAndAuthenticate(conn, steamId, steamName));
        }

        private IEnumerator GetPlayerDataAndAuthenticate(NetworkConnection conn, string steamId, string steamName, bool recursiveCall = false)
        {
            ServerLogger.LogMessage("Fetching player data for " + steamId, ServerLogger.LogType.Info);
            using (UnityWebRequest webRequest = UnityWebRequest.Get(EndpointRegister.GetServerFetchProfileUrl(steamId, ServerAuthenticator.Instance.authToken)))
            {
                yield return webRequest.SendWebRequest();

                if (webRequest.isNetworkError || webRequest.responseCode != 200)
                {
                    //if error and unauthorized
                    if (!recursiveCall)
                    {
                        //get new token
                        yield return StartCoroutine(ServerAuthenticator.Instance.GetAuthTokenRequest());

                        //if token is null, the request failed
                        if (ServerAuthenticator.Instance.authToken == null)
                        {
                            ServerLogger.LogMessage("Error, auth token was null right after requesting.", ServerLogger.LogType.Error);
                            ServerLogger.LogMessage("Aborting GetPlayerData call!", ServerLogger.LogType.Error);
                            ValidateTokenFailed(conn, "Failed fetching player data from database.");
                        }
                        else //if token is not null, we can try again
                        {
                            StartCoroutine(GetPlayerDataAndAuthenticate(conn, steamId, steamName, true));
                        }
                    }
                    else
                    {
                        ServerLogger.LogMessage("Error while trying to get player data: " + webRequest.error + webRequest.downloadHandler.text,
                            ServerLogger.LogType.Error);
                        ServerLogger.LogMessage("Aborting GetPlayerData call!", ServerLogger.LogType.Error);
                        ValidateTokenFailed(conn, "Failed fetching player data from database.");
                    }

                }
                else
                {
                    ServerLogger.LogMessage("Successfully fetched player data for " + steamId, ServerLogger.LogType.Success);
                    ServerLogger.LogMessage("Data:" + webRequest.downloadHandler.text, ServerLogger.LogType.Success);
                    var playerData = JsonConvert.DeserializeObject<Profile>(webRequest.downloadHandler.text);
                    playerData.name = steamName;
                    ProfileService.Instance.ConnectionInfos.Add(conn, playerData);
                    AuthResponseMessage authResponseMessage = new AuthResponseMessage();
                    conn.Send(authResponseMessage);
                    OnServerAuthenticated.Invoke(conn);
                }
            }
        }

        private void ValidateTokenFailed(NetworkConnection conn, string reason)
        {
            AuthResponseMessage authResponseMessage =
                new AuthResponseMessage()
                {
                    FailReason = reason
                };
            conn.Send(authResponseMessage);
        }
#endif

        #endregion


        #region Client

#if !UNITY_SERVER
        public override void OnStartClient()
        {
            NetworkClient.RegisterHandler<AuthResponseMessage>(OnAuthResponseMessage, false);
        }

        private void OnAuthResponseMessage(NetworkConnection conn, AuthResponseMessage msg)
        {
            if (string.IsNullOrEmpty(msg.FailReason))
            {
                Debug.Log("Authentication successful. Connecting to game server.");
                OnClientAuthenticated.Invoke(conn);
            }
            else
            {
                Debug.Log("Authentication failed: " + msg.FailReason);
                NetworkClient.Disconnect();
            }
        }
#endif
    
        #endregion
    }
}