using System;
using System.Collections;
using System.Linq;
using FirstGearGames.FlexSceneManager;
using FirstGearGames.FlexSceneManager.LoadUnloadDatas;
using Mirror;
using Newtonsoft.Json.Linq;
using Runtime.Core;
using Runtime.Core.Server;
using Runtime.Helpers;
using Runtime.Models;
using Runtime.Registers;
using Runtime.UI;
using Steamworks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using GameServer = Runtime.Core.Server.GameServer;

namespace Runtime.Services
{
    public class CharacterService : MonoBehaviour
    {
        public static CharacterService Instance;

        private const short ResponseCodeOk = 200;
        private const short ResponseCodeError = 401;

        public UnityEvent characterListChanged;
        public UnityEvent characterCreationAnswer;

        public Character[] clientSideCharacterList;

        [Header("Scenes")] [Scene] [SerializeField]
        private string townHubScene;

        #region NetworkMessages

        private struct CharacterCreationRequest : NetworkMessage
        {
            public string Name;
            public string Class;
        }

        private struct CharacterCreationResponse : NetworkMessage
        {
            public short Code;
            public string Message;
        }

        private struct CharacterDeletionRequest : NetworkMessage
        {
            public string CharacterId;
        }

        private struct CharacterDeletionResponse : NetworkMessage
        {
            public short Code;
            public string Message;
        }

        private struct CharacterPlayRequest : NetworkMessage
        {
            public string CharacterId;
        }

        private struct CharacterPlayResponse : NetworkMessage
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

            if (GameServer.START_SERVER_IN_UNITY_EDITOR) RegisterServerHandlers();
#endif
        }

        #endregion

        #region Registering Handlers

#if !UNITY_SERVER
        public void RegisterClientHandlers()
        {
            NetworkClient.RegisterHandler<CharacterCreationResponse>(OnCharacterCreationResponse);
            NetworkClient.RegisterHandler<CharacterDeletionResponse>(OnCharacterDeletionResponse);
            NetworkClient.RegisterHandler<CharacterPlayResponse>(OnCharacterPlayResponse);
        }
#endif

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

#if !UNITY_SERVER
        [Client]
        public void FetchCharacters()
        {
            StartCoroutine(FetchCharactersCoroutine());
        }

        [Client]
        private IEnumerator FetchCharactersCoroutine()
        {
            NotificationSystem.Push("Retrieving characters ...", false);

            using (var webRequest =
                UnityWebRequest.Get(EndpointRegister.GetClientFetchAllCharactersUrl(SteamUser.GetSteamID().m_SteamID.ToString(),
                    SteamTokenAuthenticator.AuthTicket)))
            {
                yield return webRequest.SendWebRequest();

                if (webRequest.isNetworkError || webRequest.responseCode != 200)
                    NotificationSystem.Push("Couldn't retrieve character list. Network Error: " + webRequest.error, true);
                else
                    try
                    {
                        NotificationSystem.Push("Retrieved characters from Game Server.", true);
                        var jArray = JArray.Parse(webRequest.downloadHandler.text);
                        var newCharacters = new Character[jArray.Count];
                        for (var i = 0; i < jArray.Count; i++) newCharacters[i] = Character.Deserialize(jArray[i].ToString());

                        clientSideCharacterList = newCharacters;
                        characterListChanged.Invoke();
                    }
                    catch (Exception e)
                    {
                        NotificationSystem.Push(e.Message, false);
                        throw;
                    }
            }
        }
#endif

#if UNITY_SERVER || UNITY_EDITOR

        /// <summary>
        ///     Server sends web request, fetching a character. After receiving character, server sets profiles
        ///     'PlayingCharacter' to fetched character.
        /// </summary>
        private IEnumerator ServerFetchCharacterCoroutine(NetworkConnection conn, string characterId, bool recursiveCall = false)
        {
            ServerLogger.Log($"Started 'ServerFetchCharacterCoroutine'. Args(conn: {conn}, characterId: {characterId}, recursiveCall: {recursiveCall})");

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
                            yield break;
                        }
                    }

                    ServerLogger.LogError(webRequest.error);
                }
                else
                {
                    ProfileService.Instance.ConnectionInfos[conn].PlayingCharacter = Character.Deserialize(webRequest.downloadHandler.text);
                    ServerLogger.LogSuccess($"Received and deserialized {ProfileService.Instance.ConnectionInfos[conn].PlayingCharacter.name} for {conn}");
                }
            }
        }
