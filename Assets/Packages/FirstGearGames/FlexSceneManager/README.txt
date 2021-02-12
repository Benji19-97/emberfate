Video Tutorials: https://www.youtube.com/playlist?list=PLkx8oFug638pElzrgDFgl-ad73Bu6rusZ

Setup
=====================================
    Import all files within FirstGearGames/FlexSceneManager. You may exclude the demos folder.

    Create a custom NetworkManager using script templates if you do not already have one. Replace existing network manager script reference.
    Within your custom network manager:
        Under override OnServerConnect add the following to the end of the method:
            FlexSceneManager.OnServerConnect(conn);
        Under override OnServerDisconnect add the following to the top of the method:
            FlexSceneManager.OnServerDisconnect(conn);
        Under override OnStartClient add the following to the end of the method:
            FlexSceneManager.ResetInitialLoad();

    If you are not using autoCreatePlayer within your network manager then after spawning your player manually, you must also call
        FlexSceneManager.SendPlayerCreated();



Demos
=====================================
    Open the "Main" scene within each demo folder. Blue spheres load and unload scenes for connections. Red spheres load and unload scenes for network (all clients).
    See demo scripts for examples of loading and unloading.



FlexSceneChecker
=====================================
    Add to objects which you only want visible when players have the scene loaded. In most cases this will be on all of your networked objects.
    Enable Add To Current Scene if you want the object to be automatically registered to the scene it spawns in. This is typically enabled for scene objects, and sometimes player objects.



Loading and Unloading Data Types
=====================================
    SceneReferenceData
    	Handle: handle for the scene. This is controlled by the server and has no use for clients.
    	Name: name of the scene being loaded.

    SingleSceneData: only used when loading scenes.
    	SceneReferenceData: scene to load or unload.
    	MovedNetworkIdentities: network identities to move to the new scene single scene.

    AdditiveScenesData
    	SceneReferenceDatas: scenes to load or unload.

    LoadOptions
        AutomaticallyUnload: True if to automatically unload the loaded scenes on the server when they are no longer being used.
        LoadOnlyUnloaded: True if to only load scenes which are not yet loaded. When false a scene may load multiple times. This is only used by the server.
        LocalPhysics: Physics mode to use when loading this scene. Only used by the server.
        Params: Parameters which can be passed into a scene load. Params can be useful to link personalized data with scene load callbacks, such as a match Id.

    UnloadOptions
        UnloadModes Mode: How to unload scenes on the server. UnloadUnused will unload scenes which have no more clients in them. KeepUnused will not unload a scene even when empty. ForceUnload will unload a scene regardless of if clients are still connected to it.
        object[] Params: Parameters which can be passed into a scene load. Params can be useful to link personalized data with scene load callbacks, such as a match Id.



Loading and Unloading API: FlexSceneManager.Method()
=====================================    
    public static void LoadNetworkedScenes(SingleSceneData singleScene, AdditiveScenesData additiveScenes)
        Loads scenes on the server and for all clients. Future clients will automatically load these scenes.

    public static void LoadConnectionScenes(NetworkConnection conn, SingleSceneData singleScene, AdditiveScenesData additiveScenes, LoadOptions loadOptions = null)
    public static void LoadConnectionScenes(NetworkConnection[] conns, SingleSceneData singleScene, AdditiveScenesData additiveScenes, LoadOptions loadOptions = null)
        Loads scenes on the server and tells connections to load them as well. Other connections will not load this scene.

    public static void UnloadNetworkedScenes(string[] additiveScenes)
    public static void UnloadNetworkedScenes(AdditiveScenesData additiveScenes)
        Unloads additive scenes on the server and for all clients.

    public static void UnloadConnectionScenes(NetworkConnection conn, string[] additiveScenes, UnloadOptions unloadOptions = null)
    public static void UnloadConnectionScenes(NetworkConnection[] conns, string[] additiveScenes, UnloadOptions unloadOptions = null)
    public static void UnloadConnectionScenes(NetworkConnection conn, AdditiveScenesData additiveScenes, UnloadOptions unloadOptions = null)
    public static void UnloadConnectionScenes(NetworkConnection[] conns, AdditiveScenesData additiveScenes, UnloadOptions unloadOptions = null)
        Unloads scenes on server and tells connections to unload them as well. Other connections will not unload this scene.



Special Use API: FlexSceneManager.Method()
=====================================
    public static void SendPlayerCreated()
        If your player object utilizies FlexSceneChecker and you are not using autoCreatePlayer in your network manager then you must call
        after using ClientScene.AddPlayer().



Events: FlexSceneManager
=====================================
    public static event Action OnSceneQueueStart;
        Dispatched when a scene change queue has begun. This will only call if a scene has succesfully begun to load or unload. The queue may process any number of scene events. For example: if a scene is told to unload while a load is still in progress, then the unload will be placed in the queue.
    
    public static event Action OnSceneQueueEnd;
        Dispatched when the scene queue is emptied.

    public static event Action<LoadSceneStartEventArgs> OnLoadSceneStart;
        Dispatched when a scene load starts.
            LoadSceneQueueData RawData: RawData used by the current scene action.

    public static event Action<LoadScenePercentEventArgs> OnLoadScenePercentChange;
        Dispatched when completion percentage changes while loading a scene. Value is between 0f and 1f, while 1f is 100% done. Can be used for custom progress bars when loading scenes.
                LoadSceneQueueData RawData: RawData used by the current scene action.
                float Percent: Percentage of change completion. 1f is equal to 100 complete.
    
    public static event Action<LoadSceneEndEventArgs> OnLoadSceneEnd;
        Dispatched when a scene load ends.
            LoadSceneQueueData RawData: RawData used by the current scene action.
            Scene[] LoadedScenes: Scenes which were loaded.
            string[] SkippedSceneNames: Scenes which were skipped because they were already loaded.

    public static event Action<UnloadSceneStartEventArgs> OnUnloadSceneStart;
        Dispatched when a scene load starts.
        UnloadSceneQueueData RawData: RawData used by the current scene action.
   
    public static event Action<UnloadSceneEndEventArgs> OnUnloadSceneEnd;
        Dispatched when a scene load ends.
            UnloadSceneQueueData RawData: RawData used by the current scene action.

    public static event Action<ClientPresenceChangeEventArgs> OnClientPresenceChangeStart;
    public static event Action<ClientPresenceChangeEventArgs> OnClientPresenceChangeEnd;
        Dispatched when the clients presence changes for a scene.
            Scene Scene: Scene on the server which the client's presence has changed.
            NetworkConnection Connection: Connection to client.
            bool Added: True if the client was added to the scene, false is removed.