using System;
using System.Collections;
using Mirror;
using Runtime.Helpers;
using Runtime.Models;
using Runtime.Registers;
using UnityEngine;
using UnityEngine.Networking;

namespace Runtime.Services
{
#if UNITY_SERVER || UNITY_EDITOR
    public class StashService : MonoBehaviour
    {
        public static StashService Instance;

        #region Unity Event function

        private void Awake()
        {
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

        public IEnumerator UpsertStashCoroutine(NetworkConnection conn, bool recursiveCall = false)
        {
            ServerLogger.Log($"Started 'UpsertStashCoroutine'. Args(conn: {conn}, recursiveCall: {recursiveCall})");
            
            string steamId;
            try
            {
                steamId = ProfileService.Instance.ConnectionInfos[conn].steamId;
            }
            catch (Exception e)
            {
                ServerLogger.LogError(e.Message);
                yield break;
            }

            using (var webRequest =
                new UnityWebRequest(
                    EndpointRegister.GetServerUpsertStashUrl(steamId, ServerAuthenticationService.Instance.serverAuthToken),
                    "PUT"))
            {
                webRequest.uploadHandler = new UploadHandlerRaw(ProfileService.Instance.ConnectionInfos[conn].stash.Serialize());
                webRequest.downloadHandler = new DownloadHandlerBuffer();

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
                            StartCoroutine(UpsertStashCoroutine(conn, true));
                            yield break;
                        }
                    }

                    ServerLogger.LogError(webRequest.error);
                    yield break;
                }

                ServerLogger.LogSuccess($"Updated stash of {conn} on db.");
            }
        }

        // public IEnumerator FetchStashCoroutine(NetworkConnection conn, bool recursiveCall = false)
        // {
        //     ServerLogger.Log($"Started 'FetchStashCoroutine'. Args(conn: {conn}, recursiveCall: {recursiveCall})");
        //
        //     string steamId;
        //     try
        //     {
        //         steamId = ProfileService.Instance.ConnectionInfos[conn].steamId;
        //     }
        //     catch (Exception e)
        //     {
        //         ServerLogger.LogError(e.Message);
        //         yield break;
        //     }
        //
        //     using (var webRequest =
        //         UnityWebRequest.Get(EndpointRegister.GetServerFetchStashUrl(steamId, ServerAuthenticationService.Instance.serverAuthToken)))
        //     {
        //         yield return webRequest.SendWebRequest();
        //
        //         if (webRequest.isNetworkError)
        //         {
        //             ServerLogger.LogError(webRequest.error);
        //             yield break;
        //         }
        //
        //         if (webRequest.isHttpError)
        //         {
        //             if (!recursiveCall)
        //             {
        //                 yield return StartCoroutine(ServerAuthenticationService.Instance.FetchAuthTokenCoroutine());
        //
        //                 if (ServerAuthenticationService.Instance.serverAuthToken != null)
        //                 {
        //                     StartCoroutine(FetchStashCoroutine(conn, true));
        //                     yield break;
        //                 }
        //             }
        //
        //             ServerLogger.LogError(webRequest.error);
        //         }
        //         else
        //         {
        //             ProfileService.Instance.ConnectionInfos[conn].stash = Stash.Deserialize(webRequest.downloadHandler.text);
        //             ServerLogger.LogSuccess($"Received and deserialized stash of {conn}");
        //         }
        //     }
        // }
    }
#endif
}