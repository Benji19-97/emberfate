using System;
using System.Collections;
using System.IO;
using Mirror;
using Newtonsoft.Json;
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

        [SerializeField] private string uri;

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
            #if !UNITY_SERVER
            SteamInitializer.Instance.initializedSteam.AddListener(OnSteamInitialized);
            #endif
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
            using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
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

#if UNITY_SERVER
        public void SendPostRequest(ServerStatus status)
        {
            StartCoroutine(PostRequest(status)); 
            ServerLogger.LogMessage("Trying to register server on Server Status API.", ServerLogger.LogType.Info);
        }

        private IEnumerator PostRequest(ServerStatus status)
        {
            var key = FetchServerKey();
            var postData = new
            {
                data = status,
                key
            };

            var postDataJson = JsonConvert.SerializeObject(postData);

            using (UnityWebRequest webRequest = new UnityWebRequest(uri, "POST"))
            {
                byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(postDataJson);
                webRequest.uploadHandler = new UploadHandlerRaw(jsonToSend);
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", "application/json");

                yield return webRequest.SendWebRequest();

                if (webRequest.isNetworkError)
                {
                    ServerLogger.LogMessage("Error while trying to talk to Server Status API: " + webRequest.error, ServerLogger.LogType.Error);
                    //TODO: Restart to try again?
                }
                else
                {
                    ServerLogger.LogMessage("Success: " + webRequest.downloadHandler.text, ServerLogger.LogType.Success);
                    GameServer.Instance.StartServer();
                }
            }
        }

        private string FetchServerKey()
        {
            string path = "data/server_key.txt";
            StreamReader reader = new StreamReader(path);
            var key = reader.ReadToEnd();
            reader.Close();
            return key;
        }
#endif

        private void OnApplicationQuit()
        {
#if UNITY_SERVER
            NetworkManager.singleton.StopServer();
            return;
#endif
            NetworkManager.singleton.StopClient();
        }
    }
}