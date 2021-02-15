using System;
using System.Collections;
using System.IO;
using Mirror;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Runtime.Endpoints;
using Runtime.Helpers;
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

        private struct AuthRequestMessage : NetworkMessage
        {
            public string Ticket;
            public string SteamId;
            public string PersonaName;
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
            StartCoroutine(ValidateTokenGetRequestCoroutine(conn, msg.Ticket, msg.SteamId, msg.PersonaName));
        }

        [Server]
        private IEnumerator ValidateTokenGetRequestCoroutine(NetworkConnection conn, string ticket, string steamid, string steamName)
        {
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

                var steamApiUserAuthUrl = EndpointRegister.GetServerSteamApiUserAuthUrl(_webAPIKey, ticket, _appID);
                webRequest = UnityWebRequest.Get(steamApiUserAuthUrl);
            }
            catch (Exception e)
            {
                ServerLogger.LogError(e.Message);
                ValidateTokenFailed(conn, "server exception");
                yield break;
            }

            yield return webRequest.SendWebRequest();

            try
            {
                if (webRequest.isNetworkError || webRequest.isHttpError)
                {
                    ServerLogger.LogError(webRequest.error);
                    ValidateTokenFailed(conn, webRequest.error);
                    yield break;
                }

                ValidateToken(conn, webRequest.downloadHandler.text, steamid, steamName);
            }
            catch (Exception e)
            {
                ServerLogger.LogError(e.Message);
                ValidateTokenFailed(conn, "server exception");
            }
        }

        private void FetchWebApiToken()
        {
            const string path = PathRegister.Server_SteamWebApiKeyPath;
            var reader = new StreamReader(path);
            _webAPIKey = reader.ReadToEnd();
            reader.Close();
        }

        private void FetchAppId()
        {
            const string path = PathRegister.SteamAppIdPath;
            var reader = new StreamReader(path);
            _appID = reader.ReadToEnd();
            reader.Close();
        }

        private void ValidateToken(NetworkConnection conn, string text, string steamId, string steamName)
        {
            try
            {
                var jToken = JObject.Parse(text)["response"]?["params"];
                var authResponse = jToken?.ToObject<SteamAuthResponse>();

                if (authResponse == null ||
                    authResponse.vacbanned ||
                    authResponse.publisherbanned ||
                    authResponse.result != "OK" ||
                    authResponse.steamid != steamId)
                {
                    ValidateTokenFailed(conn, "bad token");
                    return;
                }

                StartCoroutine(FetchProfileAndAuthenticateAfterCoroutine(conn, steamId, steamName));
            }
            catch (Exception e)
            {
                ServerLogger.LogError(e.Message);
                ValidateTokenFailed(conn, "server exception");
            }
        }

        private IEnumerator FetchProfileAndAuthenticateAfterCoroutine(NetworkConnection conn, string steamId, string steamName, bool recursiveCall = false)
        {
            using (UnityWebRequest webRequest =
                UnityWebRequest.Get(EndpointRegister.GetServerFetchProfileUrl(steamId, ServerAuthenticator.Instance.serverAuthToken)))
            {
                yield return webRequest.SendWebRequest();

                if (webRequest.isNetworkError)
                {
                    ServerLogger.LogError(webRequest.error);
                    ValidateTokenFailed(conn, webRequest.error);
                    yield break;
                }

                if (webRequest.isHttpError)
                {
                    if (!recursiveCall)
                    {
                        yield return StartCoroutine(ServerAuthenticator.Instance.FetchAuthTokenCoroutine());

                        if (ServerAuthenticator.Instance.serverAuthToken != null)
                        {
                            StartCoroutine(FetchProfileAndAuthenticateAfterCoroutine(conn, steamId, steamName, true));
                            yield break; //TODO: Error?
                        }
                    }
                    ValidateTokenFailed(conn, "Server failed fetching profile from database.");
                    yield break;
                }
                
                var profile = JsonConvert.DeserializeObject<Profile>(webRequest.downloadHandler.text);
                profile.name = steamName;
                ValidateTokenSucceeded(conn, profile);

            }
        }

        private void ValidateTokenSucceeded(NetworkConnection conn, Profile profile)
        {
            ProfileService.Instance.ConnectionInfos.Add(conn, profile);
            conn.Send(new AuthResponseMessage());
            OnServerAuthenticated.Invoke(conn);
        }

        private void ValidateTokenFailed(NetworkConnection conn, string reason)
        {
            conn.Send(new AuthResponseMessage()
            {
                FailReason = reason
            });
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