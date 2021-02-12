using FirstGearGames.FlexSceneManager.LoadUnloadDatas;
using Mirror;

namespace FirstGearGames.FlexSceneManager.Messages
{

    /// <summary>
    /// Sent to clients to load networked scenes.
    /// </summary>
    public struct LoadScenesMessage : NetworkMessage
    {
        public LoadSceneQueueData SceneQueueData;
    }


    /// <summary>
    /// Sent to clients to unload networked scenes.
    /// </summary>
    public struct UnloadScenesMessage : NetworkMessage
    {
        public UnloadSceneQueueData SceneQueueData;
    }


    /// <summary>
    /// Sent to server to indicate which scenes a client has loaded.
    /// </summary>
    public struct ClientScenesLoadedMessage : NetworkMessage
    {
        public SceneReferenceData[] SceneDatas;
    }

    public struct ClientPlayerCreated : NetworkMessage
    {

    }

}