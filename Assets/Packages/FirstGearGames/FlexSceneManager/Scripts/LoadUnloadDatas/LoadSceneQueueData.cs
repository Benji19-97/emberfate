using Mirror;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;

namespace FirstGearGames.FlexSceneManager.LoadUnloadDatas
{


    /// <summary>
    /// Used to load a scene for a targeted connection.
    /// </summary>
    public class LoadSceneQueueData
    {
        /// <summary>
        /// Clients which receive this SceneQueueData. If Networked, all clients do. If Connections, only the specified Connections do.
        /// </summary>
        public SceneScopeTypes ScopeType;
        /// <summary>
        /// Connections to load scenes for. Only valid on the server and when ScopeType is Connections.
        /// </summary>
        [System.NonSerialized]
        public NetworkConnection[] Connections = new NetworkConnection[0];
        /// <summary>
        /// Single scene to load.
        /// </summary>
        public SingleSceneData SingleScene = null;
        /// <summary>
        /// Additive scenes to load.
        /// </summary>
        public AdditiveScenesData AdditiveScenes = null;
        /// <summary>
        /// Current data on networked scenes.
        /// </summary>
        public NetworkedScenesData NetworkedScenes = null;
        /// <summary>
        /// True if to iterate this queue data as server.
        /// </summary>
        [System.NonSerialized]
        public readonly bool AsServer;
        /// <summary>
        /// Load options for this scene queue data. This is only available on the server.
        /// </summary>
        [System.NonSerialized]
        public readonly LoadOptions LoadOptions = new LoadOptions();

        /// <summary>
        /// Creates an empty SceneQueueData that will serialize over the network.
        /// </summary>
        public LoadSceneQueueData()
        {
            MakeSerializable();
        }

        /// <summary>
        /// Creates a SceneQueueData.
        /// </summary>
        /// /// <param name="singleScene"></param>
        /// <param name="additiveScenes"></param>
        /// <param name="loadOnlyUnloaded"></param>
        public LoadSceneQueueData(SceneScopeTypes scopeType, NetworkConnection[] conns, SingleSceneData singleScene, AdditiveScenesData additiveScenes, LoadOptions loadOptions, NetworkedScenesData networkedScenes, bool asServer)
        {
            ScopeType = scopeType;
            Connections = conns;
            SingleScene = singleScene;
            AdditiveScenes = additiveScenes;
            LoadOptions = loadOptions;
            NetworkedScenes = networkedScenes;
            AsServer = asServer;
            
            MakeSerializable();
        }

        /// <summary>
        /// Ensures all values of this class can be serialized.
        /// </summary>
        public void MakeSerializable()
        {
            //Null single scene.
            if (SingleScene == null)
            {
                SingleScene = new SingleSceneData()
                {
                    SceneReferenceData = new SceneReferenceData(),
                    MovedNetworkIdentities = new NetworkIdentity[0]
                };
            }
            //Not null single scene.
            else
            {
                //Moved identities is null.
                if (SingleScene.MovedNetworkIdentities == null)
                {
                    SingleScene.MovedNetworkIdentities = new NetworkIdentity[0];
                }
                //Has moved identities.
                else
                {
                    //Remove null of unset network identities.
                    List<NetworkIdentity> listMovedIdentities = SingleScene.MovedNetworkIdentities.ToList();
                    for (int i = 0; i < listMovedIdentities.Count; i++)
                    {
                        if (listMovedIdentities[i] == null || listMovedIdentities[i].netId == 0)
                        {
                            listMovedIdentities.RemoveAt(i);
                            i--;
                        }
                    }
                    SingleScene.MovedNetworkIdentities = listMovedIdentities.ToArray();
                }

            }

            //Null additive scenes.
            if (AdditiveScenes == null)
            {
                AdditiveScenes = new AdditiveScenesData();
            }
            //Not null additive scenes.
            else
            {
                //Null scene datas.
                if (AdditiveScenes.SceneReferenceDatas == null)
                    AdditiveScenes.SceneReferenceDatas = new SceneReferenceData[0];
            }

            //Networked scenes.
            if (NetworkedScenes == null)
                NetworkedScenes = new NetworkedScenesData();

            //Connections.
            if (Connections == null)
                Connections = new NetworkConnection[0];
        }

    }


}