using Mirror;

namespace FirstGearGames.FlexSceneManager.LoadUnloadDatas
{


    public class SingleSceneData
    {
        /// <summary>
        /// SceneReferenceData for each scene to load.
        /// </summary>
        public SceneReferenceData SceneReferenceData;
        /// <summary>
        /// NetworkIdentities to move to the new single scene.
        /// </summary>
        public NetworkIdentity[] MovedNetworkIdentities;

        /// <summary>
        /// String to display when a scene name is null or empty.
        /// </summary>
        private const string NULL_EMPTY_SCENE_NAME = "SingleSceneData is being generated using a null or empty sceneName. If this was intentional, you may ignore this warning.";

        public SingleSceneData()
        {
            MovedNetworkIdentities = new NetworkIdentity[0];
        }

        public SingleSceneData(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
                UnityEngine.Debug.LogWarning(NULL_EMPTY_SCENE_NAME);

            SceneReferenceData = new SceneReferenceData() { Name = sceneName };
            MovedNetworkIdentities = new NetworkIdentity[0];
        }
        public SingleSceneData(SceneReferenceData sceneReferenceData)
        {
            SceneReferenceData = sceneReferenceData;
            MovedNetworkIdentities = new NetworkIdentity[0];
        }
        public SingleSceneData(string sceneName, NetworkIdentity[] movedNetworkIdentities)
        {
            if (string.IsNullOrEmpty(sceneName))
                UnityEngine.Debug.LogWarning(NULL_EMPTY_SCENE_NAME);

            SceneReferenceData = new SceneReferenceData() { Name = sceneName };
            MovedNetworkIdentities = movedNetworkIdentities;
        }
        public SingleSceneData(SceneReferenceData sceneReferenceData, NetworkIdentity[] movedNetworkIdentities)
        {
            SceneReferenceData = sceneReferenceData;
            MovedNetworkIdentities = movedNetworkIdentities;
        }
    }

    public class AdditiveScenesData
    {
        /// <summary>
        /// SceneReferenceData for each scene to load.
        /// </summary>
        public SceneReferenceData[] SceneReferenceDatas;

        /// <summary>
        /// String to display when scene names is null or of zero length.
        /// </summary>
        private const string NULL_SCENE_NAME_COLLECTION = "AdditiveScenesData is being generated using null or empty sceneNames. If this was intentional, you may ignore this warning.";
        /// <summary>
        /// String to display when a scene name is null or empty.
        /// </summary>
        private const string NULL_EMPTY_SCENE_NAME = "AdditiveSceneData is being generated using a null or empty sceneName. If this was intentional, you may ignore this warning.";

        public AdditiveScenesData()
        {
            SceneReferenceDatas = new SceneReferenceData[0];
        }

        public AdditiveScenesData(string[] sceneNames)
        {
            if (sceneNames == null || sceneNames.Length == 0)
                UnityEngine.Debug.LogWarning(NULL_SCENE_NAME_COLLECTION);

            SceneReferenceDatas = new SceneReferenceData[sceneNames.Length];
            for (int i = 0; i < sceneNames.Length; i++)
            {
                if (string.IsNullOrEmpty(sceneNames[i]))
                    UnityEngine.Debug.LogWarning(NULL_EMPTY_SCENE_NAME);

                SceneReferenceDatas[i] = new SceneReferenceData { Name = sceneNames[i] };
            }
        }

        public AdditiveScenesData(SceneReferenceData[] sceneReferenceDatas)
        {
            SceneReferenceDatas = sceneReferenceDatas;
        }
    }


}