#endif

        #endregion

        #region Character Creation

        [Client]
        public void SendCharacterCreationRequest(string characterName, string characterClass)
        {
            NotificationSystem.Push("Creating character ...", false);
            NetworkClient.Send(new CharacterCreationRequest
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
            ServerLogger.Log($"Received 'CharacterCreationRequest' from {conn}.");

            try
            {
                if (ProfileService.Instance.ConnectionInfos[conn].PlayingCharacter != null)
                {
                    conn.Send(new CharacterCreationResponse
                    {
                        Code = 403,
                        Message = "You cannot perform this operation while playing a character."
                    });
                    ServerLogger.LogWarning($"{conn} tried creating a character while playing.");
                }

                var profile = ProfileService.Instance.ConnectionInfos[conn];

                var failMessage = "";

                if (profile.characters.Length >= profile.maxCharacterCount)
                {
                    failMessage = "Reached Character Count Limit";
                    ServerLogger.LogWarning($"{conn} tried creating a character but has reached character limit.");
                }
                else if (msg.Name.Length < 3 || msg.Name.Length > 30)
                {
                    failMessage = "Invalid Character Name";
                    ServerLogger.LogWarning($"{conn} tried creating a character but character name was invalid.");
                }
                else if (!Character.Classes.Contains(msg.Class))
                {
                    failMessage = "Invalid Character Class";
                    ServerLogger.LogWarning($"{conn} tried creating a character but character class was invalid.");
                }
                else
                {
                    StartCoroutine(CreateCharacterCoroutine(conn, new CharacterData
                    {
                        name = msg.Name,
                        @class = msg.Class,
                        level = 0,
                        deathCount = 0,
                        isHardcore = false,
                        season = 0
                    }));
                    return;
                }

                ServerLogger.Log($"Creating character failed. Sending negative response to {conn}");
                conn.Send(new CharacterCreationResponse
                {
                    Code = ResponseCodeError,
                    Message = failMessage
                });
            }
            catch (Exception e)
            {
                ServerLogger.LogError(e.Message);
                throw;
            }
        }

        private IEnumerator CreateCharacterCoroutine(NetworkConnection conn, CharacterData data, bool recursiveCall = false)
        {
            ServerLogger.Log($"Started 'CreateCharacterCoroutine'. Args(conn: {conn}, data: {data}, recursiveCall: {recursiveCall})");

            using (var webRequest =
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

                    conn.Send(new CharacterCreationResponse
                    {
                        Code = ResponseCodeError,
                        Message = "Failed to create character."
                    });
                    ServerLogger.LogError(webRequest.error);
                    yield break;
                }

                ServerLogger.LogSuccess($"Created character. Sending confirmation response to {conn}");
                conn.Send(new CharacterCreationResponse
                {
                    Code = ResponseCodeOk,
                    Message = data.name
                });

                StartCoroutine(ProfileService.Instance.FetchProfileCoroutine(conn, ProfileService.Instance.ConnectionInfos[conn].steamId));
            }
        }
#endif
#if !UNITY_SERVER
        [Client]
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
#endif

        #endregion

        #region Character Deletion

        public void SendCharacterDeletionRequest(string characterId)
        {
            NetworkClient.Send(new CharacterDeletionRequest
            {
                CharacterId = characterId
            });
            NotificationSystem.Push("Deleting character ...", false);
        }

#if UNITY_SERVER || UNITY_EDITOR
        [Server]
        private void OnCharacterDeletionRequest(NetworkConnection conn, CharacterDeletionRequest msg)
        {
            ServerLogger.Log($"Received 'CharacterCreationRequest' from {conn}.");

            try
            {
                if (ProfileService.Instance.ConnectionInfos[conn].PlayingCharacter != null)
                {
                    conn.Send(new CharacterCreationResponse
                    {
                        Code = 403,
                        Message = "You cannot perform this operation while playing a character."
                    });
                    ServerLogger.LogWarning($"{conn} tried deleting a character while playing.");
                }

                var connectionInfo = ProfileService.Instance.ConnectionInfos[conn];

                if (connectionInfo.characters.Any(c => c.characterId == msg.CharacterId)) StartCoroutine(DeleteCharacterCoroutine(conn, msg.CharacterId));
            }
            catch (Exception e)
            {
                ServerLogger.LogError(e.Message);
                throw;
            }
        }

        private IEnumerator DeleteCharacterCoroutine(NetworkConnection conn, string characterId, bool recursiveCall = false)
        {
            ServerLogger.Log($"Started 'DeleteCharacterCoroutine'. Args(conn: {conn}, characterId: {characterId}, recursiveCall: {recursiveCall})");

            using (var webRequest = UnityWebRequest.Delete(
                EndpointRegister.GetServerDeleteCharacterUrl(characterId, ProfileService.Instance.ConnectionInfos[conn].steamId  ,ServerAuthenticationService.Instance.serverAuthToken)
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

                    conn.Send(new CharacterDeletionResponse
                    {
                        Code = ResponseCodeError,
                        Message = "Couldn't delete character."
                    });
                    ServerLogger.LogError(webRequest.error);
                    yield break;
                }

                ServerLogger.LogSuccess($"Deleted character. Sending confirmation response to {conn}");
                conn.Send(new CharacterCreationResponse
                {
                    Code = ResponseCodeOk,
                    Message = characterId
                });
                StartCoroutine(ProfileService.Instance.FetchProfileCoroutine(conn, ProfileService.Instance.ConnectionInfos[conn].steamId));
            }
        }
