using System;
using System.Collections;
using System.IO;
using Mirror;
using Newtonsoft.Json;
using Runtime.Endpoints;
using Runtime.Helpers;
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

        #region Unity Event functions

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
#elif UNITY_EDITOR
            if (GameServer.START_SERVER_IN_UNITY_EDITOR)
            {
                return;
            }
#endif
            SteamInitializer.Instance.initializedSteam.AddListener(OnSteamInitialized);
        }
        
        private void OnApplicationQuit()
        {
#if UNITY_SERVER
            NetworkManager.singleton.StopServer();
            return;
#elif UNITY_EDITOR
            if (GameServer.START_SERVER_IN_UNITY_EDITOR)
            {
                NetworkManager.singleton.StopServer();
                return;
            }
#endif
            NetworkManager.singleton.StopClient();
        }

        #endregion

        private void OnSteamInitialized()
        {
            SendGetRequest();
        }

        public void SendGetRequest()
        {
            StartCoroutine(FetchServerStatusCoroutine());
        }

        private IEnumerator FetchServerStatusCoroutine()
        {
            NotificationSystem.Push("Retrieving server status ...", false);
            using (UnityWebRequest webRequest = UnityWebRequest.Get(EndpointRegister.GetClientFetchServerStatusUrl()))
            {
                yield return webRequest.SendWebRequest();

                if (webRequest.isNetworkError)
                {
                    NotificationSystem.Push("Couldn't retrieve server status. Network Error: " + webRequest.error, true);
                    yield break;
                }

                if (webRequest.isHttpError)
                {
                    NotificationSystem.Push("Couldn't retrieve server status. Http Error: " + webRequest.error, true);
                    yield break;
                }

                NotificationSystem.Push("Retrieved server status from API.", true);
                serverStatus = JsonConvert.DeserializeObject<ServerStatus[]>(webRequest.downloadHandler.text);
                serverStatusReceived.Invoke();
            }
        }

#if UNITY_SERVER || UNITY_EDITOR

        [Server]
        public void SendServerStatusPostRequest(ServerStatus status)
        {
            StartCoroutine(PostServerStatusCoroutine(status));
        }

        private IEnumerator PostServerStatusCoroutine(ServerStatus status, bool recursiveCall = false)
        {
            var postDataJson = JsonConvert.SerializeObject(status);

            using (var webRequest =
                new UnityWebRequest(EndpointRegister.GetServerUpdateServerStatusUrl(ServerAuthenticator.Instance.serverAuthToken), "POST"))
            {
                var jsonToSend = new System.Text.UTF8Encoding().GetBytes(postDataJson);
                webRequest.uploadHandler = new UploadHandlerRaw(jsonToSend);
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", "application/json");

                yield return webRequest.SendWebRequest();

                if (webRequest.isNetworkError)
                {
                    ServerLogger.LogError(webRequest.error);
                    yield break;
                }

                if (webRequest.isHttpError)
                {
                    if (!recursiveCall)
                    {
                        yield return StartCoroutine(ServerAuthenticator.Instance.FetchAuthTokenCoroutine());

                        if (ServerAuthenticator.Instance.serverAuthToken != null)
                        {
                            StartCoroutine(PostServerStatusCoroutine(status, true));
                        }
                    }
                }
                else
                {
                    GameServer.Instance.StartServer();
                }

 

     
            }
        }

#endif


    }
}