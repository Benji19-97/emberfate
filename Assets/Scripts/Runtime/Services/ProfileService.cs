using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using Newtonsoft.Json;
using Runtime.Endpoints;
using Runtime.Helpers;
using Runtime.Models;
using Runtime.Utils;
using UnityEngine;
using UnityEngine.Networking;

namespace Runtime
{
    public class ProfileService : MonoBehaviour
    {
        public static ProfileService Instance;
        public Dictionary<NetworkConnection, Profile> ConnectionInfos = new Dictionary<NetworkConnection, Profile>();

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
                Destroy(gameObject);
            }
        }

        #endregion

        [Server]
        public IEnumerator FetchProfileCoroutine(NetworkConnection conn, string steamId, bool recursiveCall = false)
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Get(EndpointRegister.GetServerFetchProfileUrl(steamId, ServerAuthenticationService.Instance.serverAuthToken)))
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
                            StartCoroutine(FetchProfileCoroutine(conn, steamId, true));
                        }
                    }
                }
                else
                {
                    try
                    {
                        ConnectionInfos[conn] = JsonConvert.DeserializeObject<Profile>(webRequest.downloadHandler.text);
                    }
                    catch (Exception e)
                    {
                        ServerLogger.LogError(e.Message);
                        throw;
                    }
                }
            }
        }

        [Server]
        public IEnumerator UpsertProfileCoroutine(NetworkConnection connKey, bool removeAfter = false, bool recursiveCall = false)
        {
            var attachedJson = JsonConvert.SerializeObject(ConnectionInfos[connKey]);
            using (UnityWebRequest webRequest = WebRequestHelper.GetPostRequest(
                EndpointRegister.GetServerUpsertProfileUrl(ConnectionInfos[connKey].steamId, ServerAuthenticationService.Instance.serverAuthToken), attachedJson))
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
                            StartCoroutine(UpsertProfileCoroutine(connKey, removeAfter, true));
                        }
                    }
                }
                else
                {
                    if (removeAfter)
                    {
                        ConnectionInfos.Remove(connKey);
                    }
                }
            }
        }
    }
}