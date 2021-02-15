using System;
using System.Collections;
using System.IO;
using Mirror;
using Newtonsoft.Json;
using Runtime.Endpoints;
using Runtime.Models;
using Runtime.UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace Runtime
{
    public class ServerStatusService : MonoBehaviour
    {
        public static ServerStatusService Instance { get; private set; }

        public ServerStatus[] serverStatus { get; private set; }

        [HideInInspector] public UnityEvent serverStatusReceived;



        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(this);
            }
        }

        private void Start()
        {
#if UNITY_SERVER
            return;
#endif
#if UNITY_EDITOR
            if (GameServer.START_SERVER_IN_UNITY_EDITOR)
            {
                return;
            }
#endif
            SteamInitializer.Instance.initializedSteam.AddListener(OnSteamInitialized);
        }

        private void OnSteamInitialized()
        {
            SendGetRequest();
        }

        public void SendGetRequest()
        {
            StartCoroutine(GetRequest());
        }

        private IEnumerator GetRequest()
        {
            NotificationSystem.Instance.PushNotification("Retrieving server status ...", false);
            using (UnityWebRequest webRequest = UnityWebRequest.Get(EndpointRegister.GetClientFetchServerStatusUrl()))
            {
                yield return webRequest.SendWebRequest();

                if (webRequest.isNetworkError)
                {
                    NotificationSystem.Instance.PushNotification("Couldn't retrieve server status. Network Error: " + webRequest.error, true);
                }
                else
                {
                    NotificationSystem.Instance.PushNotification("Retrieved server status from API.", true);
                    serverStatus = JsonConvert.DeserializeObject<ServerStatus[]>(webRequest.downloadHandler.text);
                    serverStatusReceived.Invoke();
                }
            }
        }

#if UNITY_SERVER || UNITY_EDITOR
        public void SendServerStatusPostRequest(ServerStatus status)
        {
            StartCoroutine(PostServerStatus(status));
        }

        private IEnumerator PostServerStatus(ServerStatus status, bool recursiveCall = false)
        {
            ServerLogger.LogMessage("Updating server status on API...", ServerLogger.LogType.Info);
            var postDataJson = JsonConvert.SerializeObject(status);

            using (UnityWebRequest webRequest = new UnityWebRequest(EndpointRegister.GetServerUpdateServerStatusUrl(ServerAuthenticator.Instance.authToken), "POST"))
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
                            ServerLogger.LogMessage("Aborting PostServerStatus call!", ServerLogger.LogType.Error);
                        }
                        else //if token is not null, we can try again to post status
                        {
                            ServerLogger.LogMessage("Trying again to update server status on API.", ServerLogger.LogType.Info);
                            StartCoroutine(PostServerStatus(status, true));
                        }
                    }
                    else
                    {
                        ServerLogger.LogMessage("Error while trying to post server status: " + webRequest.error + webRequest.downloadHandler.text,
                            ServerLogger.LogType.Error);
                        ServerLogger.LogMessage("Aborting PostServerStatus call!", ServerLogger.LogType.Error);
                    }
                }
                else
                {
                    ServerLogger.LogMessage("Successfully updated server status on API.", ServerLogger.LogType.Success);
                    GameServer.Instance.StartServer();
                }
            }
        }

#endif

        private void OnApplicationQuit()
        {
#if UNITY_SERVER
            NetworkManager.singleton.StopServer();
            return;
#endif
#if UNITY_EDITOR
            if (GameServer.START_SERVER_IN_UNITY_EDITOR)
            {
                NetworkManager.singleton.StopServer();
                return;
            }
#endif
            NetworkManager.singleton.StopClient();
        }
    }
}