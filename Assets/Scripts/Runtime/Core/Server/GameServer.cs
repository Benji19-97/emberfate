using System;
using System.Collections;
using System.IO;
using Mirror;
using Newtonsoft.Json;
using Runtime.Helpers;
using Runtime.Models;
using Runtime.Registers;
using Runtime.Services;
using UnityEngine;

namespace Runtime.Core.Server
{
    public class GameServer : MonoBehaviour
    {
#if UNITY_SERVER || UNITY_EDITOR

#if UNITY_EDITOR
        // ReSharper disable once InconsistentNaming
        public static bool START_SERVER_IN_UNITY_EDITOR = false;
#endif

        public static GameServer Instance { get; private set; }

        public ServerConfig Config { get; private set; }

        #region Unity Event functions

        private void Awake()
        {
#if UNITY_EDITOR
            if (!START_SERVER_IN_UNITY_EDITOR)
            {
                Destroy(gameObject);
                return;
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
                return;
            }
#if UNITY_EDITOR
            var path = PathRegister.Server_ConfigPath_UnityEditor;
#else
            string path = PathRegister.Server_ConfigPath;
#endif
            var reader = new StreamReader(path);
            var json = reader.ReadToEnd();
            reader.Close();
            Config = JsonConvert.DeserializeObject<ServerConfig>(json);
        }


        [RuntimeInitializeOnLoadMethod]
        public static void RunOnStart()
        {
            try
            {
                Application.wantsToQuit += Instance.ExitApplication;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private void Start()
        {
            ServerStatusService.Instance.UpdateServerStatus(new ServerStatus
            {
                name = Config.name,
                ip = Config.ip,
                maxConnections = Config.maxConnections,
                location = Config.location,
                status = "online",
                port = Config.port
            });
        }

        public bool ExitApplication()
        {
#if UNITY_SERVER
            return StopServer();
#elif UNITY_EDITOR
            if (START_SERVER_IN_UNITY_EDITOR)
            {
                return StopServer();
            }
#endif
            EmberfateNetworkManager.Instance.StopClient();
            return true;
        }

        #endregion

        public void StartServer()
        {
            EmberfateNetworkManager.Instance.networkAddress = Config.ip;
            EmberfateNetworkManager.Instance.maxConnections = Config.maxConnections;
            EmberfateNetworkManager.Instance.GetComponent<TelepathyTransport>().port = Config.port;
            EmberfateNetworkManager.Instance.StartServer();
        }

        public bool StopServer()
        {
            try
            {
                ServerLogger.Log("Stopping server...");
                ServerStatusService.Instance.UpdateServerStatus(new ServerStatus
                {
                    name = Config.name,
                    ip = Config.ip,
                    maxConnections = Config.maxConnections,
                    location = Config.location,
                    status = "offline",
                    port = Config.port
                });

                EmberfateNetworkManager.Instance.StopServer();
            }
            catch (Exception e)
            {
                ServerLogger.LogError(e.Message);
            }

            return true;
        }

        public void PushServerToDb()
        {
            foreach (var connectionInfo in ProfileService.Instance.ConnectionInfos)
            {
                StartCoroutine(ProfileService.Instance.UpsertProfileCoroutine(connectionInfo.Key));

                StartCoroutine(StashService.Instance.UpsertStashCoroutine(connectionInfo.Key));

                if (connectionInfo.Value.PlayingCharacter != null)
                {
                    StartCoroutine(CharacterService.Instance.UpdateCharacterOnDatabaseCoroutine(connectionInfo.Value.PlayingCharacter));
                }
            }
        }
#endif
    }
}