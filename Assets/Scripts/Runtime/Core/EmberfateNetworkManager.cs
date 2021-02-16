using System;
using System.Collections;
using FirstGearGames.FlexSceneManager;
using Mirror;
using Runtime.Core.Server;
using Runtime.Helpers;
using Runtime.Services;
using UnityEngine;

namespace Runtime.Core
{
    public class EmberfateNetworkManager : NetworkManager
    {
        public static EmberfateNetworkManager Instance;

        [Header("UI")] [SerializeField] private GameObject loginMenu;
        [SerializeField] private GameObject characterSelectionMenu;

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

        public override void OnApplicationQuit()
        {
#if !UNITY_SERVER
            if (NetworkClient.isConnected)
            {
                Disconnect();
            }
#endif
        }

#if !UNITY_SERVER
        [Client]
        public override void OnClientConnect(NetworkConnection conn)
        {
            loginMenu.SetActive(false);
            characterSelectionMenu.SetActive(true);
            CharacterService.Instance.RegisterClientHandlers();
            CharacterService.Instance.FetchCharacters();
        }

        [Client]
        public void Disconnect()
        {
            StopClient();
            loginMenu.SetActive(true);
            characterSelectionMenu.SetActive(false);
        }
#endif

#if UNITY_SERVER || UNITY_EDITOR
        [Server]
        private void OnServerInitialized()
        {
            ServerLogger.LogSuccess("Server initialized.");
        }

        [Server]
        public override void OnServerConnect(NetworkConnection conn)
        {
            FlexSceneManager.OnServerConnect(conn);
            ServerLogger.Log(conn + " connected");
        }

        [Server]
        public override void OnServerDisconnect(NetworkConnection conn)
        {
            StartCoroutine(HandleClientDisconnectFromServer(conn));
        }

        [Server]
        private IEnumerator HandleClientDisconnectFromServer(NetworkConnection conn)
        {
            FlexSceneManager.OnServerDisconnect(conn);
            if (ProfileService.Instance.ConnectionInfos[conn].PlayingCharacter != null)
            {
                yield return StartCoroutine(
                    CharacterService.Instance.UpdateCharacterOnDatabaseCoroutine(ProfileService.Instance.ConnectionInfos[conn].PlayingCharacter));
            }

            yield return StartCoroutine(ProfileService.Instance.UpsertProfileCoroutine(conn, true));
            ServerLogger.Log(conn + " disconnected");
        }

        public override void OnStopServer()
        {
            //use GameServer 'StopServer' method instead of this
        }

        [Server]
        public override void OnStartServer()
        {
            ServerLogger.LogSuccess(
                $"Started server {GameServer.Instance.Config.name}[{GameServer.Instance.Config.location}] " +
                $"on {GameServer.Instance.Config.ip}:{GameServer.Instance.Config.port} " +
                $"with {GameServer.Instance.Config.maxConnections} maximum connections.");
        }
#endif
    }
}