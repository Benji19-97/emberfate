using System;
using System.Collections.Generic;
using FirstGearGames.FlexSceneManager;
using FirstGearGames.FlexSceneManager.LoadUnloadDatas;
using Mirror;
using Runtime.Helpers;
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
            CharacterService.Instance.FetchCharacters();
        }

        public void Disconnect()
        {
            StopClient();
            loginMenu.SetActive(true);
            characterSelectionMenu.SetActive(false);
        }

        public override void OnServerConnect(NetworkConnection conn)
        {
            FlexSceneManager.OnServerConnect(conn);
            ServerLogger.Log(conn + " connected");
        }

        public override void OnServerDisconnect(NetworkConnection conn)
        {
            FlexSceneManager.OnServerDisconnect(conn);
            StartCoroutine(ProfileService.Instance.UpsertProfileCoroutine(conn, true));
            ProfileService.Instance.ConnectionInfos.Remove(conn);
            ServerLogger.Log(conn + " disconnected");
        }

        public override void OnStopServer()
        {
            //use GameServer 'StopServer' method instead of this
        }

        public override void OnStartServer()
        {
#if UNITY_SERVER || UNITY_EDITOR
            ServerLogger.LogSuccess(
                $"Started server {GameServer.Instance.Config.name}[{GameServer.Instance.Config.location}] " +
                $"on {GameServer.Instance.Config.ip}:{GameServer.Instance.Config.port} " +
                $"with {GameServer.Instance.Config.maxConnections} maximum connections.");
#endif
        }

        public override void OnApplicationQuit()
        {
        }
    }
}