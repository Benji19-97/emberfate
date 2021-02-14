using System;
using System.Collections;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace Runtime
{
    public class ServerAuthenticator : MonoBehaviour
    {
#if UNITY_SERVER || UNITY_EDITOR
        public static ServerAuthenticator Instance;

        private const string GetAuthTokenUri = "http://localhost:3000/api/authentication/token";

        private const string PasswordPath = "data/password.txt";

        public string authToken { get; private set; }

        private void Awake()
        {
#if UNITY_EDITOR
            if (!GameServer.START_SERVER_IN_UNITY_EDITOR)
            {
                Destroy(gameObject);
            }
#endif

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

        public IEnumerator GetAuthTokenRequest()
        {
            ServerLogger.LogMessage("Requesting auth token ...", ServerLogger.LogType.Info);
            var bodyObject = new
            {
                name = GameServer.Instance.Config.name,
                password = ReadServerPassword()
            };

            var bodyJson = JsonConvert.SerializeObject(bodyObject);


            using (UnityWebRequest webRequest = new UnityWebRequest(GetAuthTokenUri, "POST"))
            {
                byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(bodyJson);
                webRequest.uploadHandler = new UploadHandlerRaw(jsonToSend);
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", "application/json");

                yield return webRequest.SendWebRequest();

                if (webRequest.isNetworkError || webRequest.responseCode != 200)
                {
                    ServerLogger.LogMessage("Error requesting authentication token: " + webRequest.error, ServerLogger.LogType.Error);
                    ServerLogger.LogMessage(webRequest.downloadHandler.text, ServerLogger.LogType.Error);
                    authToken = null;
                }
                else
                {
                    ServerLogger.LogMessage("Received authentication token.", ServerLogger.LogType.Success);
                    authToken = webRequest.downloadHandler.text;
                }
            }
        }

        private string ReadServerPassword()
        {
            string path = PasswordPath;
            StreamReader reader = new StreamReader(path);
            var pw = reader.ReadToEnd();
            reader.Close();
            return pw;
        }

#endif
    }
}