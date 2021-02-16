using System;
using System.Collections;
using System.Linq;
using FirstGearGames.FlexSceneManager;
using FirstGearGames.FlexSceneManager.LoadUnloadDatas;
using Mirror;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Runtime.Endpoints;
using Runtime.Helpers;
using Runtime.Models;
using Runtime.UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using CharacterInfo = Runtime.Models.CharacterInfo;

namespace Runtime
{
    public class CharacterService : MonoBehaviour
    {
        public static CharacterService Instance;

        public UnityEvent characterListChanged;
        public UnityEvent characterCreationAnswer;
        
        public Character[] characters;

        private const short ResponseCodeOk = 200;
        private const short ResponseCodeError = 401;


        [Header("Scenes")] [Scene] [SerializeField]
        private string townHubScene;
        
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

        #region Registering Handlers

        public void RegisterClientHandlers()
        {
            NetworkClient.RegisterHandler<CharacterCreationResponse>(OnCharacterCreationResponse);
            NetworkClient.RegisterHandler<CharacterDeletionResponse>(OnCharacterDeletionResponse);
            NetworkClient.RegisterHandler<CharacterPlayResponse>(OnCharacterPlayResponse);
        }

#if UNITY_SERVER || UNITY_EDITOR
        private void RegisterServerHandlers()
        {
            NetworkServer.RegisterHandler<CharacterCreationRequest>(OnCharacterCreationRequest);
            NetworkServer.RegisterHandler<CharacterDeletionRequest>(OnCharacterDeletionRequest);
            NetworkServer.RegisterHandler<CharacterPlayRequest>(OnCharacterPlayRequest);
        }
#endif

        #endregion

        #region Character Fetching

        public void FetchCharacters()
        {
            StartCoroutine(FetchCharactersCoroutine());
        }

        private IEnumerator FetchCharactersCoroutine()
        {
            NotificationSystem.Push("Retrieving characters ...", false);

            using (UnityWebRequest webRequest =
                UnityWebRequest.Get(EndpointRegister.GetClientFetchAllCharactersUrl(Steamworks.SteamUser.GetSteamID().m_SteamID.ToString(),
                    SteamTokenAuthenticator.AuthTicket)))
            {
                yield return webRequest.SendWebRequest();

                if (webRequest.isNetworkError || webRequest.responseCode != 200)
                {
                    NotificationSystem.Push("Couldn't retrieve character list. Network Error: " + webRequest.error, true);
                }
                else
                {
                    try
                    {
                        NotificationSystem.Push("Retrieved characters from Game Server.", true);
                        var jArray = JArray.Parse(webRequest.downloadHandler.text);
                        var newCharacters = new Character[jArray.Count];
                        for (int i = 0; i < jArray.Count; i++)
                        {
                            newCharacters[i] = Character.Deserialize(jArray[i].ToString());
                        }

                        characters = newCharacters;
                        characterListChanged.Invoke();
                    }
                    catch (Exception e)
                    {
                        NotificationSystem.Push(e.Message, false);
                        throw;
                    }
                }
            }
        }

#if UNITY_SERVER || UNITY_EDITOR

        /// <summary>Server sends web request, fetching a character. After receiving character, server sets profiles 'PlayingCharacter' to fetched character.</summary>
        private IEnumerator ServerFetchCharacterCoroutine(NetworkConnection conn, string characterId, bool recursiveCall = false)
        {
            using (var webRequest =
                UnityWebRequest.Get(EndpointRegister.GetServerFetchCharacterUrl(characterId, ServerAuthenticationService.Instance.serverAuthToken)))
            {
                yield return webRequest.SendWebRequest();

                if (webRequest.isNetworkError)
                {
                    ServerLogger.LogError(webRequest.error);
                    yield break;
                }

                if (webRequest.isHttpError)
                {
                    if (!recursiveCall)
                    {
                        yield return StartCoroutine(ServerAuthenticationService.Instance.FetchAuthTokenCoroutine());

                        if (ServerAuthenticationService.Instance.serverAuthToken != null)
                        {
                            StartCoroutine(ServerFetchCharacterCoroutine(conn, characterId, true));
                        }
                    }
                }
                else
                {
                    ProfileService.Instance.ConnectionInfos[conn].PlayingCharacter = Character.Deserialize(webRequest.downloadHandler.text);
                }
            }
        }
#endif

