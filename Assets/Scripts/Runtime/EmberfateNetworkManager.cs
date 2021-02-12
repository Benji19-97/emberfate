using System;
using FirstGearGames.FlexSceneManager;
using FirstGearGames.FlexSceneManager.LoadUnloadDatas;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Runtime
{
    public class EmberfateNetworkManager : NetworkManager
    {
        [SerializeField] private GameObject loginMenu;
        [SerializeField] private GameObject characterSelectionMenu;
        // [Header("Scenes")]
        // [Scene][SerializeField] private string characterSelectionScene;
        
        private void OnServerInitialized()
        {
            Console.WriteLine("Server initialized!");
        }
        
        public override void OnClientConnect(NetworkConnection conn)
        {
            loginMenu.SetActive(false);
            characterSelectionMenu.SetActive(true);
            CharacterService.Instance.RegisterClientHandler();
            CharacterService.Instance.SendCharacterListRequest();
        }

        public override void OnClientDisconnect(NetworkConnection conn)
        {
            loginMenu.SetActive(true);
            characterSelectionMenu.SetActive(false);
        }

        public override void OnServerConnect(NetworkConnection conn)
        {
            base.OnServerConnect(conn);
            FlexSceneManager.OnServerConnect(conn);
            Console.WriteLine(conn + " connected to server.");
        }
        
        public override void OnServerDisconnect(NetworkConnection conn)
        {
            FlexSceneManager.OnServerDisconnect(conn);
            base.OnServerDisconnect(conn);
        }

        public override void OnServerAddPlayer(NetworkConnection conn)
        {
            // GameObject player = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
            // NetworkServer.AddPlayerForConnection(conn, player);
        }
    }
}