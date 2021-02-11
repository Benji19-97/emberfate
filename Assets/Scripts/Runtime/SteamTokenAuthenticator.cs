using System;
using System.Collections;
using System.IO;
using Mirror;
using Newtonsoft.Json.Linq;
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
        private const string SteamApiUserAuthUri = "https://partner.steam-api.com/ISteamUserAuth/AuthenticateUserTicket/v1/";

        private struct AuthRequestMessage : NetworkMessage
        {
            public string Ticket;
            public string SteamId;
        }

        private struct AuthResponseMessage : NetworkMessage
        {
            public string FailReason;
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
                SteamId = Steamworks.SteamUser.GetSteamID().m_SteamID.ToString()
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
            StartCoroutine(__ValidateToken(conn, msg.Ticket, msg.SteamId));
        }

        private IEnumerator __ValidateToken(NetworkConnection conn, string ticket, string steamid)
        {
            string parameters;
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

                parameters = $"?key={_webAPIKey}&ticket={ticket}&appid={_appID}";
                webRequest = UnityWebRequest.Get(SteamApiUserAuthUri + parameters);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                ValidateTokenFailed(conn, "Server Exception.");
                yield break;
            }

            if (!string.IsNullOrEmpty(parameters) || webRequest == null)
            {
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
                    HandleTokenValidation(conn, webRequest.downloadHandler.text, ticket, steamid);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                ValidateTokenFailed(conn, "Server Exception.");
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


        private void HandleTokenValidation(NetworkConnection conn, string text, string ticket, string steamId)
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
                        ValidateTokenSucceeded(conn, steamId);
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Log(e);
            }

            ValidateTokenFailed(conn, "Authentication failed: " + text);
        }

        private void ValidateTokenSucceeded(NetworkConnection conn, string steamId)
        {
            AuthResponseMessage authResponseMessage = new AuthResponseMessage();
            conn.Send(authResponseMessage);
            OnServerAuthenticated.Invoke(conn);
            //TODO: Add connection to player list with steamId
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
                Debug.Log("Authenticator success. Player may connect.");
                OnClientAuthenticated.Invoke(conn);
            }
            else
            {
                Debug.LogError("Authenticator failed " + msg.FailReason);
                NetworkClient.Disconnect();
            }
        }
#endif
    
        #endregion
    }
}