using System;
using System.Linq;
using FirstGearGames.FlexSceneManager;
using FirstGearGames.FlexSceneManager.LoadUnloadDatas;
using Mirror;
using Runtime.UI;
using UnityEngine;
using UnityEngine.Events;

namespace Runtime
{
    //TODO: set the 'ClientCallback' and 'ServerCallback' attributes and also compiler stuff to strip server code, do it in the whole class
    //TODO: exception handling
    public class CharacterService : MonoBehaviour
    {
        #region NetworkMessages

        public struct CharacterListRequest : NetworkMessage
        {
        }

        public struct CharacterListResponse : NetworkMessage
        {
            public CharacterInfo[] CharacterInfos;
        }

        public struct CharacterCreationRequest : NetworkMessage
        {
            public CharacterInfo CharacterInfo;
        }

        public struct CharacterCreationResponse : NetworkMessage
        {
            public CharacterInfo[] CharacterInfos;
            public short Code;
            public string Message;
        }

        public struct CharacterDeletionRequest : NetworkMessage
        {
            public string Name;
        }

        public struct CharacterDeletionResponse : NetworkMessage
        {
            public CharacterInfo[] CharacterInfos;
            public short Code;
            public string Message;
        }

        public struct CharacterPlayRequest : NetworkMessage
        {
            public string Name;
        }

        public struct CharacterPlayResponse : NetworkMessage
        {
            public short Code;
            public string Message;
        }

        #endregion

        public static CharacterService Instance;

        public UnityEvent characterListChanged;
        public UnityEvent characterCreationAnswer;


        public CharacterInfo[] characterInfos;

        private const short ResponseCodeOk = 200;
        private const short ResponseCodeError = 401;

        [Header("Scenes")] [Scene] [SerializeField]
        private string townHubScene;


        #region Unity Event functions

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
            RegisterServerHandlers();
            return;
#endif
        }

        #endregion


        public void RegisterClientHandlers()
        {
            NetworkClient.RegisterHandler<CharacterListResponse>(OnCharacterListResponse);
            NetworkClient.RegisterHandler<CharacterCreationResponse>(OnCharacterCreationResponse);
            NetworkClient.RegisterHandler<CharacterDeletionResponse>(OnCharacterDeletionResponse);
            NetworkClient.RegisterHandler<CharacterPlayResponse>(OnCharacterPlayResponse);
        }

        public void RegisterServerHandlers()
        {
            NetworkServer.RegisterHandler<CharacterListRequest>(OnCharacterListRequest);
            NetworkServer.RegisterHandler<CharacterCreationRequest>(OnCharacterCreationRequest);
            NetworkServer.RegisterHandler<CharacterDeletionRequest>(OnCharacterDeletionRequest);
            NetworkServer.RegisterHandler<CharacterPlayRequest>(OnCharacterPlayRequest);
        }

        #region Character List

        public void SendCharacterListRequest()
        {
            NotificationSystem.Instance.PushNotification("Retrieving characters ...", false);
            NetworkClient.Send(new CharacterListRequest());
        }

        private void OnCharacterListResponse(NetworkConnection conn, CharacterListResponse msg)
        {
            //TODO: Fail state
            NotificationSystem.Instance.PushNotification("Retrieved characters from Game Server.", true);
            characterInfos = msg.CharacterInfos;
            characterListChanged.Invoke();
        }

        private void OnCharacterListRequest(NetworkConnection conn, CharacterListRequest msg)
        {
            //TODO: Fetch characters from database
            ServerLogger.LogMessage(
                EmberfateNetworkManager.Instance.ConnectionInfos[conn].playerName + "[" + EmberfateNetworkManager.Instance.ConnectionInfos[conn].steamId + "]" +
                " requested character list. Fetching characters.", ServerLogger.LogType.Info);
            var characterListResponse = new CharacterListResponse()
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

            ServerLogger.LogMessage(
                "Sending CharacterListResponse to " + EmberfateNetworkManager.Instance.ConnectionInfos[conn].playerName + "[" +
                EmberfateNetworkManager.Instance.ConnectionInfos[conn].steamId + "]", ServerLogger.LogType.Success);
            conn.Send(characterListResponse);

            EmberfateNetworkManager.Instance.ConnectionInfos[conn].characterInfos = characterListResponse.CharacterInfos;
        }

        #endregion

        #region Character Creation

        public void SendCharacterCreationRequest(CharacterInfo characterInfo)
        {
            NotificationSystem.Instance.PushNotification("Creating character ...", false);
            NetworkClient.Send(new CharacterCreationRequest()
            {
                CharacterInfo = characterInfo
            });
        }

