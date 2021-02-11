using System;
using System.Collections;
using System.Linq;
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
            SendPostRequest(new ServerStatus()
            {
                name = "UnityGameServer",
                ip = "localhost",
                maxConnections = 1,
                location = "EU",
                status = "Ok"
            });
            
            
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
    }
}