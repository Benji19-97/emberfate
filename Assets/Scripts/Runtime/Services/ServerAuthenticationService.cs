using System;
using System.Collections;
using System.IO;
using Newtonsoft.Json;
using Runtime.Core.Server;
using Runtime.Helpers;
using Runtime.Registers;
using UnityEngine;

namespace Runtime.Services
{
#if UNITY_SERVER || UNITY_EDITOR
    public class ServerAuthenticationService : MonoBehaviour
    {
        public static ServerAuthenticationService Instance;
        public string serverAuthToken { get; private set; }

        #region Unity Event functions

        private void Awake()
        {
#if UNITY_EDITOR
            if (!GameServer.START_SERVER_IN_UNITY_EDITOR) Destroy(gameObject);
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

        #endregion

        public IEnumerator FetchAuthTokenCoroutine()
        {
            ServerLogger.Log($"Started 'FetchAuthTokenCoroutine'");
            string attachedJson;
            try
            {
                attachedJson = JsonConvert.SerializeObject(new
                {
                    GameServer.Instance.Config.name,
                    password = ReadServerPassword()
                });
            }
            catch (Exception e)
            {
                ServerLogger.LogError(e.Message);
                throw;
            }

            using (var webRequest = WebRequestHelper.GetPostRequest(EndpointRegister.GetServerFetchAuthTokenUrl(), attachedJson))
            {
                yield return webRequest.SendWebRequest();

                if (webRequest.isNetworkError)
                {
                    serverAuthToken = null;
                    ServerLogger.LogError(webRequest.error);
                    yield break;
                }

                if (webRequest.isHttpError)
                {
                    serverAuthToken = null;
                    ServerLogger.LogError(webRequest.error);
                    yield break;
                }

                serverAuthToken = webRequest.downloadHandler.text;
                ServerLogger.LogSuccess($"Received auth token.");
            }
        }

        private static string ReadServerPassword()
        {
#if UNITY_EDITOR
            var path = PathRegister.Server_PasswordPath_UnityEditor;
#else
            var path = PathRegister.Server_PasswordPath;
#endif
            var reader = new StreamReader(path);
            var pw = reader.ReadToEnd();
            reader.Close();
            return pw;
        }
    }
#endif
}