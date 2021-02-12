using System;
using Mirror;
using UnityEngine;
using UnityEngine.Events;

namespace Runtime
{
    public class CharacterService : MonoBehaviour
    {
        public struct CharacterListRequest : NetworkMessage
        {
        }

        public struct CharacterListResponse : NetworkMessage
        {
            public CharacterInfo[] CharacterInfos;
        }

        public static CharacterService Instance;

        public UnityEvent characterListChanged;

        public CharacterInfo[] characterInfos;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
#if UNITY_SERVER
            RegisterServerHandler();
            return;
#endif
        }


        public void RegisterClientHandler()
        {
            NetworkClient.RegisterHandler<CharacterListResponse>(OnCharacterListResponse);
            Debug.Log("Registered CharacterListResponse Handler");
        }

        private void OnCharacterListResponse(NetworkConnection conn, CharacterListResponse msg)
        {
            Debug.Log("Retrieved new character list!");
            characterInfos = msg.CharacterInfos;
            characterListChanged.Invoke();
        }

        [ClientCallback]
        public void SendCharacterListRequest()
        {
            Debug.Log("Asking server for character list.");
            NetworkClient.Send(new CharacterListRequest());
        }

        public void RegisterServerHandler()
        {
            NetworkServer.RegisterHandler<CharacterListRequest>(OnCharacterListRequest);
        }

        private void OnCharacterListRequest(NetworkConnection conn, CharacterListRequest msg)
        {
            //TODO: Fetch characters from database
            ServerLogger.LogMessage(EmberfateNetworkManager.Instance.ConnectionInfos[conn].playerName + "[" + EmberfateNetworkManager.Instance.ConnectionInfos[conn].steamId + "]" + " requested character list. Fetching characters.", ServerLogger.LogType.Info);
            var characterListMock = new CharacterListResponse()
            {
                CharacterInfos = new CharacterInfo[3]
                {
                    new CharacterInfo()
                    {
                        name = "Tyrael",
                        @class = "Paladin",
                        level = 43
                    },
                    new CharacterInfo()
                    {
                        name = "Doedre",
                        @class = "Witch",
                        level = 60
                    },
                    new CharacterInfo()
                    {
                        name = "Valla",
                        @class = "Demon Hunter",
                        level = 12
                    }
                }
            };

            ServerLogger.LogMessage("Sending CharacterListResponse to " + EmberfateNetworkManager.Instance.ConnectionInfos[conn].playerName + "[" + EmberfateNetworkManager.Instance.ConnectionInfos[conn].steamId + "]", ServerLogger.LogType.Success);
            conn.Send(characterListMock);
        }
    }
    

}