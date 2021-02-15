using System;
using System.Collections;
using System.Linq;
using FirstGearGames.FlexSceneManager;
using FirstGearGames.FlexSceneManager.LoadUnloadDatas;
using Mirror;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Runtime.Endpoints;
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
            public string CharacterId;
        }

        public struct CharacterDeletionResponse : NetworkMessage
        {
            public short Code;
            public string Message;
        }

        public struct CharacterPlayRequest : NetworkMessage
        {
            public string CharacterId;
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


        [Header("Scenes")] [Scene] [SerializeField]
        private string townHubScene;


        #region Unity Event functions

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
            }
        }

        private void Start()
        {
#if UNITY_SERVER
            RegisterServerHandlers();
            return;
#endif
#if UNITY_EDITOR

            if (GameServer.START_SERVER_IN_UNITY_EDITOR)
            {
                RegisterServerHandlers();
            }
#endif
        }

        #endregion


        public void RegisterClientHandlers()
        {
            NetworkClient.RegisterHandler<CharacterCreationResponse>(OnCharacterCreationResponse);
            NetworkClient.RegisterHandler<CharacterDeletionResponse>(OnCharacterDeletionResponse);
            NetworkClient.RegisterHandler<CharacterPlayResponse>(OnCharacterPlayResponse);
        }

        public void RegisterServerHandlers()
        {
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

            using (UnityWebRequest webRequest =
                UnityWebRequest.Get(EndpointRegister.GetClientFetchAllCharactersUrl(Steamworks.SteamUser.GetSteamID().m_SteamID.ToString(),
                    SteamTokenAuthenticator.AuthTicket)))
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

        private IEnumerator ServerFetchCharacter(NetworkConnection conn, string characterId, bool recursiveCall = false)
        {
#if UNITY_SERVER || UNITY_EDITOR

            using (UnityWebRequest webRequest =
                UnityWebRequest.Get(EndpointRegister.GetServerFetchCharacterUrl(characterId, ServerAuthenticator.Instance.authToken)))
            {
                yield return webRequest.SendWebRequest();


                if (webRequest.isNetworkError || webRequest.responseCode != 200)
                {
                    ServerLogger.LogMessage("was error", ServerLogger.LogType.Message);


                    //if error and unauthorized
                    if (!recursiveCall)
                    {
                        //get new token
                        yield return StartCoroutine(ServerAuthenticator.Instance.GetAuthTokenRequest());

                        if (ServerAuthenticator.Instance.authToken != null)
                        {
                            StartCoroutine(ServerFetchCharacter(conn, characterId, true));
                        }
                    }
                }
                else
                {
                    ServerLogger.LogMessage("was success", ServerLogger.LogType.Message);

                    ProfileService.Instance.ConnectionInfos[conn].PlayingCharacter = Character.Deserialize(webRequest.downloadHandler.text);
                }
            }
#else
            yield break;
#endif
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
            NotificationSystem.Instance.PushNotification("Sent character create request,  " + characterName + " " + characterClass, false);
        }

        private void OnCharacterCreationRequest(NetworkConnection conn, CharacterCreationRequest msg)
        {
#if UNITY_SERVER || UNITY_EDITOR
            if (ProfileService.Instance.ConnectionInfos[conn].PlayingCharacter != null)
            {
                conn.Send(new CharacterCreationResponse()
                {
                    Code = 403,
                    Message = "You cannot perform this operation while playing a character."
                });
            }

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
            }

            conn.Send(new CharacterCreationResponse()
            {
                Code = ResponseCodeError,
                Message = failMessage
            });
#endif
        }

        private IEnumerator PostNewCharacter(NetworkConnection conn, CharacterData data, bool recursiveCall = false)
        {
#if UNITY_SERVER || UNITY_EDITOR
            using (UnityWebRequest webRequest =
                new UnityWebRequest(
                    EndpointRegister.GetServerCreateCharacterUrl(data.name, ProfileService.Instance.ConnectionInfos[conn].steamId,
                        ServerAuthenticator.Instance.authToken),
                    "POST"))
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
                            ServerLogger.LogMessage("Trying again to create character", ServerLogger.LogType.Info);
                            StartCoroutine(PostNewCharacter(conn, data, true));
                        }
                    }
                    else
                    {
                        ServerLogger.LogMessage("Error while trying to create character data " + webRequest.error + webRequest.downloadHandler.text,
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

                    StartCoroutine(ProfileService.Instance.GetProfile(conn, ProfileService.Instance.ConnectionInfos[conn].steamId));
                }
            }
#else
            yield break;