#endif

#if !UNITY_SERVER
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
#endif

        #endregion

        #region Character Playing

        [Client]
        public void SendCharacterPlayRequest(string characterId)
        {
            NetworkClient.Send(new CharacterPlayRequest
            {
                CharacterId = characterId
            });
            NotificationSystem.Push("Sent character play request for " + characterId, false);
        }

#if UNITY_SERVER || UNITY_EDITOR
        [Server]
        private void OnCharacterPlayRequest(NetworkConnection conn, CharacterPlayRequest msg)
        {
            ServerLogger.Log($"Received 'CharacterPlayRequest' from {conn}.");
            try
            {
                if (ProfileService.Instance.ConnectionInfos[conn].PlayingCharacter != null)
                {
                    conn.Send(new CharacterPlayResponse
                    {
                        Code = 403,
                        Message = "You cannot perform this operation while playing a character."
                    });
                    ServerLogger.LogWarning($"{conn} tried playing a character while playing.");
                }

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
                throw;
            }
        }

        private IEnumerator LoadCharacterCoroutine(NetworkConnection conn, string characterId)
        {
            ServerLogger.Log($"Started 'LoadCharacterCoroutine'. Args(conn: {conn}, characterId: {characterId})");

            yield return StartCoroutine(ServerFetchCharacterCoroutine(conn, characterId));

            try
            {
                if (ProfileService.Instance.ConnectionInfos[conn].PlayingCharacter == null)
                {
                    conn.Send(new CharacterPlayResponse
                    {
                        Code = 400,
                        Message = "Cannot find character."
                    });
                    ServerLogger.LogWarning($"{conn} tried playing a character but character couldn't be found.");
                    yield break;
                }

                var player = InstantiatePlayerCharacter(conn);
                var identity = player.GetComponent<NetworkIdentity>();
                FlexSceneManager.LoadConnectionScenes(conn, new SingleSceneData(townHubScene, new[] {identity}), null);

                ProfileService.Instance.ConnectionInfos[conn].playerIdentity = identity;

                
                conn.Send(new CharacterPlayResponse
                {
                    Code = 200,
                    Message = "Ok"
                });
                ServerLogger.LogSuccess($"{conn} is now playing character. Sending confirmation response to {conn}");
            }
            catch (Exception e)
            {
                ServerLogger.LogError(e.Message);
                throw;
            }
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
            NotificationSystem.Push($"Received CharacterPlayResponse message: {msg.Code} {msg.Message}", true);
            
        }

        #endregion

        #region Update Character

        public IEnumerator UpdateCharacterOnDatabaseCoroutine(Character character, bool recursiveCall = false)
        {
            ServerLogger.Log($"Started 'UpdateCharacterOnDatabaseCoroutine'. Args(character: {character}, recursiveCall: {recursiveCall})");

            using (var webRequest =
                new UnityWebRequest(
                    EndpointRegister.GetServerUpdateCharacterUrl(character.id, ServerAuthenticationService.Instance.serverAuthToken),
                    "PUT"))
            {
                webRequest.uploadHandler = new UploadHandlerRaw(character.data.Serialize());
                webRequest.downloadHandler = new DownloadHandlerBuffer();

                yield return webRequest.SendWebRequest();

                if (webRequest.isNetworkError)
                {
                    ServerLogger.LogError($"Failed to update character due to network error {character.id}. {webRequest.error}");
                    yield break;
                }

                if (webRequest.isHttpError)
                {
                    if (!recursiveCall)
                    {
                        yield return StartCoroutine(ServerAuthenticationService.Instance.FetchAuthTokenCoroutine());

                        if (ServerAuthenticationService.Instance.serverAuthToken != null)
                        {
                            StartCoroutine(UpdateCharacterOnDatabaseCoroutine(character, true));
                            yield break;
                        }
                    }

                    ServerLogger.LogError($"Failed to update character due to http error {character.id}. {webRequest.error}");
                    yield break;
                }

                ServerLogger.LogSuccess($"Updated character {character.id} on db.");
            }
        }

        #endregion
    }
}