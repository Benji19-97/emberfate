using System;
using System.Collections;
using System.IO;
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

#if UNITY_SERVER || UNITY_EDITOR
        public void SendPostRequest(ServerStatus status)
        {
            StartCoroutine(PostRequest(status));
            Console.WriteLine("Sent post request to Server Status API, to register status!");
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
                    Console.WriteLine("Error while Sending: " + webRequest.error);
                }
                else
                {
                    Console.WriteLine("Success: " + webRequest.downloadHandler.text);
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