using System;
using System.Linq;
using Mirror;
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

        #endregion


        public static CharacterService Instance;

        public UnityEvent characterListChanged;
        public UnityEvent characterCreationAnswer;


        public CharacterInfo[] characterInfos;

        private const short ResponseCodeOk = 200;
        private const short ResponseCodeError = 401;


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
        }

        public void RegisterServerHandlers()
        {
            NetworkServer.RegisterHandler<CharacterListRequest>(OnCharacterListRequest);
            NetworkServer.RegisterHandler<CharacterCreationRequest>(OnCharacterCreationRequest);
            NetworkServer.RegisterHandler<CharacterDeletionRequest>(OnCharacterDeletionRequest);
        }

        #region Character List

        public void SendCharacterListRequest()
        {
            Debug.Log("Asking server for character list.");
            NetworkClient.Send(new CharacterListRequest());
        }

        private void OnCharacterListResponse(NetworkConnection conn, CharacterListResponse msg)
        {
            Debug.Log("Retrieved new character list!");
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
            Debug.Log("Asking server to create character.");
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
                    Message = "Created"
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
                Debug.Log("Success." + msg.Message);
                characterInfos = msg.CharacterInfos;
                characterListChanged.Invoke();
            }
            else
            {
                Debug.Log("Failed to create character." + msg.Message);
            }

            characterCreationAnswer.Invoke();
        }

        #endregion

        #region Character Deletion

        public void SendCharacterDeletionRequest(string characterName)
        {
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
                    Message = "Deleted"
                });
            }
            catch (Exception e)
            {
                ServerLogger.LogMessage(e.Message, ServerLogger.LogType.Error);
                conn.Send(new CharacterDeletionResponse()
                {
                    Code = ResponseCodeError,
                    Message = "Failed to delete character."
                });
            }
        }

        private void OnCharacterDeletionResponse(NetworkConnection conn, CharacterDeletionResponse msg)
        {
            if (msg.Code == ResponseCodeOk)
            {
                Debug.Log("Deletion success." + msg.Message);
                characterInfos = msg.CharacterInfos;
                characterListChanged.Invoke();
            }
            else
            {
                Debug.Log("Failed to delete character." + msg.Message);
            }
        }

        #endregion
    }
}