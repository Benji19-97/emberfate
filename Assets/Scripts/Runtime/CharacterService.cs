using System;
using System.Collections;
using System.Linq;
using FirstGearGames.FlexSceneManager;
using FirstGearGames.FlexSceneManager.LoadUnloadDatas;
using Mirror;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Runtime.Models;
using Runtime.UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using CharacterInfo = Runtime.Models.CharacterInfo;

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
            public string Name;
            public string @Class;
        }

        public struct CharacterCreationResponse : NetworkMessage
        {
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


        public Character[] characters;

        private const short ResponseCodeOk = 200;
        private const short ResponseCodeError = 401;

        private const string GetCharacterUri = "http://localhost:3003/api/characters/";
        private const string CreateCharacterUri = "http://localhost:3000/api/characters/create/";

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
            // NetworkClient.RegisterHandler<CharacterListResponse>(OnCharacterListResponse);
            NetworkClient.RegisterHandler<CharacterCreationResponse>(OnCharacterCreationResponse);
            NetworkClient.RegisterHandler<CharacterDeletionResponse>(OnCharacterDeletionResponse);
            NetworkClient.RegisterHandler<CharacterPlayResponse>(OnCharacterPlayResponse);
        }

        public void RegisterServerHandlers()
        {
            // NetworkServer.RegisterHandler<CharacterListRequest>(OnCharacterListRequest);
            NetworkServer.RegisterHandler<CharacterCreationRequest>(OnCharacterCreationRequest);
            NetworkServer.RegisterHandler<CharacterDeletionRequest>(OnCharacterDeletionRequest);
            NetworkServer.RegisterHandler<CharacterPlayRequest>(OnCharacterPlayRequest);
        }

        #region Character List

        public void GetCharactersFromDatabase()
        {
            StartCoroutine(FetchCharacters());
        }

        private IEnumerator FetchCharacters()
        {
            NotificationSystem.Instance.PushNotification("Retrieving characters ...", false);

            using (UnityWebRequest webRequest = UnityWebRequest.Get(GetCharacterUri + Steamworks.SteamUser.GetSteamID().m_SteamID + "/all/?token=" + SteamTokenAuthenticator.AuthTicket))
            {
                yield return webRequest.SendWebRequest();

                if (webRequest.isNetworkError || webRequest.responseCode != 200)
                {
                    NotificationSystem.Instance.PushNotification("Couldn't retrieve character list. Network Error: " + webRequest.error, true);
                }
                else
                {
                    NotificationSystem.Instance.PushNotification("Retrieved characters from Game Server.", true);

                    var jArray = JArray.Parse(webRequest.downloadHandler.text);

                    var newCharacters = new Character[jArray.Count];

                    for (int i = 0; i < jArray.Count; i++)
                    {
                        newCharacters[i] = Character.Deserialize(jArray[i].ToString());
                    }

                    characters = newCharacters;

                    characterListChanged.Invoke();
                }
            }
        }

        #endregion

        #region Character Creation

        public void SendCharacterCreationRequest(string characterName, string characterClass)
        {
            NotificationSystem.Instance.PushNotification("Creating character ...", false);
            NetworkClient.Send(new CharacterCreationRequest()
            {
                Name = characterName,
                Class = characterClass
            });
        }

        private void OnCharacterCreationRequest(NetworkConnection conn, CharacterCreationRequest msg)
        {
            var connectionInfo = ProfileService.Instance.ConnectionInfos[conn];
            var newCharacterInfos = connectionInfo.characters.ToList();

            string failMessage = "";

            if (connectionInfo.characters.Length >= connectionInfo.maxCharacterCount)
            {
                failMessage = "Reached Character Count Limit";
            }
            else if (msg.Name.Length <= 1)
            {
                failMessage = "Invalid Character Name";
            }
            else if (!Character.Classes.Contains(msg.Class))
            {
                failMessage = "Invalid Character Class";
            }
            else
            {
                CharacterData characterData = new CharacterData()
                {
                    name = msg.Name,
                    @class = msg.Class,
                    level = 0
                };
                StartCoroutine(PostNewCharacter(conn, characterData));
                return;
            }

            conn.Send(new CharacterCreationResponse()
            {
                Code = ResponseCodeError,
                Message = failMessage
            });
        }

        private IEnumerator PostNewCharacter(NetworkConnection conn, CharacterData data, bool recursiveCall = false)
        {
            using (UnityWebRequest webRequest = new UnityWebRequest(CreateCharacterUri + data.name + "/" + ProfileService.Instance.ConnectionInfos[conn].steamId + "/" + ServerAuthenticator.Instance.authToken, "POST"))
            {
                webRequest.uploadHandler = new UploadHandlerRaw(data.Serialize());
                webRequest.downloadHandler = new DownloadHandlerBuffer();

                yield return webRequest.SendWebRequest();

                if (webRequest.isNetworkError || webRequest.responseCode != 200)
                {
                    //if error and unauthorized
                    if (!recursiveCall)
                    {
                        //get new token
                        yield return StartCoroutine(ServerAuthenticator.Instance.GetAuthTokenRequest());

                        //if token is null, the token request failed
                        if (ServerAuthenticator.Instance.authToken == null)
                        {
                            ServerLogger.LogMessage("Error, auth token was null right after requesting.", ServerLogger.LogType.Error);
                            ServerLogger.LogMessage("Aborting PostNewCharacter call!", ServerLogger.LogType.Error);
                            conn.Send(new CharacterCreationResponse()
                            {
                                Code = ResponseCodeError,
                                Message = "Couldn't create"
                            });
                        }
                        else //if token is not null, we can try again to post status
                        {
                            ServerLogger.LogMessage("Trying again to update server status on API.", ServerLogger.LogType.Info);
                            StartCoroutine(PostNewCharacter(conn, data,true));
                        }
                    }
                    else
                    {
                        ServerLogger.LogMessage("Error while trying to push player data " + webRequest.error + webRequest.downloadHandler.text,
                            ServerLogger.LogType.Error);
                        ServerLogger.LogMessage("Aborting PostNewCharacter call!", ServerLogger.LogType.Error);
                        conn.Send(new CharacterCreationResponse()
                        {
                            Code = ResponseCodeError,
                            Message = "Couldn't create"
                        });
                    }
                }
                else
                {
                    ServerLogger.LogMessage("Successfully created new character.", ServerLogger.LogType.Success);
                    conn.Send(new CharacterCreationResponse()
                    {
                        Code = ResponseCodeOk,
                        Message = data.name
                    });
                }
            }
            

        }

        private void OnCharacterCreationResponse(NetworkConnection conn, CharacterCreationResponse msg)
        {
            if (msg.Code == ResponseCodeOk)
            {
                NotificationSystem.Instance.PushNotification("Successfully created character: " + msg.Message, true);
                GetCharactersFromDatabase();
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
            var connectionInfo = ProfileService.Instance.ConnectionInfos[conn];
            var newCharacterInfos = connectionInfo.characters.ToList();

            try
            {
                var character = newCharacterInfos.Find(info => info.characterName == msg.Name); //TODO: This can be done better or smoother
                newCharacterInfos.Remove(character);

                connectionInfo.characters = newCharacterInfos.ToArray();

                conn.Send(new CharacterDeletionResponse()
                {
                    Code = ResponseCodeOk,
                    CharacterInfos = connectionInfo.characters,
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

                // characters = msg.CharacterInfos;
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
                var matches = ProfileService.Instance.ConnectionInfos[conn].characters.Where(c => c.characterName == msg.Name);
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