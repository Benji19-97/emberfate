using System;
using System.IO;
using JetBrains.Annotations;
using kcp2k;
using Mirror;
using Newtonsoft.Json;
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

        private const string ServerConfigPath = "data/config.json";

        public ServerConfig Config { get; private set; }

        private void Awake()
        {
#if UNITY_EDITOR
            if (!START_SERVER_IN_UNITY_EDITOR)
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
                return;
            }

            string path = ServerConfigPath;
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

        public void StartServer()
        {
            NetworkManager.singleton.networkAddress = Config.ip;
            NetworkManager.singleton.maxConnections = Config.maxConnections;
            NetworkManager.singleton.GetComponent<TelepathyTransport>().port = (ushort) Config.port;
            NetworkManager.singleton.StartServer();
        }

        public void OnStopServer()
        {
            ServerStatusService.Instance.SendServerStatusPostRequest(new ServerStatus()
            {
                name = Config.name,
                ip = Config.ip,
                maxConnections = Config.maxConnections,
                location = Config.location,
                status = "offline",
                port = Config.port
            });
        }
#endif
    }
}