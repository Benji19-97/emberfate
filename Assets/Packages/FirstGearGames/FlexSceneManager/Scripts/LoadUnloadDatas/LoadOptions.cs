using UnityEngine.SceneManagement;

namespace FirstGearGames.FlexSceneManager.LoadUnloadDatas
{
    public class LoadOptions
    {
        /// <summary>
        /// True if to automatically unload the loaded scenes when they are no longer being used.
        /// </summary>
        [System.NonSerialized]
        public bool AutomaticallyUnload = true;
        /// <summary>
        /// True if to only load scenes which are not yet loaded. When false a scene may load multiple times; this is known as scene stacking. This is only used by the server.
        /// </summary>
        [System.NonSerialized]
        public bool LoadOnlyUnloaded = true;
        /// <summary>
        /// True to automatically remove player objects with FlexSceneCheckers which are not being moved to a new scene. This only applies when loading a single scene.
        /// </summary>
        [System.NonSerialized]
        public bool RemovePlayerObjects = true;
        /// <summary>
        /// LocalPhysics mode to use when loading this scene. Generally this will only be used when applying scene stacking. Only used by the server.
        /// https://docs.unity3d.com/ScriptReference/SceneManagement.LocalPhysicsMode.html
        /// </summary>
        [System.NonSerialized]
        public LocalPhysicsMode LocalPhysics = LocalPhysicsMode.None;
        /// <summary>
        /// Parameters which can be passed into a scene load. Params can be useful to link personalized data with scene load callbacks, such as a match Id.
        /// </summary>
        [System.NonSerialized]
        public object[] Params = null;
    }


}