using Mirror;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FirstGearGames.FlexSceneManager
{

    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkIdentity))]
    public class FlexSceneChecker : NetworkVisibility
    {
        #region Public.
        /// <summary>
        /// Scenes this object resides in.
        /// </summary>
        public HashSet<Scene> CurrentScenes { get; private set; } = new HashSet<Scene>();
        #endregion

        #region Serialized.
        /// <summary>
        /// Enable to force this object to be hidden from all observers.
        /// <para>If this object is a player object, it will not be hidden for that client.</para>
        /// </summary>
        [Tooltip("Enable to force this object to be hidden from all observers. If this object is a player object, it will not be hidden for that client.")]
        [SerializeField]
        private bool _forceHidden = false;
        /// <summary>
        /// True to add this object to whichever scene it is spawned in.
        /// </summary>
        [Tooltip("True to add this object to whichever scene it is spawned in.")]
        [SerializeField]
        private bool _addToCurrentScene = true;
        #endregion

        private void OnEnable()
        {
            if (NetworkServer.active)
            {
                FlexSceneManager.AddSceneChecker(this);
            }
        }
        private void OnDisable()
        {
            if (NetworkServer.active)
            {
                FlexSceneManager.RemoveSceneChecker(this);
            }
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            if (_addToCurrentScene)
            {
                CurrentScenes.Add(gameObject.scene);
                /* If this has a connection it also needs to be added
                 * in the manager to have observers updated. */
                if (base.connectionToClient != null)
                    FlexSceneManager.AddToScene(gameObject.scene, base.connectionToClient);
            }
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            if (_addToCurrentScene)
            {
                CurrentScenes.Remove(gameObject.scene);
                /* If this has a connection it also needs to be removed
                * in the manager to have observers updated. */
                if (base.connectionToClient != null)
                    FlexSceneManager.RemoveFromScene(gameObject.scene, base.connectionToClient);
            }
        }

        /// <summary>
        /// Adds this object to a scene for observers.
        /// </summary>
        /// <param name="s"></param>
        [Server]
        public void AddedToScene(Scene s)
        {
            if (string.IsNullOrEmpty(s.name))
                return;

            CurrentScenes.Add(s);
        }

        /// <summary>
        /// Removes this object from a scene for observers.
        /// </summary>
        /// <param name="s"></param>
        [Server]
        public void RemovedFromScene(Scene s)
        {
            if (string.IsNullOrEmpty(s.name))
                return;

            CurrentScenes.Remove(s);
        }

        /// <summary>
        /// Replaces all scenes this object is in with a new scene.
        /// </summary>
        /// <param name="s"></param>
        [Server]
        public void ReplacedScene(Scene s)
        {
            if (string.IsNullOrEmpty(s.name))
                return;

            CurrentScenes.Clear();
            AddedToScene(s);
        }

        /// <summary>
        /// Manually rebuilds observers.
        /// </summary>
        [Server]
        public void RebuildObservers()
        {
            base.netIdentity.RebuildObservers(false);
        }

        /// <summary>
        /// Callback used by the visibility system to determine if an observer (player) can see this object.
        /// <para>If this function returns true, the network connection will be added as an observer.</para>
        /// </summary>
        /// <param name="conn">Network connection of a player.</param>
        /// <returns>True if the player can see this object.</returns>
        public override bool OnCheckObserver(NetworkConnection conn)
        {
            if (_forceHidden)
                return false;

            HashSet<NetworkConnection> sceneConnections;
            /* Get network identities for the scene which this object resides.
             * If the scene is found in the collection return if the network identity
             * for the connection is found in the scene. */
            if (FlexSceneManager.SceneConnections.TryGetValue(gameObject.scene, out sceneConnections))
                return sceneConnections.Contains(conn);

            //Fall through. Scene doesn't exist in collection therefor no identities are added to it.
            return false;
        }


        /// <summary>
        /// Callback used by the visibility system to (re)construct the set of observers that can see this object.
        /// <para>Implementations of this callback should add network connections of players that can see this object to the observers set.</para>
        /// </summary>
        /// <param name="observers">The new set of observers for this object.</param>
        /// <param name="initialize">True if the set of observers is being built for the first time.</param>
        public override void OnRebuildObservers(HashSet<NetworkConnection> observers, bool initialize)
        {
            //Hidden to clients, don't add any observers.
            if (_forceHidden)
                return;

            //For all objects which exist in the same scene as this one, add as an observer.
            HashSet<NetworkConnection> sceneConnections;
            foreach (Scene item in CurrentScenes)
            {
                if (FlexSceneManager.SceneConnections.TryGetValue(item, out sceneConnections))
                {
                    foreach (NetworkConnection conn in sceneConnections)
                    {
                        if (conn != null)
                            observers.Add(conn);
                    }
                }
            }
        }


    }
}
