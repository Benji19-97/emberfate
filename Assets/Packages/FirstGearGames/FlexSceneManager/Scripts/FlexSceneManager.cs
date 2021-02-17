using FirstGearGames.FlexSceneManager.Events;
using FirstGearGames.FlexSceneManager.LoadUnloadDatas;
using FirstGearGames.FlexSceneManager.Messages;
using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Runtime.Helpers;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FirstGearGames.FlexSceneManager
{
    public class FlexSceneManager : MonoBehaviour
    {
        #region Types.

        /// <summary>
        /// Data about a scene which is to be loaded. Generated when processing scene queue data.
        /// </summary>
        private class LoadableScene
        {
            public LoadableScene(string sceneName, LoadSceneMode loadMode)
            {
                SceneName = sceneName;
                LoadMode = loadMode;
            }

            public readonly string SceneName;
            public readonly LoadSceneMode LoadMode;
        }

        #endregion

        #region Public.

        /// <summary>
        /// Dispatched when a scene change queue has begun. This will only call if a scene has succesfully begun to load or unload. The queue may process any number of scene events. For example: if a scene is told to unload while a load is still in progress, then the unload will be placed in the queue.
        /// </summary>
        public static event Action OnSceneQueueStart;

        /// <summary>
        /// Dispatched when the scene queue is emptied.
        /// </summary>
        public static event Action OnSceneQueueEnd;

        /// <summary>
        /// Dispatched when a scene load starts.
        /// </summary>
        public static event Action<LoadSceneStartEventArgs> OnLoadSceneStart;

        /// <summary>
        /// Dispatched when completion percentage changes while loading a scene. Value is between 0f and 1f, while 1f is 100% done. Can be used for custom progress bars when loading scenes.
        /// </summary>
        public static event Action<LoadScenePercentEventArgs> OnLoadScenePercentChange;

        /// <summary>
        /// Dispatched when a scene load ends.
        /// </summary>
        public static event Action<LoadSceneEndEventArgs> OnLoadSceneEnd;

        /// <summary>
        /// Dispatched when a scene load starts.
        /// </summary>
        public static event Action<UnloadSceneStartEventArgs> OnUnloadSceneStart;

        /// <summary>
        /// Dispatched when a scene load ends.
        /// </summary>
        public static event Action<UnloadSceneEndEventArgs> OnUnloadSceneEnd;

        /// <summary>
        /// Dispatched before the server rebuilds observers when the clients presence changes for a scene.
        /// </summary>
        public static event Action<ClientPresenceChangeEventArgs> OnClientPresenceChangeStart;

        /// <summary>
        /// Dispatched after the server rebuilds observers when the clients presence changes for a scene.
        /// </summary>
        public static event Action<ClientPresenceChangeEventArgs> OnClientPresenceChangeEnd;

        #endregion

        #region Private.

        /// <summary>
        /// Singleton reference of this script.
        /// </summary>
        private static FlexSceneManager _instance;

        /// <summary>
        /// Scenes which are currently loaded as networked scenes. All players should have networked scenes loaded.
        /// </summary>
        private NetworkedScenesData _networkedScenes = new NetworkedScenesData();

        /// <summary>
        /// Scenes to load or unload, in order.
        /// </summary>
        private List<object> _queuedSceneOperations = new List<object>();

        /// <summary>
        /// Collection of FlexSceneCheckers that are currently enabled. This data only exist on server.
        /// </summary>
        private HashSet<FlexSceneChecker> _sceneCheckers = new HashSet<FlexSceneChecker>();

        /// <summary>
        /// 
        /// </summary>
        private Dictionary<Scene, HashSet<NetworkConnection>> _sceneConnections = new Dictionary<Scene, HashSet<NetworkConnection>>();

        /// <summary>
        /// Scenes which connections are registered as existing.
        /// </summary>
        public static Dictionary<Scene, HashSet<NetworkConnection>> SceneConnections
        {
            get { return _instance._sceneConnections; }
        }

        /// <summary>
        /// Scenes which must be manually unloaded, even when emptied.
        /// </summary>
        private HashSet<Scene> _manualUnloadScenes = new HashSet<Scene>();

        /// <summary>
        /// Scene containing moved objects when changing single scene. On client this will contain all objects moved until the server destroys them.
        /// Mirror only sends spawn messages once per-client, per server side scene load. If a scene load is performed only for specific connections
        /// then the server is not resetting their single scene, but rather the single scene for those connections only. Because of this, any objects
        /// which are to be moved will not receive a second respawn message, as they are never destroyed on server, only on client.
        /// While on server only this scene contains objects being moved temporarily, before being moved to the new scene.
        /// </summary>
        private Scene _movedObjectsScene;

        /// <summary>
        /// Becomes true when client receives the initial scene load message.
        /// </summary>
        private bool _receivedInitialLoad = false;

        /// <summary>
        /// Default value for auto create player.
        /// When FlexSceneManager starts it will set autoCreatePlayer to false,
        /// while storing the default value. When a client completes their first scene
        /// load the player will be spawned in if autoCreatePlayer was previously true.
        /// </summary>
        private bool _defaultAutoCreatePlayer;

        /// <summary>
        /// Becomes true when when a scene first successfully begins to load or unload. Value is reset to false when the scene queue is emptied.
        /// </summary>
        private bool _sceneQueueStartInvoked = false;

        /// <summary>
        /// True once NetworkManage singleton is found.
        /// </summary>
        private bool _networkManagerFound = false;

        #endregion

        #region Unity callbacks and initialization.

        /// <summary>
        /// Initializes this script for use. Should only be completed once.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void FirstInitialize()
        {
            GameObject go = new GameObject();
            go.name = "FlexSceneManager";
            go.AddComponent<FlexSceneManager>();
            DontDestroyOnLoad(go);
        }

        private void Awake()
        {
            if (_instance != null)
            {
                Debug.LogWarning("Duplicate FlexSceneManager found. You do not need to add FlexSceneManager to your scene; it will be loaded automatically.");
                Destroy(this);
                return;
            }
            else
            {
                _instance = this;
                DontDestroyOnLoad(this);
                SceneManager.sceneUnloaded += SceneManager_SceneUnloaded;
            }
        }

        private void OnEnable()
        {
            ChangeMessageSubscriptions(true);
        }

        private void OnDisable()
        {
            ChangeMessageSubscriptions(false);
        }

        private void Update()
        {
            FindNetworkManager();
        }

        /// <summary>
        /// Received when a scene is unloaded.
        /// </summary>
        /// <param name="arg0"></param>
        private void SceneManager_SceneUnloaded(Scene scene)
        {
            if (!NetworkServer.active)
                return;

            /* Remove any unloaded scenes from local variables. This shouldn't
             * be needed if the user properly utilizes this scene manager,
             * but just incase, we don't want a memory leak. */
            SceneConnections.Remove(scene);
            _manualUnloadScenes.Remove(scene);

            /* Networked scenes. */
            //Single.
            if (scene.name == _networkedScenes.Single)
            {
                Scene s = SceneManager.GetSceneByName(_networkedScenes.Single);
                //If single scene was unloaded.
                if (string.IsNullOrEmpty(s.name))
                    _networkedScenes.Single = string.Empty;
            }

            //Additive.
            if (_networkedScenes.Additive.Length > 0)
            {
                List<string> additives = new List<string>();
                for (int i = 0; i < _networkedScenes.Additive.Length; i++)
                {
                    if (!string.IsNullOrEmpty(_networkedScenes.Additive[i]))
                    {
                        Scene s = SceneManager.GetSceneByName(_networkedScenes.Additive[i]);
                        //If single scene was unloaded.
                        if (!string.IsNullOrEmpty(s.name))
                            additives.Add(_networkedScenes.Additive[i]);
                    }
                }

                //Set additive to reconstructed list.
                _networkedScenes.Additive = additives.ToArray();
            }
        }

        /// <summary>
        /// Changes message subscriptions.
        /// </summary>
        /// <param name="subscribe"></param>
        private void ChangeMessageSubscriptions(bool subscribe)
        {
            if (subscribe)
            {
                NetworkClient.ReplaceHandler<LoadScenesMessage>(OnLoadScenes, false);
                NetworkClient.ReplaceHandler<UnloadScenesMessage>(OnUnloadScenes, false);
                NetworkServer.ReplaceHandler<ClientScenesLoadedMessage>(OnClientScenesLoaded);
                NetworkServer.ReplaceHandler<ClientPlayerCreated>(OnClientPlayerCreated);
            }
            else
            {
                NetworkClient.UnregisterHandler<LoadScenesMessage>();
                NetworkClient.UnregisterHandler<UnloadScenesMessage>();
                NetworkServer.UnregisterHandler<ClientScenesLoadedMessage>();
                NetworkServer.UnregisterHandler<ClientPlayerCreated>();
            }
        }

        /// <summary>
        /// Finds the NetworkManager so that FlexSceneManager can configure autoCreate player.
        /// </summary>
        private void FindNetworkManager()
        {
            if (_networkManagerFound)
                return;

            if (NetworkManager.singleton != null)
            {
                NetworkManager nm = NetworkManager.singleton;
                _defaultAutoCreatePlayer = nm.autoCreatePlayer;
                nm.autoCreatePlayer = false;

                /* Make sure user isn't setting scenes in NetworkManager.
                * Cannot use Mirror offline/online scenes with FSM as FSM is
                * a replacement for Mirror scene management. */
                bool sceneCheckFailed = false;
                if (nm.offlineScene != null && nm.offlineScene.Length > 0)
                {
                    Debug.LogError(
                        "While using FlexSceneManager OfflineScene should be empty within your NetworkManager. Please make these changes before running your project.");
                    sceneCheckFailed = true;
                }

                if (nm.onlineScene != null && nm.onlineScene.Length > 0)
                {
                    Debug.LogError(
                        "While using FlexSceneManager OnlineScene should be empty within your NetworkManager. Please make these changes before running your project.");
                    sceneCheckFailed = true;
                }

#if UNITY_EDITOR
                //If user needs remove scenes from their network manager.
                if (sceneCheckFailed)
                    UnityEditor.EditorApplication.isPlaying = false;
#endif

                _networkManagerFound = true;
            }
        }

        #endregion

        #region Synchronizing late joiners.

        /// <summary>
        /// Called when a client connects to the server, after authentication.
        /// </summary>
        /// <param name="conn"></param>
        public static void OnServerConnect(NetworkConnection conn)
        {
            _instance.OnServerConnectInternal(conn);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="conn"></param>
        private void OnServerConnectInternal(NetworkConnection conn)
        {
            /* If connection is null and not client host then
             * message cannot be sent. 
             * If client host message still wont be sent but
             * it will be simulated locally. */
            bool connectionEmpty = (conn == null || conn.connectionId == 0);
            if (connectionEmpty && (!NetworkServer.active && !NetworkClient.active))
                return;

            /* If there are no networked scenes then we must still send
             * an empty initial scene load so the client knows they're
             * caught up on scenes when first connecting. */
            if (_networkedScenes.Single.Length == 0 && _networkedScenes.Additive.Length == 0)
            {
                LoadSceneQueueData emptySqd = new LoadSceneQueueData();
                //Send message to load the networked scenes.
                LoadScenesMessage emptyMsg = new LoadScenesMessage()
                {
                    SceneQueueData = emptySqd
                };

                if (!connectionEmpty)
                    conn.Send(emptyMsg);
                else
                    OnLoadScenes(null, emptyMsg);

                return;
            }

            SingleSceneData ssd = null;
            //If a single scene exist.
            if (!string.IsNullOrEmpty(_networkedScenes.Single))
                ssd = new SingleSceneData(_networkedScenes.Single);

            AdditiveScenesData asd = null;
            //If additive scenes exist.
            if (_networkedScenes.Additive.Length > 0)
                asd = new AdditiveScenesData(_networkedScenes.Additive);

            /* Client will only load what is unloaded. This is so
             * if they are on the scene with the networkmanager or other
             * ddols, the ddols wont be loaded multiple times. */
            LoadSceneQueueData sqd = new LoadSceneQueueData(SceneScopeTypes.Networked, null, ssd, asd, new LoadOptions(), _networkedScenes, false);

            //Send message to load the networked scenes.
            LoadScenesMessage msg = new LoadScenesMessage()
            {
                SceneQueueData = sqd
            };

            if (!connectionEmpty)
                conn.Send(msg);
            else
                OnLoadScenes(null, msg);
        }

        #endregion

        #region Player disconnect.

        /// <summary>
        /// Received when a player disconnects from the server.
        /// </summary>
        /// <param name="conn"></param>
        [Server]
        public static void OnServerDisconnect(NetworkConnection conn)
        {
            _instance.OnServerDisconnectInternal(conn);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="conn"></param>
        private void OnServerDisconnectInternal(NetworkConnection conn)
        {
            //Scenes to unload because there are no more observers.
            List<SceneReferenceData> unloadSceneReferenceDatas = new List<SceneReferenceData>();
            //Current active scene.
            Scene activeScene = SceneManager.GetActiveScene();

            RemoveFromAllScenes(conn, false);
            //Remove connection from all connection scenes.
            foreach (KeyValuePair<Scene, HashSet<NetworkConnection>> item in SceneConnections)
            {
                //No more connections, not manual unload, and not a networked scene.
                if (item.Value.Count == 0 && !_manualUnloadScenes.Contains(item.Key) && !IsNetworkedScene(item.Key.name, _networkedScenes))
                {
                    //If not the active seen then add to be unloaded.
                    if (item.Key != activeScene)
                    {
                        SceneReferenceData sd = new SceneReferenceData()
                        {
                            Handle = item.Key.handle,
                            Name = item.Key.name
                        };
                        unloadSceneReferenceDatas.Add(sd);
                    }
                }
            }

            //If at least one scene should be unloaded.
            if (unloadSceneReferenceDatas.Count > 0)
            {
                AdditiveScenesData asd = new AdditiveScenesData()
                {
                    SceneReferenceDatas = unloadSceneReferenceDatas.ToArray()
                };

                UnloadConnectionScenes(new NetworkConnection[] {null}, asd);
            }
        }

        #endregion

        #region Player creation.

        /// <summary>
        /// Called on the client to tell the server when the client's player has been created. Typically called after performing ClientScene.AddPlayer().
        /// </summary>
        public static void SendPlayerCreated()
        {
            _instance.SendPlayerCreatedInternal();
        }

        /// <summary>
        /// Called on the client to tell the server when the client's player has been created. Typically called after performing ClientScene.AddPlayer().
        /// </summary>
        private void SendPlayerCreatedInternal()
        {
            NetworkClient.Send(new ClientPlayerCreated());
        }

        /// <summary>
        /// Resets that initial load has been completed. Must be called when connecting to reset that initial scenes have been loaded, as well to register messages again.
        /// </summary>
        [Client]
        public static void ResetInitialLoad()
        {
            _instance._receivedInitialLoad = false;
            _instance.ChangeMessageSubscriptions(true);
        }

        #endregion

        #region Server received messages.

        /// <summary>
        /// Received on the server immediately after the client request their player to be spawned.
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="msg"></param>
        private void OnClientPlayerCreated(NetworkConnection conn, ClientPlayerCreated msg)
        {
            if (conn == null || conn.identity == null)
            {
                Debug.LogWarning("Connection or identity on connection is null.");
                return;
            }

            //Add to single scene.
            Scene s;
            if (!string.IsNullOrEmpty(_networkedScenes.Single))
            {
                s = SceneManager.GetSceneByName(_networkedScenes.Single);
                if (!string.IsNullOrEmpty(s.name))
                    AddToScene(s, conn);
            }

            if (_networkedScenes.Additive != null)
            {
                for (int i = 0; i < _networkedScenes.Additive.Length; i++)
                {
                    s = SceneManager.GetSceneByName(_networkedScenes.Additive[i]);
                    if (!string.IsNullOrEmpty(s.name))
                        AddToScene(s, conn);
                }
            }
        }

        /// <summary>
        /// Received on server when a client loads scenes.
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="msg"></param>
        private void OnClientScenesLoaded(NetworkConnection conn, ClientScenesLoadedMessage msg)
        {
            List<Scene> scenesLoaded = new List<Scene>();
            //Build scenes for events.
            foreach (SceneReferenceData item in msg.SceneDatas)
            {
                if (!string.IsNullOrEmpty(item.Name))
                {
                    Scene s;
                    //If handle exist then get scene by the handle.
                    if (item.Handle != 0)
                        s = GetSceneByHandle(item.Handle);
                    //Otherwise get it by the name.
                    else
                        s = SceneManager.GetSceneByName(item.Name);

                    if (!string.IsNullOrEmpty(s.name))
                        scenesLoaded.Add(s);
                }
            }

            //Add to scenes.
            for (int i = 0; i < scenesLoaded.Count; i++)
                AddToScene(scenesLoaded[i], conn);
        }

        #endregion

        #region Events.

        /// <summary>
        /// Checks if OnQueueStart should invoke, and if so invokes.
        /// </summary>
        private void TryInvokeOnQueueStart()
        {
            if (_sceneQueueStartInvoked)
                return;

            _sceneQueueStartInvoked = true;
            OnSceneQueueStart?.Invoke();
        }

        /// <summary>
        /// Checks if OnQueueEnd should invoke, and if so invokes.
        /// </summary>
        private void TryInvokeOnQueueEnd()
        {
            if (!_sceneQueueStartInvoked)
                return;

            _sceneQueueStartInvoked = false;
            OnSceneQueueEnd?.Invoke();
        }

        /// <summary>
        /// Invokes that a scene load has started. Only called when valid scenes will be loaded.
        /// </summary>
        /// <param name="sqd"></param>
        private void InvokeOnSceneLoadStart(LoadSceneQueueData sqd)
        {
            TryInvokeOnQueueStart();
            OnLoadSceneStart?.Invoke(new LoadSceneStartEventArgs(sqd));
        }

        /// <summary>
        /// Invokes that a scene load has ended. Only called after a valid scene has loaded.
        /// </summary>
        /// <param name="sqd"></param>
        private void InvokeOnSceneLoadEnd(LoadSceneQueueData sqd, List<string> requestedLoadScenes, List<Scene> loadedScenes)
        {
            //Make new list to not destroy original data.
            List<string> skippedScenes = requestedLoadScenes.ToList();
            //Remove loaded scenes from requested scenes.
            for (int i = 0; i < loadedScenes.Count; i++)
                skippedScenes.Remove(loadedScenes[i].name);

            LoadSceneEndEventArgs args = new LoadSceneEndEventArgs(sqd, loadedScenes.ToArray(), skippedScenes.ToArray());
            OnLoadSceneEnd?.Invoke(args);
        }

        /// <summary>
        /// Invokes that a scene unload has started. Only called when valid scenes will be unloaded.
        /// </summary>
        /// <param name="sqd"></param>
        private void InvokeOnSceneUnloadStart(UnloadSceneQueueData sqd)
        {
            TryInvokeOnQueueStart();
            OnUnloadSceneStart?.Invoke(new UnloadSceneStartEventArgs(sqd));
        }

        /// <summary>
        /// Invokes that a scene unload has ended. Only called after a valid scene has unloaded.
        /// </summary>
        /// <param name="sqd"></param>
        private void InvokeOnSceneUnloadEnd(UnloadSceneQueueData sqd)
        {
            OnUnloadSceneEnd?.Invoke(new UnloadSceneEndEventArgs(sqd));
        }

        /// <summary>
        /// Invokes when completion percentage changes while unloading or unloading a scene. Value is between 0f and 1f, while 1f is 100% done.
        /// </summary>
        /// <param name="value"></param>
        private void InvokeOnScenePercentChange(LoadSceneQueueData sqd, float value)
        {
            value = Mathf.Clamp(value, 0f, 1f);
            OnLoadScenePercentChange?.Invoke(new LoadScenePercentEventArgs(sqd, value));
        }

        #endregion

        #region Scene queue processing.

        /// <summary>
        /// Processes queued scene operations.
        /// </summary>
        /// <param name="asServer"></param>
        /// <returns></returns>
        private IEnumerator __ProcessSceneQueue()
        {
            /* Queue start won't invoke unless a scene load or unload actually occurs.
             * For example: if a scene is already loaded, and nothing needs to be loaded,
             * queue start will not invoke. */

            while (_queuedSceneOperations.Count > 0)
            {
                //If a load scene.
                if (_queuedSceneOperations[0] is LoadSceneQueueData)
                    yield return StartCoroutine(__LoadScenes());
                //If an unload scene.
                else if (_queuedSceneOperations[0] is UnloadSceneQueueData)
                    yield return StartCoroutine(__UnloadScenes());

                _queuedSceneOperations.RemoveAt(0);
            }

            /* AutoCreatePlayer.
             * If this is the first time a scene load is being called on client
             * then the client can auto create their player after all scene
             * loads have been processed. */
            if (NetworkClient.active && !_receivedInitialLoad)
            {
                _receivedInitialLoad = true;

                //If auto create was defaulted to true then create player.
                if (_defaultAutoCreatePlayer)
                {
                    ClientScene.AddPlayer(NetworkClient.connection);
                    SendPlayerCreatedInternal();
                }
            }

            TryInvokeOnQueueEnd();
        }

        #endregion

        #region LoadScenes

        /// <summary>
        /// Loads scenes on the server and for all clients. Future clients will automatically load these scenes.
        /// </summary>
        /// <param name="singleScene">Single scene to load. Use null to opt-out of single scene loading.</param>
        /// <param name="additiveScenes">Additive scenes to load. Use null to opt-out of additive scene loading.</param>
        [Server]
        public static void LoadNetworkedScenes(SingleSceneData singleScene, AdditiveScenesData additiveScenes)
        {
            _instance.LoadScenesInternal(SceneScopeTypes.Networked, null, singleScene, additiveScenes, new LoadOptions(), _instance._networkedScenes, true);
        }

        /// <summary>
        /// Loads scenes on server and tells connections to load them as well. Other connections will not load this scene.
        /// </summary>
        /// <param name="conn">Connections to load scenes for.</param>
        /// <param name="singleScene">Single scene to load. Use null to opt-out of single scene loading.</param>
        /// <param name="additiveScenes">Additive scenes to load. Use null to opt-out of additive scene loading.</param>
        /// <param name="loadOptions">Additional LoadOptions for this action.</param>
        [Server]
        public static void LoadConnectionScenes(NetworkConnection conn, SingleSceneData singleScene, AdditiveScenesData additiveScenes,
            LoadOptions loadOptions = null)
        {
            LoadConnectionScenes(new NetworkConnection[] {conn}, singleScene, additiveScenes, loadOptions);
        }

        /// <summary>
        /// Loads scenes on server and tells connections to load them as well. Other connections will not load this scene.
        /// </summary>
        /// <param name="conns">Connections to load scenes for.</param>
        /// <param name="singleScene">Single scene to load. Use null to opt-out of single scene loading.</param>
        /// <param name="additiveScenes">Additive scenes to load. Use null to opt-out of additive scene loading.</param>
        [Server]
        public static void LoadConnectionScenes(NetworkConnection[] conns, SingleSceneData singleScene, AdditiveScenesData additiveScenes,
            LoadOptions loadOptions = null)
        {
            if (loadOptions == null)
                loadOptions = new LoadOptions();

            _instance.LoadScenesInternal(SceneScopeTypes.Connections, conns, singleScene, additiveScenes, loadOptions, _instance._networkedScenes, true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="singleScene"></param>
        /// <param name="additiveScenes"></param>
        /// <param name="asServer"></param>
        private void LoadScenesInternal(SceneScopeTypes scope, NetworkConnection[] conns, SingleSceneData singleScene, AdditiveScenesData additiveScenes,
            LoadOptions loadOptions, NetworkedScenesData networkedScenes, bool asServer)
        {
            //Add to scene queue data.        
            _queuedSceneOperations.Add(new LoadSceneQueueData(scope, conns, singleScene, additiveScenes, loadOptions, networkedScenes, asServer));
            /* If only one entry then scene operations are not currently in progress.
             * Should there be more than one entry then scene operations are already 
             * occuring. The coroutine will automatically load in order. */

            if (_queuedSceneOperations.Count == 1)
                StartCoroutine(__ProcessSceneQueue());
        }

        /// <summary>
        /// Loads a connection scene queue data. This behaves just like a networked scene load except it sends only to the specified connections, and it always loads as an additive scene on server.
        /// </summary>
        /// <returns></returns>
        private IEnumerator __LoadScenes()
        {
            Debug.Log("Method started");
            LoadSceneQueueData sqd = _queuedSceneOperations[0] as LoadSceneQueueData;
            RemoveInvalidSceneQueueData(ref sqd);
            /* No single or additive scene data. They were
             * empty or removed due to being invalid. */
            if (sqd.SingleScene == null && sqd.AdditiveScenes == null)
                yield break;

            /* It's safe to assume that every entry in single scene or additive scenes
             * are valid so long as SingleScene or AdditiveScenes are not null. */
            //True if running as client, while network server is active.
            bool asClientServerActive = (!sqd.AsServer && NetworkServer.active);

            //Create moved objects scene. It will probably be used eventually. If not, no harm either way.
            if (string.IsNullOrEmpty(_movedObjectsScene.name))
                _movedObjectsScene = SceneManager.CreateScene("MovedObjectsHolder");
            //Scenes processed by a client during this method.
            HashSet<SceneReferenceData> clientProcessedScenes = new HashSet<SceneReferenceData>();
            //SceneDatas generated for single and additive scenes within this SceneQueueData which are already loaded, or have been.
            SceneReferenceData singleSceneReferenceData = new SceneReferenceData();
            List<SceneReferenceData> additiveSceneReferenceDatas = new List<SceneReferenceData>();
            //Single scene which is loaded, or is to be loaded. Will contain a valid scene if a single scene is specified.
            Scene singleScene = new Scene();
            //True if a connections load and is client only.
            bool connectionsAndClientOnly = (sqd.ScopeType == SceneScopeTypes.Connections && !NetworkServer.active);
            //True if a single scene is specified, whether it needs to be loaded or not.
            bool singleSceneSpecified = (sqd.SingleScene != null && !string.IsNullOrEmpty(sqd.SingleScene.SceneReferenceData.Name));

            /* Scene queue data scenes.
            * All scenes in the scene queue data whether they will be loaded or not. */
            List<string> requestedLoadScenes = new List<string>();
            if (sqd.SingleScene != null)
                requestedLoadScenes.Add(sqd.SingleScene.SceneReferenceData.Name);
            if (sqd.AdditiveScenes != null)
            {
                for (int i = 0; i < sqd.AdditiveScenes.SceneReferenceDatas.Length; i++)
                    requestedLoadScenes.Add(sqd.AdditiveScenes.SceneReferenceDatas[i].Name);
            }

            /* Add to client processed scenes. */
            if (!sqd.AsServer)
            {
                /* Add all scenes to client processed scenes, wether loaded or not.
                 * This is so client can tell the server they have those scenes ready
                 * afterwards, and server will update observers. */
                if (sqd.SingleScene != null)
                    clientProcessedScenes.Add(sqd.SingleScene.SceneReferenceData);

                if (sqd.AdditiveScenes != null)
                {
                    for (int i = 0; i < sqd.AdditiveScenes.SceneReferenceDatas.Length; i++)
                        clientProcessedScenes.Add(sqd.AdditiveScenes.SceneReferenceDatas[i]);
                }
            }

            /* Set networked scenes.
             * If server, and networked scope. */
            if (sqd.AsServer && sqd.ScopeType == SceneScopeTypes.Networked)
            {
                //If single scene specified then reset networked scenes.
                if (singleSceneSpecified)
                    _networkedScenes = new NetworkedScenesData();

                if (sqd.SingleScene != null)
                    _networkedScenes.Single = sqd.SingleScene.SceneReferenceData.Name;
                if (sqd.AdditiveScenes != null)
                {
                    List<string> newNetworkedScenes = _networkedScenes.Additive.ToList();
                    foreach (SceneReferenceData item in sqd.AdditiveScenes.SceneReferenceDatas)
                    {
                        /* Add to additive only if it doesn't already exist.
                         * This is because the same scene cannot be loaded
                         * twice as a networked scene, though it can if loading for a connection. */
                        if (!_networkedScenes.Additive.Contains(item.Name))
                            newNetworkedScenes.Add(item.Name);

                        _networkedScenes.Additive = newNetworkedScenes.ToArray();
                    }
                }

                //Update queue data.
                sqd.NetworkedScenes = _networkedScenes;
            }

            /* LoadableScenes and SceneReferenceDatas.
            /* Will contain scenes which may be loaded.
             * Scenes might not be added to loadableScenes
             * if for example loadOnlyUnloaded is true and
             * the scene is already loaded. */
            List<LoadableScene> loadableScenes = new List<LoadableScene>();
            bool loadSingleScene = false;
            //Do not run if running as client, and server is active. This would have already run as server.
            if (!asClientServerActive)
            {
                //Add single.
                if (sqd.SingleScene != null)
                {
                    loadSingleScene = CanLoadScene(sqd.SingleScene.SceneReferenceData, sqd.LoadOptions.LoadOnlyUnloaded, sqd.AsServer);
                    //If can load.
                    if (loadSingleScene)
                        loadableScenes.Add(new LoadableScene(sqd.SingleScene.SceneReferenceData.Name, LoadSceneMode.Single));
                    //If cannot load, see if it already exist, and if so add to server scene datas.
                    else
                        singleScene = TryAddToServerSceneDatas(sqd.AsServer, sqd.SingleScene.SceneReferenceData, ref singleSceneReferenceData);
                }

                //Add additives.
                if (sqd.AdditiveScenes != null)
                {
                    foreach (SceneReferenceData sceneData in sqd.AdditiveScenes.SceneReferenceDatas)
                    {
                        if (CanLoadScene(sceneData, sqd.LoadOptions.LoadOnlyUnloaded, sqd.AsServer))
                            loadableScenes.Add(new LoadableScene(sceneData.Name, LoadSceneMode.Additive));
                        else
                            TryAddToServerSceneDatas(sqd.AsServer, sceneData, ref additiveSceneReferenceDatas);
                    }
                }
            }

            /* Resetting SceneConnections. */
            if (sqd.AsServer)
            {
                //Networked.
                if (sqd.ScopeType == SceneScopeTypes.Networked)
                {
                    if (loadSingleScene)
                    {
                        /* Clear all scene connections.
                         * There is no need to refresh scene checkers because their
                         * objects will be destroyed during the unload process, which
                         * will result in a refresh. */
                        SceneConnections.Clear();
                    }
                }
                //Connections.
                else if (sqd.ScopeType == SceneScopeTypes.Connections)
                {
                    /* If only certain connections then remove connections
                    * from all scenes. They will be placed into new scenes
                    * once they confirm the scenes have loaded on their end. */
                    if (singleSceneSpecified)
                    {
                        for (int i = 0; i < sqd.Connections.Length; i++)
                            RemoveFromAllScenes(sqd.Connections, true);
                        /* Refresh scene checkers.
                         * All scene checkers must be refreshed. This ensures the clients
                         * lose visibility on the scenes they're going to be
                         * moved out of before the new scene loads in. */
                        foreach (FlexSceneChecker fsc in _sceneCheckers)
                            fsc.RebuildObservers();
                    }
                }
            }

            /* Move identities
             * to holder scene to preserve them. 
             * Required if a single scene is specified. Cannot rely on
             * loadSingleScene since it is only true if the single scene
             * must be loaded, which may be false if it's already loaded on
             * the server. */
            //Do not run if running as client, and server is active. This would have already run as server.
            if (singleSceneSpecified && !asClientServerActive)
            {
                foreach (NetworkIdentity ni in sqd.SingleScene.MovedNetworkIdentities)
                    SceneManager.MoveGameObjectToScene(ni.gameObject, _movedObjectsScene);

                /* Destroy non-moved player objects.
                 * Only runs on the server. */
                if (sqd.AsServer && sqd.LoadOptions.RemovePlayerObjects)
                {
                    //For every connection see which objects need to be removed.
                    foreach (NetworkConnection c in sqd.Connections)
                    {
                        if (c == null)
                            continue;
                        if (c.clientOwnedObjects == null)
                            continue;

                        List<NetworkIdentity> netIdsToDestroy = new List<NetworkIdentity>();
                        //Go through every owned object.
                        foreach (NetworkIdentity objId in c.clientOwnedObjects)
                        {
                            bool inMovedObjects = false;
                            for (int z = 0; z < sqd.SingleScene.MovedNetworkIdentities.Length; z++)
                            {
                                //If in moved objects.
                                if (objId == sqd.SingleScene.MovedNetworkIdentities[z])
                                {
                                    inMovedObjects = true;
                                    break;
                                }
                            }

                            //If not in moved objects then add to destroy.
                            if (!inMovedObjects && objId.GetComponent<FlexSceneChecker>() != null)
                                netIdsToDestroy.Add(objId);
                        }

                        //Destroy objects as required.
                        for (int i = 0; i < netIdsToDestroy.Count; i++)
                            NetworkServer.Destroy(netIdsToDestroy[i].gameObject);
                    }
                }
            }

            /* Scene unloading.
             * 
            /* Unload all scenes (except moved objects scene). */
            /* Make a list for scenes which will be unloaded rather
            * than unload during the iteration. This is to prevent a
            * collection has changed error. 
            *
            * unloadableScenes is created so that if either unloadableScenes
            * or loadableScenes has value, the OnLoadStart event knows to dispatch. */
            List<Scene> unloadableScenes = new List<Scene>();
            //If a single is specified then build scenes to unload.
            //Do not run if running as client, and server is active. This would have already run as server.
            if (singleSceneSpecified && !asClientServerActive)
            {
                //Unload all other scenes.
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    Scene s = SceneManager.GetSceneAt(i);
                    //True if scene is unused.
                    bool unusedScene;
                    //If client only unload regardless.
                    if (NetworkClient.active && !NetworkServer.active)
                    {
                        unusedScene = true;
                    }
                    //Unused checks only apply if loading for connections and is server.
                    else if (sqd.ScopeType == SceneScopeTypes.Connections && sqd.AsServer)
                    {
                        //If scene must be manually unloaded then it cannot be unloaded here.
                        if (_manualUnloadScenes.Contains(s))
                        {
                            unusedScene = false;
                        }
                        //Not in manual unload, check if empty.
                        else
                        {
                            //If found in scenes set unused if has no connections.
                            if (SceneConnections.TryGetValue(s, out HashSet<NetworkConnection> conns))
                                unusedScene = (conns.Count == 0);
                            //If not found then set unused.
                            else
                                unusedScene = true;
                        }
                    }
                    /* Networked will always be unused, since scenes will change for
                     * everyone resulting in old scenes being wiped from everyone. */
                    else if (sqd.ScopeType == SceneScopeTypes.Networked)
                    {
                        unusedScene = true;
                    }
                    //Unhandled scope type. This should never happen.
                    else
                    {
                        Debug.LogWarning("Unhandled scope type for unused check.");
                        unusedScene = true;
                    }

                    //True if the scene being checked to unload is in scene queue data.
                    bool inSceneQueueData = requestedLoadScenes.Contains(s.name);
                    /* canUnload becomes true when the scene is
                     * not in the scene queue data, and when it passes
                     * CanUnloadScene conditions. */
                    bool canUnload = (
                        unusedScene &&
                        s.name != _movedObjectsScene.name &&
                        !inSceneQueueData &&
                        CanUnloadScene(s.name, sqd.NetworkedScenes)
                    );
                    //If not scene being changed to and not the object holder scene.
                    if (canUnload)
                        unloadableScenes.Add(s);
                }
            }

            /* Start event. */
            if (unloadableScenes.Count > 0 || loadableScenes.Count > 0)
                InvokeOnSceneLoadStart(sqd);

            /* Unloading scenes. */
            for (int i = 0; i < unloadableScenes.Count; i++)
            {
                //Unload one at a time.
                AsyncOperation async = SceneManager.UnloadSceneAsync(unloadableScenes[i]);
                while (!async.isDone)
                    yield return null;
            }

            //Scenes which have been loaded.
            List<Scene> loadedScenes = new List<Scene>();
            /* Scene loading.
            /* Use additive to not thread lock server. */
            for (int i = 0; i < loadableScenes.Count; i++)
            {
                //Start load async and wait for it to finish.
                LoadSceneParameters loadSceneParameters = new LoadSceneParameters()
                {
                    loadSceneMode = LoadSceneMode.Additive,
                    localPhysicsMode = sqd.LoadOptions.LocalPhysics
                };

                AsyncOperation loadAsync = SceneManager.LoadSceneAsync(loadableScenes[i].SceneName, loadSceneParameters);
                while (!loadAsync.isDone)
                {
                    /* How much percentage each scene load can be worth
                     * at maximum completion. EG: if there are two scenes
                     * 1f / 2f is 0.5f. */
                    float maximumIndexWorth = (1f / (float) loadableScenes.Count);
                    /* Total percent will be how much percentage is complete
                     * in total. Initialize it with a value based on how many
                     * scenes are already fully loaded. */
                    float totalPercent = (i * maximumIndexWorth);
                    //Add this scenes progress onto total percent.
                    totalPercent += Mathf.Lerp(0f, maximumIndexWorth, loadAsync.progress);

                    //Dispatch with total percent.
                    InvokeOnScenePercentChange(sqd, totalPercent);

                    yield return null;
                }

                //After loaded, add to loaded scenes and datas.
                Scene lastLoadedScene = SceneManager.GetSceneAt(SceneManager.sceneCount - 1);
                SceneReferenceData sd = new SceneReferenceData()
                {
                    Handle = lastLoadedScene.handle,
                    Name = lastLoadedScene.name
                };
                //Add to loaded scenes.
                loadedScenes.Add(lastLoadedScene);

                /* Scene references */
                if (loadableScenes[i].LoadMode == LoadSceneMode.Single)
                {
                    singleSceneReferenceData = sd;
                    singleScene = lastLoadedScene;
                }
                else if (loadableScenes[i].LoadMode == LoadSceneMode.Additive)
                {
                    additiveSceneReferenceDatas.Add(sd);
                }
            }

            //When all scenes are loaded invoke with 100% done.
            InvokeOnScenePercentChange(sqd, 1f);

            /* Manual Unload Scenes. */
            if (sqd.AsServer && !sqd.LoadOptions.AutomaticallyUnload)
            {
                for (int i = 0; i < loadedScenes.Count; i++)
                    _manualUnloadScenes.Add(loadedScenes[i]);
            }

            /* Move identities to new single scene. */
            //Do not run if running as client, and server is active. This would have already run as server.
            if (singleSceneSpecified && !asClientServerActive)
            {
                for (int i = 0; i < sqd.SingleScene.MovedNetworkIdentities.Length; i++)
                {
                    /* The identities were already cleaned but this is just incase something happened
                     * to them while scenes were loading. */
                    foreach (NetworkIdentity ni in sqd.SingleScene.MovedNetworkIdentities)
                    {
                        if (ni != null && ni.netId != 0)
                            SceneManager.MoveGameObjectToScene(ni.gameObject, singleScene);
                    }
                }
            }

            /* Activate single scene. */
            if (singleSceneSpecified)
            {
                /* Set active scene.
                * If networked, since all clients will be changing.
                * Or if connectionsAndClientOnly. 
                * 
                * Cannot change active scene if client host because new objects will spawn
                * into the single scene intended for only certain connections; this will break observers. */
                if ((sqd.ScopeType == SceneScopeTypes.Networked && !asClientServerActive) || connectionsAndClientOnly)
                    SceneManager.SetActiveScene(singleScene);
            }

            /* Completion messages.
            * If running as server. */
            if (sqd.AsServer)
            {
                if (sqd.SingleScene != null)
                    sqd.SingleScene.SceneReferenceData = singleSceneReferenceData;
                if (sqd.AdditiveScenes != null)
                    sqd.AdditiveScenes.SceneReferenceDatas = additiveSceneReferenceDatas.ToArray();

                /* Make SceneQueueData serializable again.
                 * Data may have been altered when removing invalid entries. */
                sqd.MakeSerializable();
                //Tell clients to load same scenes.
                LoadScenesMessage msg = new LoadScenesMessage()
                {
                    SceneQueueData = sqd
                };

                //If networked scope then send to all.
                if (sqd.ScopeType == SceneScopeTypes.Networked)
                {
                    NetworkServer.SendToAll(msg);
                }
                //If connections scope then only send to connections.
                else if (sqd.ScopeType == SceneScopeTypes.Connections)
                {
                    for (int i = 0; i < sqd.Connections.Length; i++)
                    {
                        if (sqd.Connections[i] != null)
                        {
                            sqd.Connections[i].Send(msg);
                        }
                    }
                }
            }
            /* If running as client then send a message
             * to the server to tell them the scene was loaded.
             * This allows the server to add the client
             * to the scene for checkers. */
            else if (!sqd.AsServer)
            {
                ClientScenesLoadedMessage msg = new ClientScenesLoadedMessage()
                {
                    SceneDatas = clientProcessedScenes.ToArray()
                };
                NetworkClient.Send(msg);
            }

            Debug.Log("Method ran to end :)");

            InvokeOnSceneLoadEnd(sqd, requestedLoadScenes, loadedScenes);
        }

        /// <summary>
        /// Tries to find a scene and if found adds it to the specified setter.
        /// </summary>
        /// <returns>Scene added if found.</returns>
        private Scene TryAddToServerSceneDatas(bool asServer, SceneReferenceData dataToLookup, ref List<SceneReferenceData> setter)
        {
            Scene s = ReturnSceneFromReferenceData(asServer, dataToLookup);
            SceneReferenceData d = ReturnReferenceData(s);
            //If found.
            if (d != null)
            {
                setter.Add(d);
                return s;
            }

            /* Fall through, scene or ref data couldn't be found or made. */
            return new Scene();
        }

        /// <summary>
        /// Tries to find a scene and if found sets it to the specified reference.
        /// </summary>
        /// <returns>Scene added if found.</returns>
        private Scene TryAddToServerSceneDatas(bool asServer, SceneReferenceData dataToLookup, ref SceneReferenceData setter)
        {
            Scene s = ReturnSceneFromReferenceData(asServer, dataToLookup);
            SceneReferenceData d = ReturnReferenceData(s);
            //If found.
            if (d != null)
            {
                setter = d;
                return s;
            }

            /* Fall through, scene or ref data couldn't be found or made. */
            return new Scene();
        }

        /// <summary>
        /// Returns a scene using reference data.
        /// </summary>
        /// <param name="asServer"></param>
        /// <param name="referenceData"></param>
        /// <returns></returns>
        private Scene ReturnSceneFromReferenceData(bool asServer, SceneReferenceData referenceData)
        {
            //True if as client and server is also active.
            bool asClientServerActive = (!asServer && NetworkServer.active);

            Scene s;
            /* If handle is specified and server is running then find by
            * handle. Only the server can lookup by handle. 
            * Otherwise look up by name. */

            if (referenceData.Handle != 0 && (asServer || asClientServerActive))
                s = GetSceneByHandle(referenceData.Handle);
            else
                s = SceneManager.GetSceneByName(referenceData.Name);

            return s;
        }

        /// <summary>
        /// Returns a scene reference data for a scene.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private SceneReferenceData ReturnReferenceData(Scene s)
        {
            if (string.IsNullOrEmpty(s.name))
            {
                return null;
            }
            else
            {
                SceneReferenceData sd = new SceneReferenceData()
                {
                    Handle = s.handle,
                    Name = s.name
                };

                return sd;
            }
        }

        /// <summary>
        /// Received on client when connection scenes must be loaded.
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="msg"></param>
        [Client]
        private void OnLoadScenes(NetworkConnection conn, LoadScenesMessage msg)
        {
            Debug.Log("Got scene load for " + msg.SceneQueueData.SingleScene.SceneReferenceData.Name); //TODO-BB: Delete later
            LoadSceneQueueData sqd = msg.SceneQueueData;
            LoadScenesInternal(sqd.ScopeType, null, sqd.SingleScene, sqd.AdditiveScenes, new LoadOptions(), sqd.NetworkedScenes, false);
        }

        #endregion

        #region UnloadScenes.

        /// <summary>
        /// Unloads additive scenes on the server and for all clients.
        /// </summary>
        /// <param name="additiveScenes">Scenes to unload by string lookup.</param>
        [Server]
        public static void UnloadNetworkedScenes(string[] additiveScenes)
        {
            AdditiveScenesData asd = new AdditiveScenesData(additiveScenes);
            UnloadNetworkedScenes(asd);
        }

        /// <summary>
        /// Unloads additive scenes on the server and for all clients.
        /// </summary>
        /// <param name="additiveScenes">Scenes to unload by scene references.</param>
        [Server]
        public static void UnloadNetworkedScenes(AdditiveScenesData additiveScenes)
        {
            _instance.UnloadScenesInternal(SceneScopeTypes.Networked, null, additiveScenes, new UnloadOptions(), _instance._networkedScenes, true);
        }

        /// <summary>
        /// Unloads scenes on server and tells a connection to unload them as well. Other connections will not unload this scene.
        /// </summary>
        /// <param name="conn">Connections to unload scenes for.</param>
        /// <param name="additiveScenes">Scenes to unload by string lookup.</param>
        /// <param name="unloadOptions">Additional unload options for this action.</param>
        [Server]
        public static void UnloadConnectionScenes(NetworkConnection conn, string[] additiveScenes, UnloadOptions unloadOptions = null)
        {
            UnloadConnectionScenes(new NetworkConnection[] {conn}, additiveScenes, unloadOptions);
        }

        /// <summary>
        /// Unloads scenes on server and tells connections to unload them as well. Other connections will not unload this scene.
        /// </summary>
        /// <param name="conns">Connections to unload scenes for.</param>
        /// <param name="additiveScenes">Scenes to unload by string lookup.</param>
        [Server]
        public static void UnloadConnectionScenes(NetworkConnection[] conns, string[] additiveScenes, UnloadOptions unloadOptions = null)
        {
            AdditiveScenesData asd = new AdditiveScenesData(additiveScenes);
            UnloadConnectionScenes(conns, asd, unloadOptions);
        }

        /// <summary>
        /// Unloads scenes on server and tells connections to unload them as well. Other connections will not unload this scene.
        /// </summary>
        /// <param name="conns">Connections to unload scenes for.</param>
        /// <param name="additiveScenes">Scenes to unload by scene references.</param>
        [Server]
        public static void UnloadConnectionScenes(NetworkConnection conn, AdditiveScenesData additiveScenes, UnloadOptions unloadOptions = null)
        {
            UnloadConnectionScenes(new NetworkConnection[] {conn}, additiveScenes, unloadOptions);
        }

        /// <summary>
        /// Unloads scenes on server and tells connections to unload them as well. Other connections will not unload this scene.
        /// </summary>
        /// <param name="conns">Connections to unload scenes for.</param>
        /// <param name="additiveScenes">Scenes to unload by scene references.</param>
        [Server]
        public static void UnloadConnectionScenes(NetworkConnection[] conns, AdditiveScenesData additiveScenes, UnloadOptions unloadOptions = null)
        {
            if (unloadOptions == null)
                unloadOptions = new UnloadOptions();

            _instance.UnloadScenesInternal(SceneScopeTypes.Connections, conns, additiveScenes, unloadOptions, _instance._networkedScenes, true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="conns"></param>
        /// <param name="additiveScenes"></param>
        /// <param name="asServer"></param>
        private void UnloadScenesInternal(SceneScopeTypes scope, NetworkConnection[] conns, AdditiveScenesData additiveScenes, UnloadOptions unloadOptions,
            NetworkedScenesData networkedScenes, bool asServer)
        {
            _queuedSceneOperations.Add(new UnloadSceneQueueData(scope, conns, additiveScenes, unloadOptions, networkedScenes, asServer));
            /* If only one entry then scene operations are not currently in progress.
             * Should there be more than one entry then scene operations are already 
             * occuring. The coroutine will automatically load in order. */
            if (_queuedSceneOperations.Count == 1)
                StartCoroutine(__ProcessSceneQueue());
        }

        /// <summary>
        /// Loads scenes within QueuedSceneLoads.
        /// </summary>
        /// <returns></returns>
        private IEnumerator __UnloadScenes()
        {
            UnloadSceneQueueData sqd = _queuedSceneOperations[0] as UnloadSceneQueueData;

            /* Update visibilities. 
             *
             * This is to be done regardless of if a scene is unloaded or not.
             * A scene may not be unloaded because other clients could still be
             * in it, but visibility should still be removed for those
             * which are unloading. */
            if (sqd.AsServer)
                RemoveFromSceneConnections(sqd.Connections, sqd.AdditiveScenes);

            RemoveInvalidSceneQueueData(ref sqd);
            /* No additive scenes to unload. */
            if (sqd.AdditiveScenes == null)
                yield break;

            /* It's safe to assume that every entry in additive scenes
             * are valid so long as AdditiveScenes are not null. */
            //True if running as client, while network server is active.
            bool asClientServerActive = (!sqd.AsServer && NetworkServer.active);

            /* Remove from networked scenes.
            * If server and scope is networked. 
            * All passed in scenes should be removed from networked
            * regardless of if they're valid or not. If they are invalid,
            * then they shouldn't be in networked to begin with. */
            if (sqd.AsServer && sqd.ScopeType == SceneScopeTypes.Networked)
            {
                List<string> newNetworkedScenes = _networkedScenes.Additive.ToList();
                //Remove unloaded from networked scenes.
                foreach (SceneReferenceData item in sqd.AdditiveScenes.SceneReferenceDatas)
                    newNetworkedScenes.Remove(item.Name);
                _networkedScenes.Additive = newNetworkedScenes.ToArray();

                //Update queue data.
                sqd.NetworkedScenes = _networkedScenes;
            }

            /* Build unloadable scenes collection. */
            List<Scene> unloadableScenes = new List<Scene>();
            /* Do not run if running as client, and server is active. This would have already run as server.
             * This can still run as server, or client long as client is not also server. */
            if (!asClientServerActive)
            {
                foreach (SceneReferenceData item in sqd.AdditiveScenes.SceneReferenceDatas)
                {
                    Scene s;
                    /* If the handle exist and as server
                     * then unload using the handle. Otherwise
                     * unload using the name. Handles are used to
                     * unload scenes with the same name, which would
                     * only occur on the server since it can spawn multiple instances
                     * of the same scene. Client will always only have
                     * one copy of each scene so it must get the scene
                     * by name. */
                    if (item.Handle != 0 && sqd.AsServer)
                        s = GetSceneByHandle(item.Handle);
                    else
                        s = SceneManager.GetSceneByName(item.Name);

                    //True if scene is unused.
                    bool unusedScene;
                    //If client only, unload regardless.
                    if (NetworkClient.active && !NetworkServer.active)
                    {
                        unusedScene = true;
                    }
                    //Unused checks only apply if loading for connections and is server.
                    else if (sqd.ScopeType == SceneScopeTypes.Connections && sqd.AsServer)
                    {
                        //If force unload.
                        if (sqd.UnloadOptions.Mode == UnloadOptions.UnloadModes.ForceUnload)
                        {
                            unusedScene = true;
                        }
                        //If can unload unused.
                        else if (sqd.UnloadOptions.Mode == UnloadOptions.UnloadModes.UnloadUnused)
                        {
                            //If found in scenes set unused if has no connections.
                            if (SceneConnections.TryGetValue(s, out HashSet<NetworkConnection> conns))
                                unusedScene = (conns.Count == 0);
                            //If not found then set unused.
                            else
                                unusedScene = true;
                        }
                        //If cannot unload unused then set unused as false;
                        else
                        {
                            unusedScene = false;
                        }
                    }
                    /* Networked will always be unused, since scenes will change for
                     * everyone resulting in old scenes being wiped from everyone. */
                    else if (sqd.ScopeType == SceneScopeTypes.Networked)
                    {
                        unusedScene = true;
                    }
                    //Unhandled scope type. This should never happen.
                    else
                    {
                        Debug.LogWarning("Unhandled scope type for unused check.");
                        unusedScene = true;
                    }

                    /* canUnload becomes true when the scene is
                     * not in the scene queue data, and when it passes
                     * CanUnloadScene conditions. */
                    bool canUnload = (
                        unusedScene &&
                        s.name != _movedObjectsScene.name &&
                        CanUnloadScene(s, sqd.NetworkedScenes)
                    );

                    if (canUnload)
                        unloadableScenes.Add(s);
                }
            }

            //If there are scenes to unload.
            if (unloadableScenes.Count > 0)
            {
                /* If there are still scenes to unload after connections pass.
                 * There may not be scenes to unload as if another connection still
                 * exist in the unloadable scenes, then they cannot be unloaded. */
                if (unloadableScenes.Count > 0)
                {
                    InvokeOnSceneUnloadStart(sqd);

                    /* Remove each scene key from SceneConnections.
                     * There is no reason to update observers because
                     * the scene will be unloaded, which will remove
                     * the observer entirely. */
                    for (int i = 0; i < unloadableScenes.Count; i++)
                    {
                        SceneConnections.Remove(unloadableScenes[i]);
                        _manualUnloadScenes.Remove(unloadableScenes[i]);
                    }

                    /* Unload scenes.
                    /* Use additive to not thread lock server. */
                    foreach (Scene s in unloadableScenes)
                    {
                        AsyncOperation async = SceneManager.UnloadSceneAsync(s);
                        while (!async.isDone)
                            yield return null;
                    }
                }
            }

            /* If running as server. */
            if (sqd.AsServer)
            {
                /* Make SceneQueueData serializable again.
                 * Data may have been altered when removing invalid entries. */
                sqd.MakeSerializable();
                //Tell clients to unload same scenes.
                UnloadScenesMessage msg = new UnloadScenesMessage()
                {
                    SceneQueueData = sqd
                };
                //If connections scope.
                if (sqd.ScopeType == SceneScopeTypes.Networked)
                {
                    NetworkServer.SendToAll(msg);
                }
                //Networked scope.
                else if (sqd.ScopeType == SceneScopeTypes.Connections)
                {
                    if (sqd.Connections != null)
                    {
                        for (int i = 0; i < sqd.Connections.Length; i++)
                        {
                            if (sqd.Connections[i] != null)
                                sqd.Connections[i].Send(msg);
                        }
                    }
                }
            }

            InvokeOnSceneUnloadEnd(sqd);
        }

        /// <summary>
        /// Received on clients when networked scenes must be unloaded.
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="msg"></param>
        private void OnUnloadScenes(NetworkConnection conn, UnloadScenesMessage msg)
        {
            UnloadSceneQueueData sqd = msg.SceneQueueData;
            UnloadScenesInternal(sqd.ScopeType, sqd.Connections, sqd.AdditiveScenes, new UnloadOptions(), sqd.NetworkedScenes, false);
        }

        #endregion

        #region Add scene checkers.

        /// <summary>
        /// Adds a FlexSceneChecker to SceneCheckers.
        /// </summary>
        /// <param name="checker"></param>
        public static void AddSceneChecker(FlexSceneChecker checker)
        {
            _instance._sceneCheckers.Add(checker);
        }

        /// <summary>
        /// Sets a connection as being in a scene and updates FlexSceneCheckers.
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="conn"></param>
        [Server]
        public static void AddToScene(Scene scene, NetworkConnection conn)
        {
            _instance.AddToSceneInternal(scene, new NetworkConnection[] {conn});
        }

        /// <summary>
        /// Sets connections as being in a scene and updates FlexSceneCheckers.
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="conns"></param>
        [Server]
        public static void AddToScene(Scene scene, NetworkConnection[] conns)
        {
            _instance.AddToSceneInternal(scene, conns);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sceneName"></param>
        /// <param name="conns"></param>
        [Server]
        private void AddToSceneInternal(Scene scene, NetworkConnection[] conns)
        {
            if (string.IsNullOrEmpty(scene.name) || conns == null || conns.Length == 0)
                return;

            HashSet<NetworkConnection> hs;
            /* If the scene hasn't been added to the collection
             * yet then add it with an empty hashset. The hashet
             * will be populated below. */
            if (!SceneConnections.TryGetValue(scene, out hs))
            {
                hs = new HashSet<NetworkConnection>();
                SceneConnections[scene] = hs;
            }

            //Connections which have had their presence changed.
            List<NetworkConnection> changedPresences = new List<NetworkConnection>();
            bool rebuildObservers = false;
            //Go through each connection and add to hashset.
            for (int i = 0; i < conns.Length; i++)
            {
                if (conns[i] == null)
                {
                    Debug.LogWarning("NetworkConnection is null.");
                    continue;
                }

                //If not in the scene yet then add to changed presence.
                if (!hs.Contains(conns[i]))
                    changedPresences.Add(conns[i]);

                /* Check if this object has a scene checker, and if it does then
                * update the scene checker to include the scene it's being added
                * to. */
                if (conns[i].identity != null && conns[i].identity.GetComponent<FlexSceneChecker>() is FlexSceneChecker fsc)
                {
                    //Also set added if scene checker was updated, so that all scene checkers refresh.
                    rebuildObservers = true;
                    fsc.AddedToScene(scene);
                }

                bool r = hs.Add(conns[i]);
                if (r)
                    rebuildObservers = true;
            }

            //Dispatched start changed presences.
            InvokeClientPresenceChange(scene, changedPresences, true, true);
            /* If any connections were modified from scenes. */
            if (rebuildObservers)
            {
                foreach (FlexSceneChecker item in _sceneCheckers)
                    item.RebuildObservers();
            }

            //Dispatched end changed presences.
            InvokeClientPresenceChange(scene, changedPresences, true, false);
        }

        #endregion

        #region Remove scene checkers.

        /// <summary>
        /// Removes a FlexSceneChecker from SceneCheckers.
        /// </summary>
        /// <param name="checker"></param>
        public static void RemoveSceneChecker(FlexSceneChecker checker)
        {
            _instance._sceneCheckers.Remove(checker);
        }

        /// <summary>
        /// Unsets a connection as being in a scene and updates FlexSceneCheckers.
        /// </summary>
        /// <param name="sceneName"></param>
        /// <param name="conn"></param>
        [Server]
        public static void RemoveFromScene(Scene scene, NetworkConnection conn)
        {
            _instance.RemoveFromSceneInternal(scene, new NetworkConnection[] {conn});
        }

        /// <summary>
        /// Unsets connections as being in a scene and updates FlexSceneCheckers.
        /// </summary>
        /// <param name="sceneName"></param>
        /// <param name="conns"></param>
        [Server]
        public static void RemoveFromScene(Scene scene, NetworkConnection[] conns)
        {
            _instance.RemoveFromSceneInternal(scene, conns);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sceneName"></param>
        /// <param name="conns"></param>
        [Server]
        private void RemoveFromSceneInternal(Scene scene, NetworkConnection[] conns)
        {
            if (string.IsNullOrEmpty(scene.name) || conns == null || conns.Length == 0)
                return;

            HashSet<NetworkConnection> hs;
            /* If sceneName is not in the collection then nothing
             * can be removed as the hashset does not exist. */
            if (!SceneConnections.TryGetValue(scene, out hs))
                return;

            //Connections which have had their presence changed.
            List<NetworkConnection> changedPresences = new List<NetworkConnection>();
            bool rebuildObservers = false;
            //Go through each connection and remove from hashset.
            for (int i = 0; i < conns.Length; i++)
            {
                if (conns[i] == null)
                    continue;
                //Connection isn't in the scene.
                if (!hs.Contains(conns[i]))
                    continue;


                /* Check if this object has a scene checker, and if it does then
                 * update the scene checker to remove the scene it's being removed
                 * from. */
                if (conns[i].identity != null && conns[i].identity.GetComponent<FlexSceneChecker>() is FlexSceneChecker removedFsc)
                {
                    //Also set removed so that scene checkers update.
                    rebuildObservers = true;
                    removedFsc.RemovedFromScene(scene);
                }

                //Becomes true if the connection still has an object in the scene.
                bool stillInScene = false;
                /* See if player has any other objects in the scene. 
                 * Destroyed networkIdentity objects are removed from the
                 * client owned objects hashset before this runs. */
                foreach (NetworkIdentity ni in conns[i].clientOwnedObjects)
                {
                    if (ni != null && ni.GetComponent<FlexSceneChecker>() is FlexSceneChecker ownedFsc)
                    {
                        //If player has a FSC still in scene.
                        if (ownedFsc.CurrentScenes.Contains(scene))
                        {
                            stillInScene = true;
                            break;
                        }
                    }
                }

                /* Only try to remove the connection from the SceneConnections
                 * hashset if they are no longer in the scene. */
                if (!stillInScene)
                {
                    bool r = hs.Remove(conns[i]);
                    if (r)
                        rebuildObservers = true;

                    changedPresences.Add(conns[i]);
                }
            }

            //Dispatched start changed presences.
            InvokeClientPresenceChange(scene, changedPresences, false, true);
            /* If any connections were modified from scenes. */
            if (rebuildObservers)
            {
                foreach (FlexSceneChecker item in _sceneCheckers)
                    item.RebuildObservers();
            }

            //Dispatched end changed presences.
            InvokeClientPresenceChange(scene, changedPresences, false, false);
        }

        #endregion

        #region Remove Invalid Scenes.

        /// <summary>
        /// Removes invalid scene entries from a SceneQueueData.
        /// </summary>
        /// <param name="sceneDatas"></param>
        private void RemoveInvalidSceneQueueData(ref LoadSceneQueueData sqd)
        {
            //Check single scene.
            //If scene name is invalid.
            if (string.IsNullOrEmpty(sqd.SingleScene.SceneReferenceData.Name) ||
                //Loading for connection but already a single networked scene.
                (sqd.ScopeType == SceneScopeTypes.Connections && IsNetworkedScene(sqd.SingleScene.SceneReferenceData.Name, _networkedScenes))
            )
                sqd.SingleScene = null;

            //Check additive scenes.
            if (sqd.AdditiveScenes != null)
            {
                //Make all scene names into a list for easy removal.
                List<SceneReferenceData> listSceneReferenceDatas = sqd.AdditiveScenes.SceneReferenceDatas.ToList();
                for (int i = 0; i < listSceneReferenceDatas.Count; i++)
                {
                    //Scene name is null or empty.
                    if (string.IsNullOrEmpty(listSceneReferenceDatas[i].Name))
                    {
                        listSceneReferenceDatas.RemoveAt(i);
                        i--;
                    }
                }

                //Set back to array.
                sqd.AdditiveScenes.SceneReferenceDatas = listSceneReferenceDatas.ToArray();

                //If additive scene names is null or has no length then nullify additive scenes.
                if (sqd.AdditiveScenes.SceneReferenceDatas == null || sqd.AdditiveScenes.SceneReferenceDatas.Length == 0)
                    sqd.AdditiveScenes = null;
            }

            //Connections.
            if (sqd.Connections.Length > 0)
            {
                List<NetworkConnection> listConnections = sqd.Connections.ToList();
                for (int i = 0; i < listConnections.Count; i++)
                {
                    if (listConnections[i] == null || listConnections[i].connectionId == 0)
                    {
                        listConnections.RemoveAt(i);
                        i--;
                    }
                }
            }
        }

        /// <summary>
        /// Removes invalid scene entries from a SceneQueueData.
        /// </summary>
        /// <param name="sceneDatas"></param>
        private void RemoveInvalidSceneQueueData(ref UnloadSceneQueueData sqd)
        {
            Scene activeScene = SceneManager.GetActiveScene();
            //Check additive scenes.
            if (sqd.AdditiveScenes != null)
            {
                NetworkedScenesData networkedScenes = (NetworkServer.active) ? _networkedScenes : sqd.NetworkedScenes;
                //Make all scene names into a list for easy removal.
                List<SceneReferenceData> listSceneNames = sqd.AdditiveScenes.SceneReferenceDatas.ToList();
                for (int i = 0; i < listSceneNames.Count; i++)
                {
                    //If scene name is null or zero length/
                    if (string.IsNullOrEmpty(listSceneNames[i].Name) ||
                        //Or if scene name is active scene.
                        (activeScene != null && listSceneNames[i].Name == activeScene.name) ||
                        //If unloading as connection but scene is networked.
                        (sqd.ScopeType == SceneScopeTypes.Connections && IsNetworkedScene(listSceneNames[i].Name, networkedScenes))
                    )
                    {
                        listSceneNames.RemoveAt(i);
                        i--;
                    }
                }

                //Set back to array.
                sqd.AdditiveScenes.SceneReferenceDatas = listSceneNames.ToArray();

                //If additive scene names is null or has no length then nullify additive scenes.
                if (sqd.AdditiveScenes.SceneReferenceDatas == null || sqd.AdditiveScenes.SceneReferenceDatas.Length == 0)
                    sqd.AdditiveScenes = null;
            }
        }

        #endregion

        #region Can Load/Unload Scene.

        /// <summary>
        /// Returns if a scene name can be loaded.
        /// </summary>
        /// <param name="sceneName"></param>
        /// <param name="loadOnlyUnloaded"></param>
        /// <returns></returns>
        private bool CanLoadScene(SceneReferenceData sceneReferenceData, bool loadOnlyUnloaded, bool asServer)
        {
            /* When a handle is specified a scene can only be loaded if the handle does not exist.
             * This is regardless of loadOnlyUnloaded value. This is also only true for the server, as
             * only the server actually utilizies/manages handles. This feature exist so users may stack scenes
             * by setting loadOnlyUnloaded false, while also passing in a scene reference which to add a connection
             * to.
             * 
             * For example: if scene stacking is enabled(so, !loadOnlyUnloaded), and a player is the first to join Blue scene. Let's assume
             * the handle for that spawned scene becomes -10. Later, the server wants to add another player to the same
             * scene. They would generate the load scene data, passing in the handle of -10 for the scene to load. The
             * loader will then check if a scene is loaded by that handle, and if so add the player to that scene rather than
             * load an entirely new scene. If a scene does not exist then a new scene will be made. */
            if (asServer && sceneReferenceData.Handle != 0)
            {
                if (!string.IsNullOrEmpty(GetSceneByHandle(sceneReferenceData.Handle).name))
                    return false;
            }

            return CanLoadScene(sceneReferenceData.Name, loadOnlyUnloaded);
        }

        /// <summary>
        /// Returns if a scene name can be loaded.
        /// </summary>
        /// <param name="sceneName"></param>
        /// <param name="loadOnlyUnloaded"></param>
        /// <returns></returns>
        private bool CanLoadScene(string sceneName, bool loadOnlyUnloaded)
        {
            if (string.IsNullOrEmpty(sceneName))
                return false;

            if (!loadOnlyUnloaded || (loadOnlyUnloaded && !IsSceneLoaded(sceneName)))
                return true;
            else
                return false;
        }

        /// <summary>
        /// Returns if a scene can be unloaded.
        /// </summary>
        /// <param name="sceneName"></param>
        /// <param name="scopeType"></param>
        /// <returns></returns>
        private bool CanUnloadScene(string sceneName, NetworkedScenesData networkedScenes)
        {
            //Not loaded.
            if (!IsSceneLoaded(sceneName))
                return false;

            /* Cannot unload networked scenes.
             * If a scene should be unloaded, that is networked,
             * then it must be removed from the networked scenes
             * collection first. */
            if (IsNetworkedScene(sceneName, networkedScenes))
                return false;

            return true;
        }

        /// <summary>
        /// Returns if a scene can be unloaded.
        /// </summary>
        /// <param name="sceneName"></param>
        /// <param name="scopeType"></param>
        /// <returns></returns>
        private bool CanUnloadScene(Scene scene, NetworkedScenesData networkedScenes)
        {
            return CanUnloadScene(scene.name, networkedScenes);
        }

        #endregion

        #region Remove From Scene Connections

        [Server]
        private void RemoveFromAllScenes(NetworkConnection conn, bool removeEmptySceneConnections)
        {
            RemoveFromAllScenes(new NetworkConnection[] {conn}, removeEmptySceneConnections);
        }

        /// <summary>
        /// Removes a connection from all scenes.
        /// </summary>
        /// <param name="sceneName"></param>
        /// <param name="conns"></param>
        [Server]
        private void RemoveFromAllScenes(NetworkConnection[] conns, bool removeEmptySceneConnections)
        {
            if (conns == null)
                return;

            //Iterate every scene.
            foreach (KeyValuePair<Scene, HashSet<NetworkConnection>> item in SceneConnections)
                RemoveFromScene(item.Key, conns);

            if (removeEmptySceneConnections)
                RemoveEmptySceneConnections();
        }

        /// <summary>
        /// Removes connections from specified scenes.
        /// </summary>
        /// <param name="conns"></param>
        /// <param name="asd"></param>
        [Server]
        private void RemoveFromSceneConnections(NetworkConnection[] conns, AdditiveScenesData asd)
        {
            //Build a collection of scenes which visibility is being removed from.
            List<Scene> scenesToRemoveFrom = new List<Scene>();
            //Build scenes which connection is in using additive scenes data.
            for (int i = 0; i < asd.SceneReferenceDatas.Length; i++)
            {
                Scene s;
                if (asd.SceneReferenceDatas[i].Handle != 0)
                    s = GetSceneByHandle(asd.SceneReferenceDatas[i].Handle);
                else
                    s = SceneManager.GetSceneByName(asd.SceneReferenceDatas[i].Name);

                if (!string.IsNullOrEmpty(s.name))
                    scenesToRemoveFrom.Add(s);
            }

            RemoveFromSceneConnections(conns, scenesToRemoveFrom.ToArray());
        }

        /// <summary>
        /// Removes connections from specified scenes.
        /// </summary>
        /// <param name="conns"></param>
        /// <param name="asd"></param>
        [Server]
        private void RemoveFromSceneConnections(NetworkConnection[] conns, Scene[] scenes)
        {
            //Remove connections from every scene to unload.
            for (int i = 0; i < scenes.Length; i++)
                RemoveFromScene(scenes[i], conns);

            RemoveEmptySceneConnections();
        }

        #endregion

        #region Helpers.

        /// <summary>
        /// Invokes OnClientPresenceChange start or end.
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="conns"></param>
        /// <param name="added"></param>
        /// <param name="start"></param>
        private void InvokeClientPresenceChange(Scene scene, List<NetworkConnection> conns, bool added, bool start)
        {
            for (int i = 0; i < conns.Count; i++)
            {
                ClientPresenceChangeEventArgs cpc = new ClientPresenceChangeEventArgs(scene, conns[i], added);
                if (start)
                    OnClientPresenceChangeStart?.Invoke(cpc);
                else
                    OnClientPresenceChangeEnd?.Invoke(cpc);
            }
        }

        /// <summary>
        /// Removes keys from SceneConnections which contain no value.
        /// </summary>
        private void RemoveEmptySceneConnections()
        {
            List<Scene> keysToRemove = new List<Scene>();
            foreach (KeyValuePair<Scene, HashSet<NetworkConnection>> item in SceneConnections)
            {
                if (item.Value.Count == 0)
                    keysToRemove.Add(item.Key);
            }

            for (int i = 0; i < keysToRemove.Count; i++)
                SceneConnections.Remove(keysToRemove[i]);
        }

        /// <summary>
        /// Returns if a sceneName is a networked scene.
        /// </summary>
        /// <param name="sceneName"></param>
        /// <returns></returns>
        private bool IsNetworkedScene(string sceneName, NetworkedScenesData networkedScenes)
        {
            if (string.IsNullOrEmpty(sceneName))
                return false;

            //Matches single sene.
            if (networkedScenes.Single != null && sceneName == networkedScenes.Single)
                return true;

            //Matches at least one additive.
            if (networkedScenes.Additive != null)
            {
                if (networkedScenes.Additive.Contains(sceneName))
                    return true;
            }

            //Fall through, no matches.
            return false;
        }

        /// <summary>
        /// Returns if a scene is loaded.
        /// </summary>
        /// <param name="sceneName"></param>
        /// <returns></returns>
        private bool IsSceneLoaded(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
                return false;

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if (SceneManager.GetSceneAt(i).name == sceneName)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Returns a scene by handle.
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        public static Scene GetSceneByHandle(int handle)
        {
            return _instance.GetSceneByHandleInternal(handle);
        }

        /// <summary>
        /// Returns a scene by handle.
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        private Scene GetSceneByHandleInternal(int handle)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene s = SceneManager.GetSceneAt(i);
                if (s.handle == handle)
                    return s;
            }

            //Fall through, not found.
            return new Scene();
        }

        #endregion

        #region Unused.

        ///// <summary>
        ///// Returns if it's possible to attempt a MoveToScene.
        ///// </summary>
        ///// <param name="scene"></param>
        ///// <param name="netIdentities"></param>
        ///// <returns></returns>
        //private bool ValidateMoveNetworkIdentity(string newScene, NetworkIdentity[] netIdentities)
        //{
        //    //Network Identity null or unset.
        //    if (netIdentities == null || netIdentities.Length == 0)
        //    {
        //        Debug.LogError("NetworkIdentities is either null or of zero length.");
        //        return false;
        //    }

        //    //Make sure scene is loaded.
        //    Scene scene = GetSceneByName(newScene);
        //    if (scene.path == null)
        //        return false;

        //    //First make sure the scene is loaded.
        //    bool newSceneLoaded = false;
        //    for (int i = 0; i < SceneManager.sceneCount; i++)
        //    {
        //        //Found scene.
        //        if (SceneManager.GetSceneAt(i).name == newScene)
        //        {
        //            newSceneLoaded = true;
        //            break;
        //        }
        //    }
        //    /* If newScene isn't loaded then the networkidentity
        //    * cannot be moved. */
        //    if (!newSceneLoaded)
        //    {
        //        Debug.LogError("Scene " + newScene + " is not loaded.");
        //        return false;
        //    }

        //    //Fall through. If here all checks passed.
        //    return true;
        //}

        ///// <summary>
        ///// Moves a NetworkIdentity to a new scene on server and clients.
        ///// </summary>
        ///// <param name="newScene"></param>
        ///// <param name="netIdentities"></param>
        ///// <param name="reloadScene"></param>
        //[Server]
        //public void MoveNetworkIdentities(string newScene, NetworkIdentity[] netIdentities, bool broadcastToClients)
        //{
        //    if (!ValidateMoveNetworkIdentity(newScene, netIdentities))
        //        return;

        //    /* Remove the identity from all scenes first.
        //     * A brute force check on scenes is likely faster
        //     * than storing which scene every identity is in. */
        //    foreach (KeyValuePair<string, HashSet<NetworkIdentity>> sceneIds in SceneIdentities)
        //        RemoveFromScene(sceneIds.Key, netIdentities);

        //    //After removed from all scenes add to new scene.
        //    AddToScene(newScene, netIdentities);

        //    Scene scene = GetSceneByName(newScene);
        //    //Move objects to new scene.
        //    for (int i = 0; i < netIdentities.Length; i++)
        //    {
        //        if (netIdentities[i] == null || netIdentities[i].netId == 0)
        //        {
        //            Debug.LogWarning("NetworkIdentity is null or unset.");
        //            continue;
        //        }

        //        SceneManager.MoveGameObjectToScene(netIdentities[i].gameObject, scene);
        //    }

        //}


        ///// <summary>
        ///// Received on clients when they should load a scene and move to it.
        ///// </summary>
        ///// <param name="conn"></param>
        ///// <param name="msg"></param>
        //[ClientCallback]
        //private void OnMoveNetworkIdentity(NetworkConnection conn, MoveNetworkIdentityMessage msg)
        //{
        //    if (!ValidateMoveNetworkIdentity(msg.SceneName, msg.NetworkIdentities))
        //        return;

        //    Scene scene = GetSceneByName(msg.SceneName);
        //    NetworkIdentity[] netIdentities = msg.NetworkIdentities;
        //    //Move objects to new scene.
        //    for (int i = 0; i < netIdentities.Length; i++)
        //    {
        //        if (netIdentities[i] == null || netIdentities[i].netId == 0)
        //        {
        //            Debug.LogWarning("NetworkIdentity is null or unset.");
        //            continue;
        //        }

        //        SceneManager.MoveGameObjectToScene(netIdentities[i].gameObject, scene);
        //    }
        //}

        #endregion
    }
}