        private void OnCharacterCreationRequest(NetworkConnection conn, CharacterCreationRequest msg)
        {
            var connectionInfo = EmberfateNetworkManager.Instance.ConnectionInfos[conn];
            var newCharacterInfos = connectionInfo.characterInfos.ToList();

            string failMessage = "";

            if (connectionInfo.characterInfos.Length >= connectionInfo.maxCharacterCount)
            {
                failMessage = "Reached Character Count Limit";
            }
            else if (msg.CharacterInfo.name.Length <= 1)
            {
                failMessage = "Invalid Character Name";
            }
            else if (msg.CharacterInfo.@class.Length <= 1)
            {
                failMessage = "Invalid Character Class";
            }
            else
            {
                //TODO: Check stuff like if name is unique, give it a new unique ID
                msg.CharacterInfo.level = 1; //TODO: ? is this nice way to do this?
                newCharacterInfos.Add(msg.CharacterInfo);

                connectionInfo.characterInfos = newCharacterInfos.ToArray();

                conn.Send(new CharacterCreationResponse()
                {
                    Code = ResponseCodeOk,
                    CharacterInfos = connectionInfo.characterInfos,
                    Message = msg.CharacterInfo.name
                });
                return;
            }

            conn.Send(new CharacterCreationResponse()
            {
                Code = ResponseCodeError,
                Message = failMessage
            });
        }

        private void OnCharacterCreationResponse(NetworkConnection conn, CharacterCreationResponse msg)
        {
            if (msg.Code == ResponseCodeOk)
            {
                NotificationSystem.Instance.PushNotification("Successfully created character: " + msg.Message, true);
                characterInfos = msg.CharacterInfos;
                characterListChanged.Invoke();
            }
            else
            {
                NotificationSystem.Instance.PushNotification("Failed to create character: " + msg.Message, true);
            }

            characterCreationAnswer.Invoke();
        }

        #endregion

        #region Character Deletion

        public void SendCharacterDeletionRequest(string characterName)
        {
            NotificationSystem.Instance.PushNotification("Deleting character ...", false);
            NetworkClient.Send(new CharacterDeletionRequest()
            {
                Name = characterName
            });
        }

        private void OnCharacterDeletionRequest(NetworkConnection conn, CharacterDeletionRequest msg)
        {
            var connectionInfo = EmberfateNetworkManager.Instance.ConnectionInfos[conn];
            var newCharacterInfos = connectionInfo.characterInfos.ToList();

            try
            {
                var character = newCharacterInfos.Find(info => info.name == msg.Name); //TODO: This can be done better or smoother
                newCharacterInfos.Remove(character);

                connectionInfo.characterInfos = newCharacterInfos.ToArray();

                conn.Send(new CharacterDeletionResponse()
                {
                    Code = ResponseCodeOk,
                    CharacterInfos = connectionInfo.characterInfos,
                    Message = msg.Name
                });
            }
            catch (Exception e)
            {
                ServerLogger.LogMessage(e.Message, ServerLogger.LogType.Error);
                conn.Send(new CharacterDeletionResponse()
                {
                    Code = ResponseCodeError,
                    Message = "Unknown Error"
                });
            }
        }

        private void OnCharacterDeletionResponse(NetworkConnection conn, CharacterDeletionResponse msg)
        {
            if (msg.Code == ResponseCodeOk)
            {
                NotificationSystem.Instance.PushNotification("Successfully deleted character: " + msg.Message, true);

                characterInfos = msg.CharacterInfos;
                characterListChanged.Invoke();
            }
            else
            {
                NotificationSystem.Instance.PushNotification("Failed to delete character: " + msg.Message, true);
            }
        }

        #endregion

        #region Character Play

        public void SendCharacterPlayRequest(string characterName)
        {
            NetworkClient.Send(new CharacterPlayRequest()
            {
                Name = characterName
            });
        }

        private void OnCharacterPlayRequest(NetworkConnection conn, CharacterPlayRequest msg)
        {
            ServerLogger.LogMessage("Player wants to play character " + msg.Name, ServerLogger.LogType.Info);
            try
            {
                var matches = EmberfateNetworkManager.Instance.ConnectionInfos[conn].characterInfos.Where(c => c.name == msg.Name);
                if (matches.Any())
                {
                    var player = LoadPlayerCharacter(conn);
                    var identity = player.GetComponent<NetworkIdentity>();
                    FlexSceneManager.LoadConnectionScenes(conn, new SingleSceneData(townHubScene, new NetworkIdentity[] {identity}), null);
                }
            }
            catch (Exception e)
            {
                ServerLogger.LogMessage(e.Message, ServerLogger.LogType.Error);
            }
        }

        private void OnCharacterPlayResponse(NetworkConnection conn, CharacterPlayResponse msg)
        {
        }
        
        private GameObject LoadPlayerCharacter(NetworkConnection conn)
        {
            var startPos = Vector3.one;
            var player = Instantiate(EmberfateNetworkManager.Instance.playerPrefab, startPos, Quaternion.identity);
            NetworkServer.AddPlayerForConnection(conn, player);
            return player;
        }

        #endregion



    }
}