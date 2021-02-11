using System;
using System.Collections;
using System.Linq;
using Mirror;
using Newtonsoft.Json;
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
#if UNITY_SERVER
            SendPostRequest(new ServerStatus()
            {
                name = "UnityGameServer",
                ip = "localhost",
                maxConnections = 1,
                location = "EU",
                status = "Ok"
            });
            NetworkManager.singleton.StartServer();
            Debug.Log("Netowkrmanager: Started server!");
#endif
        }

        public void SendGetRequest()
        {
            StartCoroutine(GetRequest());
        }

        private IEnumerator GetRequest()
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
            {
                yield return webRequest.SendWebRequest();

                if (webRequest.isNetworkError)
                {
                    Debug.Log("Error: " + webRequest.error);
                }
                else
                {
                    serverStatus = JsonConvert.DeserializeObject<ServerStatus[]>(webRequest.downloadHandler.text);
                    serverStatusReceived.Invoke();
                }
            }
        }

#if UNITY_SERVER
        public void SendPostRequest(ServerStatus status)
        {
            StartCoroutine(PostRequest(status));
            Debug.Log("Sent post request!");
        }

        private IEnumerator PostRequest(ServerStatus status)
        {
            var key = "ozShHLD0shAEzxf6uaUXQDg8YNqOufoR"; //TODO: Don't put this in code
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
                    Debug.Log("Error while Sending: " + webRequest.error);
                }
                else
                {
                    Debug.Log("Success: " + webRequest.downloadHandler.text);
                }
            }
        }
#endif

        private void OnApplicationQuit()
        {
#if UNITY_SERVER || UNITY_EDITOR
            NetworkManager.singleton.StopServer();
#endif
        }
    }
}