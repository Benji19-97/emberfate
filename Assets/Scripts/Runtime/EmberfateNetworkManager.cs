using System;
using System.Collections.Generic;
using FirstGearGames.FlexSceneManager;
using FirstGearGames.FlexSceneManager.LoadUnloadDatas;
using Mirror;
using Runtime.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Runtime
{
    public class EmberfateNetworkManager : NetworkManager
    {
        public static EmberfateNetworkManager Instance;

        public override void Awake()
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

            base.Awake();
        }


        [Header("UI")] [SerializeField] private GameObject loginMenu;
        [SerializeField] private GameObject characterSelectionMenu;

        private void OnServerInitialized()
        {
            Console.WriteLine("Server initialized!");
        }

        public override void OnClientConnect(NetworkConnection conn)
        {
            loginMenu.SetActive(false);
            characterSelectionMenu.SetActive(true);
            CharacterService.Instance.RegisterClientHandlers();
            CharacterService.Instance.SendCharacterListRequest();
        }

        public void Disconnect()
        {
            StopClient();
            loginMenu.SetActive(true);
            characterSelectionMenu.SetActive(false);
        }

        public override void OnServerConnect(NetworkConnection conn)
        {
            base.OnServerConnect(conn);
            FlexSceneManager.OnServerConnect(conn);
            ServerLogger.LogMessage(PlayerDataService.Instance.ConnectionInfos[conn].name + " [" + PlayerDataService.Instance.ConnectionInfos[conn].steamId + "]" + " connected", ServerLogger.LogType.Info);
        }

        public override void OnServerDisconnect(NetworkConnection conn)
        {
            FlexSceneManager.OnServerDisconnect(conn);
            base.OnServerDisconnect(conn);
            if (PlayerDataService.Instance.ConnectionInfos.ContainsKey(conn))
            {
                StartCoroutine(PlayerDataService.Instance.PushPlayerData(conn, true));
                ServerLogger.LogMessage(PlayerDataService.Instance.ConnectionInfos[conn].name + " disconnected.", ServerLogger.LogType.Info);
            }
        }

        public override void OnStopServer()
        {
            ServerLogger.LogMessage("Stopping server...", ServerLogger.LogType.Info);
#if UNITY_SERVER
            GameServer.Instance.OnStopServer();
#endif
#if UNITY_EDITOR
            if (GameServer.START_SERVER_IN_UNITY_EDITOR)
            {
                GameServer.Instance.OnStopServer();
            }
#endif
            base.OnStopServer();
        }

        public override void OnStartServer()
        {
            ServerLogger.LogMessage(
                $"Started server {GameServer.Instance.Config.name}[{GameServer.Instance.Config.location}] " +
                $"on {GameServer.Instance.Config.ip}:{GameServer.Instance.Config.port} " +
                $"with {GameServer.Instance.Config.maxConnections} maximum connections.",
                ServerLogger.LogType.Success);
            base.OnStartServer();
        }
    }
}