        #endregion

        #region Character Creation

        public void SendCharacterCreationRequest(string characterName, string characterClass)
        {
            NotificationSystem.Push("Creating character ...", false);
            NetworkClient.Send(new CharacterCreationRequest()
            {
                Name = characterName,
                Class = characterClass
            });
            NotificationSystem.Push("Creating character...", false);
        }

#if UNITY_SERVER || UNITY_EDITOR
        [Server]
        private void OnCharacterCreationRequest(NetworkConnection conn, CharacterCreationRequest msg)
        {
            if (ProfileService.Instance.ConnectionInfos[conn].PlayingCharacter != null)
            {
                conn.Send(new CharacterCreationResponse()
                {
                    Code = 403,
                    Message = "You cannot perform this operation while playing a character."
                });
            }

            var profile = ProfileService.Instance.ConnectionInfos[conn];

            string failMessage = "";

            if (profile.characters.Length >= profile.maxCharacterCount)
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
                StartCoroutine(CreateCharacterCoroutine(conn, new CharacterData()
                {
                    name = msg.Name,
                    @class = msg.Class,
                    level = 0
                }));
                return;
            }

            conn.Send(new CharacterCreationResponse()
            {
                Code = ResponseCodeError,
                Message = failMessage
            });
        }

        private IEnumerator CreateCharacterCoroutine(NetworkConnection conn, CharacterData data, bool recursiveCall = false)
        {
            using (UnityWebRequest webRequest =
                new UnityWebRequest(
                    EndpointRegister.GetServerCreateCharacterUrl(data.name, ProfileService.Instance.ConnectionInfos[conn].steamId,
                        ServerAuthenticationService.Instance.serverAuthToken),
                    "POST"))
            {
                webRequest.uploadHandler = new UploadHandlerRaw(data.Serialize());
                webRequest.downloadHandler = new DownloadHandlerBuffer();

                yield return webRequest.SendWebRequest();

                if (webRequest.isNetworkError)
                {
                    ServerLogger.LogError(webRequest.error);
                    yield break;
                }

                if (webRequest.isHttpError)
                {
                    if (!recursiveCall)
                    {
                        yield return StartCoroutine(ServerAuthenticationService.Instance.FetchAuthTokenCoroutine());

                        if (ServerAuthenticationService.Instance.serverAuthToken != null)
                        {
                            StartCoroutine(CreateCharacterCoroutine(conn, data, true));
                            yield break;
                        }
                    }

                    conn.Send(new CharacterCreationResponse()
                    {
                        Code = ResponseCodeError,
                        Message = "Failed to create character."
                    });
                    yield break;
                }

                conn.Send(new CharacterCreationResponse()
                {
                    Code = ResponseCodeOk,
                    Message = data.name
                });

                StartCoroutine(ProfileService.Instance.FetchProfileCoroutine(conn, ProfileService.Instance.ConnectionInfos[conn].steamId));
            }
        }
#endif
        private void OnCharacterCreationResponse(NetworkConnection conn, CharacterCreationResponse msg)
        {
            if (msg.Code == ResponseCodeOk)
            {
                NotificationSystem.Push("Successfully created character: " + msg.Message, true);
                FetchCharacters();
            }
            else
            {
                NotificationSystem.Push("Failed to create character: " + msg.Message, true);
            }

            characterCreationAnswer.Invoke();
        }

        #endregion

        #region Character Deletion

        public void SendCharacterDeletionRequest(string characterId)
        {
            NetworkClient.Send(new CharacterDeletionRequest()
            {
                CharacterId = characterId
            });
            NotificationSystem.Push("Deleting character ...", false);
        }

#if UNITY_SERVER || UNITY_EDITOR
        [Server]
        private void OnCharacterDeletionRequest(NetworkConnection conn, CharacterDeletionRequest msg)
        {
            if (ProfileService.Instance.ConnectionInfos[conn].PlayingCharacter != null)
            {
                conn.Send(new CharacterCreationResponse()
                {
                    Code = 403,
                    Message = "You cannot perform this operation while playing a character."
                });
            }

            var connectionInfo = ProfileService.Instance.ConnectionInfos[conn];

            if (connectionInfo.characters.Any(c => c.characterId == msg.CharacterId))
            {
                StartCoroutine(DeleteCharacterCoroutine(conn, msg.CharacterId));
            }
        }

