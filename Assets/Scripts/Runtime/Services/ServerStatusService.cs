using System;
using System.Collections;
using System.Text;
using Newtonsoft.Json;
using Runtime.Core.Server;
using Runtime.Core.Steam;
using Runtime.Helpers;
using Runtime.Models;
using Runtime.Registers;
using Runtime.UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace Runtime.Services
{
    public class ServerStatusService : MonoBehaviour
    {
        [HideInInspector] public UnityEvent serverStatusReceived;
        public static ServerStatusService Instance { get; private set; }
        public ServerStatus[] serverStatus { get; private set; }

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
            if (GameServer.START_SERVER_IN_UNITY_EDITOR) return;
#endif
            SteamInitializer.Instance.initializedSteam.AddListener(OnSteamInitialized);
        }

        #endregion
        
        private void OnSteamInitialized()
        {
            FetchServerStatus();
        }

        private void FetchServerStatus()
        {
            StartCoroutine(FetchServerStatusCoroutine());
        }

        private IEnumerator FetchServerStatusCoroutine()
        {
            NotificationSystem.Push("Retrieving server status ...", false);
            using (var webRequest = UnityWebRequest.Get(EndpointRegister.GetClientFetchServerStatusUrl()))
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

        public void UpdateServerStatus(ServerStatus status)
        {
            StartCoroutine(UpdateServerStatusCoroutine(status));
        }

        private IEnumerator UpdateServerStatusCoroutine(ServerStatus status, bool recursiveCall = false)
        {
            ServerLogger.Log($"Started 'UpdateServerStatusCoroutine'. Args(conn: {status}, recursiveCall: {recursiveCall})");

            string postDataJson;
            try
            {
                postDataJson = JsonConvert.SerializeObject(status);
            }
            catch (Exception e)
            {
                ServerLogger.LogError(e.Message);
                throw;
            }

            using (var webRequest =
                WebRequestHelper.GetPostRequest(EndpointRegister.GetServerUpdateServerStatusUrl(ServerAuthenticationService.Instance.serverAuthToken), postDataJson))
            {
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
                        yield return StartCoroutine(ServerAuthenticationService.Instance.FetchAuthTokenCoroutine());

                        if (ServerAuthenticationService.Instance.serverAuthToken != null)
                        {
                            StartCoroutine(UpdateServerStatusCoroutine(status, true));
                            yield break;
                        }
                    }
                    
                    ServerLogger.LogError(webRequest.error);
                    yield break;
                }
                else
                {
                    GameServer.Instance.StartServer();
                    ServerLogger.LogSuccess($"Updated server status on DB. {status.status}");
                }
            }
        }

#endif
    }
}