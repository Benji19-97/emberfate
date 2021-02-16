using System;
using System.Collections;
using FirstGearGames.FlexSceneManager;
using FirstGearGames.FlexSceneManager.LoadUnloadDatas;
using Mirror;
using Runtime.Core.Server;
using Runtime.Helpers;
using Runtime.Services;
using Runtime.UI.Managers;
using UnityEngine;

namespace Runtime.Core
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
                base.Awake();
            }
            else
            {
                Destroy(gameObject);
            }
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
            MainMenuUiManager.Instance.loginMenu.SetActive(false);
            MainMenuUiManager.Instance.characterSelectionMenu.SetActive(true);
            CharacterService.Instance.RegisterClientHandlers();
            CharacterService.Instance.FetchCharacters();
            
            base.OnClientConnect(conn);
        }

        [Client]
        public void Disconnect()
        {
            StopClient();
            MainMenuUiManager.Instance.loginMenu.SetActive(true);
            MainMenuUiManager.Instance.characterSelectionMenu.SetActive(false);
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
            base.OnServerConnect(conn);
        }

        [Server]
        public override void OnServerDisconnect(NetworkConnection conn)
        {
            FlexSceneManager.OnServerDisconnect(conn);
            StartCoroutine(HandleClientDisconnectFromServer(conn));
            base.OnServerDisconnect(conn);
        }

        [Server]
        private IEnumerator HandleClientDisconnectFromServer(NetworkConnection conn)
        {
            if (ProfileService.Instance.ConnectionInfos[conn].PlayingCharacter != null)
            {
                yield return StartCoroutine(
                    CharacterService.Instance.UpdateCharacterOnDatabaseCoroutine(ProfileService.Instance.ConnectionInfos[conn].PlayingCharacter));
            }
            yield return StartCoroutine(StashService.Instance.UpsertStashCoroutine(conn));
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
            
            base.OnStartServer();
        }
#endif
    }
}