        private IEnumerator DeleteCharacterCoroutine(NetworkConnection conn, string characterId, bool recursiveCall = false)
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Delete(
                EndpointRegister.GetServerDeleteCharacterUrl(characterId, ServerAuthenticationService.Instance.serverAuthToken)
            ))
            {
                yield return webRequest.SendWebRequest();

                if (webRequest.isNetworkError)
                {
                    ServerLogger.LogError(webRequest.error);
                    yield break;
                }

                if (webRequest.isHttpError)
                {
                    if (!recursiveCall)
                    {
                        yield return StartCoroutine(ServerAuthenticationService.Instance.FetchAuthTokenCoroutine());

                        if (ServerAuthenticationService.Instance.serverAuthToken != null)
                        {
                            StartCoroutine(DeleteCharacterCoroutine(conn, characterId, true));
                            yield break;
                        }
                    }

                    conn.Send(new CharacterDeletionResponse()
                    {
                        Code = ResponseCodeError,
                        Message = "Couldn't delete character."
                    });
                    yield break;
                }

                conn.Send(new CharacterCreationResponse()
                {
                    Code = ResponseCodeOk,
                    Message = characterId
                });
                StartCoroutine(ProfileService.Instance.FetchProfileCoroutine(conn, ProfileService.Instance.ConnectionInfos[conn].steamId));
            }
        }
#endif

        private void OnCharacterDeletionResponse(NetworkConnection conn, CharacterDeletionResponse msg)
        {
            if (msg.Code == ResponseCodeOk)
            {
                NotificationSystem.Push("Successfully deleted character: " + msg.Message, true);
                FetchCharacters();
            }
            else
            {
                NotificationSystem.Push("Failed to delete character: " + msg.Message, true);
            }
        }

        #endregion

        #region Character Playing

        [Client]
        public void SendCharacterPlayRequest(string characterId)
        {
            NetworkClient.Send(new CharacterPlayRequest()
            {
                CharacterId = characterId
            });
            NotificationSystem.Push("Sent character play request for " + characterId, false);
        }

#if UNITY_SERVER || UNITY_EDITOR
        [Server]
        private void OnCharacterPlayRequest(NetworkConnection conn, CharacterPlayRequest msg)
        {
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
                foreach (var characterInfo in ProfileService.Instance.ConnectionInfos[conn].characters)
                {
                    if (characterInfo.characterId == msg.CharacterId)
                    {
                        StartCoroutine(LoadCharacterCoroutine(conn, characterInfo.characterId));
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                ServerLogger.LogError(e.Message);
            }
        }

        private IEnumerator LoadCharacterCoroutine(NetworkConnection conn, string characterId)
        {
            yield return StartCoroutine(ServerFetchCharacterCoroutine(conn, characterId));

            if (ProfileService.Instance.ConnectionInfos[conn].PlayingCharacter == null)
            {
                conn.Send(new CharacterPlayResponse()
                {
                    Code = 400,
                    Message = "Cannot find character."
                });
            }

            var player = InstantiatePlayerCharacter(conn);
            var identity = player.GetComponent<NetworkIdentity>();
            FlexSceneManager.LoadConnectionScenes(conn, new SingleSceneData(townHubScene, new NetworkIdentity[] {identity}), null);

            conn.Send(new CharacterPlayResponse()
            {
                Code = 200,
                Message = "Ok"
            });
        }

        private GameObject InstantiatePlayerCharacter(NetworkConnection conn)
        {
            var startPos = Vector3.one;
            var player = Instantiate(EmberfateNetworkManager.Instance.playerPrefab, startPos, Quaternion.identity);
            NetworkServer.AddPlayerForConnection(conn, player);
            return player;
        }
#endif

        private void OnCharacterPlayResponse(NetworkConnection conn, CharacterPlayResponse msg)
        {
            Debug.Log(msg.Code + ": " + msg.Message);
            NetworkServer.SetClientReady(NetworkClient.connection);
        }

        #endregion
    }
}