#endif
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

        public void SendCharacterDeletionRequest(string characterId)
        {
            NotificationSystem.Instance.PushNotification("Deleting character ...", false);
            NetworkClient.Send(new CharacterDeletionRequest()
            {
                CharacterId = characterId
            });
            NotificationSystem.Instance.PushNotification("Sent character delete request for " + characterId, false);
        }

        private void OnCharacterDeletionRequest(NetworkConnection conn, CharacterDeletionRequest msg)
        {
#if UNITY_SERVER || UNITY_EDITOR
            if (ProfileService.Instance.ConnectionInfos[conn].PlayingCharacter != null)
            {
                conn.Send(new CharacterCreationResponse()
                {
                    Code = 403,
                    Message = "You cannot perform this operation while playing a character."
                });
            }

            ;

            var connectionInfo = ProfileService.Instance.ConnectionInfos[conn];

            if (connectionInfo.characters.Any(c => c.characterId == msg.CharacterId))
            {
                StartCoroutine(DeleteCharacter(conn, msg.CharacterId));
            }
#endif
        }

        private IEnumerator DeleteCharacter(NetworkConnection conn, string characterId, bool recursiveCall = false)
        {
#if UNITY_SERVER || UNITY_EDITOR

            using (UnityWebRequest webRequest = UnityWebRequest.Delete(
                EndpointRegister.GetServerDeleteCharacterUrl(characterId, ServerAuthenticator.Instance.authToken)
            ))
            {
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
                            ServerLogger.LogMessage("Aborting DeleteCharacter call!", ServerLogger.LogType.Error);
                            conn.Send(new CharacterCreationResponse()
                            {
                                Code = ResponseCodeError,
                                Message = "Couldn't delete"
                            });
                        }
                        else //if token is not null, we can try again to post status
                        {
                            ServerLogger.LogMessage("Trying again to delete character.", ServerLogger.LogType.Info);
                            StartCoroutine(DeleteCharacter(conn, characterId, true));
                        }
                    }
                    else
                    {
                        ServerLogger.LogMessage("Error while trying to delete character " + webRequest.error + webRequest.downloadHandler.text,
                            ServerLogger.LogType.Error);
                        ServerLogger.LogMessage("Aborting DeleteCharacter call!", ServerLogger.LogType.Error);
                        conn.Send(new CharacterDeletionResponse()
                        {
                            Code = ResponseCodeError,
                            Message = "Couldn't delete"
                        });
                    }
                }
                else
                {
                    ServerLogger.LogMessage("Successfully deleted character.", ServerLogger.LogType.Success);
                    conn.Send(new CharacterCreationResponse()
                    {
                        Code = ResponseCodeOk,
                        Message = characterId
                    });

                    StartCoroutine(ProfileService.Instance.GetProfile(conn, ProfileService.Instance.ConnectionInfos[conn].steamId));
                }
            }
#else
            yield break;
#endif
        }

        private void OnCharacterDeletionResponse(NetworkConnection conn, CharacterDeletionResponse msg)
        {
            if (msg.Code == ResponseCodeOk)
            {
                NotificationSystem.Instance.PushNotification("Successfully deleted character: " + msg.Message, true);
                GetCharactersFromDatabase();
            }
            else
            {
                NotificationSystem.Instance.PushNotification("Failed to delete character: " + msg.Message, true);
            }
        }

        #endregion

        #region Character Play

        public void SendCharacterPlayRequest(string characterId)
        {
            NetworkClient.Send(new CharacterPlayRequest()
            {
                CharacterId = characterId
            });
            NotificationSystem.Instance.PushNotification("Sent character play request for " + characterId, false);
        }

        private void OnCharacterPlayRequest(NetworkConnection conn, CharacterPlayRequest msg)
        {
#if UNITY_SERVER || UNITY_EDITOR

            ServerLogger.LogMessage(conn + " wants to play character " + msg.CharacterId, ServerLogger.LogType.Info);

            if (ProfileService.Instance.ConnectionInfos[conn].PlayingCharacter != null)
            {
                conn.Send(new CharacterPlayResponse()
                {
                    Code = 403,
                    Message = "You cannot perform this operation while playing a character."
                });
            }

            try
            {
                ServerLogger.LogMessage("Looking for this id: " + msg.CharacterId, ServerLogger.LogType.Info);

                foreach (var characterInfo in ProfileService.Instance.ConnectionInfos[conn].characters)
                {
                    ServerLogger.LogMessage(characterInfo.characterId + " = " + characterInfo.characterName, ServerLogger.LogType.Info);

                    if (characterInfo.characterId == msg.CharacterId)
                    {
                        ServerLogger.LogMessage("Found character!", ServerLogger.LogType.Info);
                        StartCoroutine(LoadCharacterForPlayer(conn, msg.CharacterId));
                    }
                }
            }
            catch (Exception e)
            {
                ServerLogger.LogMessage(e.Message, ServerLogger.LogType.Error);
            }
#endif
        }

        private IEnumerator LoadCharacterForPlayer(NetworkConnection conn, string characterId)
        {
#if UNITY_SERVER || UNITY_EDITOR
            ServerLogger.LogMessage("Started LoadCharacterForPlayer coroutine", ServerLogger.LogType.Message);
            yield return StartCoroutine(ServerFetchCharacter(conn, characterId));

            ServerLogger.LogMessage("Ended LoadCharacterForPlayer coroutine", ServerLogger.LogType.Message);


            if (ProfileService.Instance.ConnectionInfos[conn].PlayingCharacter == null)
            {
                conn.Send(new CharacterPlayResponse()
                {
                    Code = 400,
                    Message = "Cannot find character."
                });
            }

            var player = LoadPlayerCharacter(conn);
            var identity = player.GetComponent<NetworkIdentity>();
            FlexSceneManager.LoadConnectionScenes(conn, new SingleSceneData(townHubScene, new NetworkIdentity[] {identity}), null);


            conn.Send(new CharacterPlayResponse()
            {
                Code = 200,
                Message = "Ok"
            });
#else
            yield break;
#endif
        }

        private void OnCharacterPlayResponse(NetworkConnection conn, CharacterPlayResponse msg)
        {
            Debug.Log(msg.Code + ": " + msg.Message);
            NetworkServer.SetClientReady(NetworkClient.connection);
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