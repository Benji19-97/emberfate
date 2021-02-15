using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using Newtonsoft.Json;
using Runtime.Models;
using UnityEngine;
using UnityEngine.Networking;

namespace Runtime
{
    public class ProfileService : MonoBehaviour
    {
        public static ProfileService Instance;
        public Dictionary<NetworkConnection, Profile> ConnectionInfos = new Dictionary<NetworkConnection, Profile>();

        private const string PostPlayerDataUri = "http://localhost:3000/api/profiles/upsert/";
        private const string GetPlayerDataUri = "http://localhost:3000/api/profiles/";

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public IEnumerator GetProfile(NetworkConnection conn, string steamId, bool recursiveCall = false)
        {
#if UNITY_SERVER || UNITY_EDITOR
            ServerLogger.LogMessage("Fetching profile data for " + steamId, ServerLogger.LogType.Info);
            using (UnityWebRequest webRequest = UnityWebRequest.Get(GetPlayerDataUri + steamId + "/" + ServerAuthenticator.Instance.authToken))
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
                        }
                        else //if token is not null, we can try again
                        {
                            StartCoroutine(GetProfile(conn, steamId, true));
                        }
                    }
                    else
                    {
                        ServerLogger.LogMessage("Error while trying to get player data: " + webRequest.error + webRequest.downloadHandler.text,
                            ServerLogger.LogType.Error);
                        ServerLogger.LogMessage("Aborting GetPlayerData call!", ServerLogger.LogType.Error);
                    }

                }
                else
                {
                    ServerLogger.LogMessage("Successfully fetched player data for " + steamId, ServerLogger.LogType.Success);
                    ServerLogger.LogMessage("Data:" + webRequest.downloadHandler.text, ServerLogger.LogType.Success);
                    var playerData = JsonConvert.DeserializeObject<Profile>(webRequest.downloadHandler.text);
                    ConnectionInfos[conn] = playerData;
                }
            }
#else
            yield break;
#endif
        }

        public IEnumerator PushProfile(NetworkConnection connKey, bool removeAfter = false, bool recursiveCall = false)
        {
#if UNITY_SERVER || UNITY_EDITOR
            ServerLogger.LogMessage("Pushing profile to database...", ServerLogger.LogType.Info);
            var postDataJson = JsonConvert.SerializeObject(ConnectionInfos[connKey]);
            ServerLogger.LogMessage("Sending this JSON: " + postDataJson, ServerLogger.LogType.Info);

            using (UnityWebRequest webRequest =
                new UnityWebRequest(PostPlayerDataUri + ConnectionInfos[connKey].steamId + "/" + ServerAuthenticator.Instance.authToken, "POST"))
            {
                byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(postDataJson);
                webRequest.uploadHandler = new UploadHandlerRaw(jsonToSend);
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", "application/json");

                yield return webRequest.SendWebRequest();

                if (webRequest.isNetworkError || webRequest.responseCode != 200)
                {
                    //if error and unauthorized
                    if (!recursiveCall)
                    {
                        //get new token
                        yield return StartCoroutine(ServerAuthenticator.Instance.GetAuthTokenRequest());

                        //if token is null, the token request failed
                        if (ServerAuthenticator.Instance.authToken == null)
                        {
                            ServerLogger.LogMessage("Error, auth token was null right after requesting.", ServerLogger.LogType.Error);
                            ServerLogger.LogMessage("Aborting PushProfile call!", ServerLogger.LogType.Error);
                        }
                        else //if token is not null, we can try again to post status
                        {
                            ServerLogger.LogMessage("Trying again to update server status on API.", ServerLogger.LogType.Info);
                            StartCoroutine(PushProfile(connKey, removeAfter, true));
                        }
                    }
                    else
                    {
                        ServerLogger.LogMessage("Error while trying to push player data " + webRequest.error + webRequest.downloadHandler.text,
                            ServerLogger.LogType.Error);
                        ServerLogger.LogMessage("Aborting PushProfile call!", ServerLogger.LogType.Error);
                    }
                }
                else
                {
                    ServerLogger.LogMessage("Successfully pushed player data to database.", ServerLogger.LogType.Success);
                    if (removeAfter)
                    {
                        ConnectionInfos.Remove(connKey);
                        ServerLogger.LogMessage("Removed " + connKey, ServerLogger.LogType.Info);
                    }
                }
            }
#else
            yield break;
#endif
        }
    }
}