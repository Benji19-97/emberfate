using System;
using System.IO;
using System.Runtime.CompilerServices;
using kcp2k;
using Mirror;
using Newtonsoft.Json;
using UnityEngine;

namespace Runtime
{
    public class GameServer : MonoBehaviour
    {
#if UNITY_SERVER || UNITY_EDITOR

        public static GameServer Instance { get; private set; }
        
        private const string ServerConfigPath = "data/config.json";

        public ServerConfig Config { get; private set; }

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
            ServerStatusService.Instance.SendPostRequest(new ServerStatus()
            {
                name = Config.name,
                ip = Config.ip,
                maxConnections = Config.maxConnections,
                location = Config.location,
                status = Config.status
            });
            NetworkManager.singleton.networkAddress = Config.ip;
            NetworkManager.singleton.GetComponent<KcpTransport>().Port = (ushort) Config.port;
            NetworkManager.singleton.StartServer();
            Debug.Log($"NetworkManager: Started server! \n name: {Config.name}\n ip: {Config.ip}:{Config.port} \n maxConnections: {Config.maxConnections}\n location: {Config.location}\n");
        }

#endif
    }
}