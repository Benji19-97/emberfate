using System;
using System.IO;
using JetBrains.Annotations;
using kcp2k;
using Mirror;
using Newtonsoft.Json;
using Runtime.Endpoints;
using Runtime.Helpers;
using Runtime.Models;
using UnityEngine;

namespace Runtime
{
    public class GameServer : MonoBehaviour
    {
#if UNITY_SERVER || UNITY_EDITOR

#if UNITY_EDITOR
        // ReSharper disable once InconsistentNaming
        public static bool START_SERVER_IN_UNITY_EDITOR = true;
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
            string path = PathRegister.Server_ConfigPath_UnityEditor;
#else
            string path = PathRegister.Server_ConfigPath;
#endif
            StreamReader reader = new StreamReader(path);
            string json = reader.ReadToEnd();
            reader.Close();
            Config = JsonConvert.DeserializeObject<ServerConfig>(json);
        }

        private void Start()
        {
            ServerStatusService.Instance.SendServerStatusPostRequest(new ServerStatus()
            {
                name = Config.name,
                ip = Config.ip,
                maxConnections = Config.maxConnections,
                location = Config.location,
                status = "online",
                port = Config.port
            });
        }

        private void OnApplicationQuit()
        {
#if UNITY_SERVER
            StopServer();
            return;
#elif UNITY_EDITOR
            if (START_SERVER_IN_UNITY_EDITOR)
            {
                StopServer();
                return;
            }
#endif
            EmberfateNetworkManager.Instance.StopClient();
        }

        #endregion

        public void StartServer()
        {
            EmberfateNetworkManager.Instance.networkAddress = Config.ip;
            EmberfateNetworkManager.Instance.maxConnections = Config.maxConnections;
            EmberfateNetworkManager.Instance.GetComponent<KcpTransport>().Port = (ushort) Config.port;
            EmberfateNetworkManager.Instance.StartServer();
        }

        public void StopServer()
        {
            ServerLogger.LogWarning("Stopping server...");
            ServerStatusService.Instance.SendServerStatusPostRequest(new ServerStatus()
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
#endif
    }
}