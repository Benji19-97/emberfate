using System.Collections;
using System.IO;
using Newtonsoft.Json;
using Runtime.Core.Server;
using Runtime.Helpers;
using Runtime.Registers;
using UnityEngine;

namespace Runtime.Services
{
    public class ServerAuthenticationService : MonoBehaviour
    {
#if UNITY_SERVER || UNITY_EDITOR
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
            var attachedJson = JsonConvert.SerializeObject(new
            {
                GameServer.Instance.Config.name,
                password = ReadServerPassword()
            });

            using (var webRequest = WebRequestHelper.GetPostRequest(EndpointRegister.GetServerFetchAuthTokenUrl(), attachedJson))
            {
                yield return webRequest.SendWebRequest();

                if (webRequest.isNetworkError)
                {
                    ServerLogger.LogError(webRequest.error);
                    serverAuthToken = null;
                    yield break;
                }

                serverAuthToken = webRequest.isHttpError ? null : webRequest.downloadHandler.text;
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
#endif
    }
}