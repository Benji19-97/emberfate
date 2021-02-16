using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using Newtonsoft.Json;
using Runtime.Helpers;
using Runtime.Models;
using Runtime.Registers;
using UnityEngine;
using UnityEngine.Networking;

namespace Runtime.Services
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
            ServerLogger.Log($"Started 'FetchProfileCoroutine'. Args(conn: {conn}, streamId: {steamId}, recursiveCall: {recursiveCall})");
            using (var webRequest =
                UnityWebRequest.Get(EndpointRegister.GetServerFetchProfileUrl(steamId, ServerAuthenticationService.Instance.serverAuthToken)))
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
                            yield break;
                        }
                    }

                    ServerLogger.LogError(webRequest.error);
                    yield break;
                }

                try
                {
                    ConnectionInfos[conn] = JsonConvert.DeserializeObject<Profile>(webRequest.downloadHandler.text);
                    ServerLogger.LogSuccess($"Fetched profile {steamId} of db.");
                }
                catch (Exception e)
                {
                    ServerLogger.LogError(e.Message);
                    throw;
                }
            }
        }

        [Server]
        public IEnumerator UpsertProfileCoroutine(NetworkConnection conn, bool removeProfilesConnectionAfter = false, bool recursiveCall = false)
        {
            ServerLogger.Log(
                $"Started 'UpsertProfileCoroutine'. Args(conn: {conn}, removeProfilesConnectionAfter: {removeProfilesConnectionAfter}, recursiveCall: {recursiveCall})");

            string attachedJson;
            try
            {
                attachedJson = JsonConvert.SerializeObject(ConnectionInfos[conn]);
            }
            catch (Exception e)
            {
                ServerLogger.LogError(e.Message);
                throw;
            }

            using (var webRequest = WebRequestHelper.GetPostRequest(
                EndpointRegister.GetServerUpsertProfileUrl(ConnectionInfos[conn].steamId, ServerAuthenticationService.Instance.serverAuthToken),
                attachedJson))
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
                            StartCoroutine(UpsertProfileCoroutine(conn, removeProfilesConnectionAfter, true));
                            yield break;
                        }
                    }
                    
                    ServerLogger.LogError(webRequest.error);
                    yield break;
                }

                try
                {
                    var steamId = ConnectionInfos[conn].steamId;

                    if (removeProfilesConnectionAfter)
                    {
                        ConnectionInfos.Remove(conn);
                        ServerLogger.LogSuccess($"Upserted profile of {steamId} to db and removed connection afterwards.");
                    }
                    else
                    {
                        ServerLogger.LogSuccess($"Upserted profile of {steamId} to db.");
                    }
                }
                catch (Exception e)
                {
                    ServerLogger.LogError(webRequest.error);
                    throw;
                }

            }
        }
    }
}