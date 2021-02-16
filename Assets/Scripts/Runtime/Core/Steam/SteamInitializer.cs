using Runtime.Helpers;
using Runtime.UI;
using Steamworks;
using UnityEngine;
using UnityEngine.Events;
using GameServer = Runtime.Core.Server.GameServer;

namespace Runtime.Core.Steam
{
    public class SteamInitializer : MonoBehaviour
    {
        public static SteamInitializer Instance;

        [SerializeField] private GameObject mainMenuCanvas;

        public UnityEvent initializedSteam;

        private void Awake()
        {
#if UNITY_SERVER
            ServerLogger.LogWarning("Destroying SteamInitializer.");
            Destroy(gameObject);
            return;
#elif UNITY_EDITOR
            if (GameServer.START_SERVER_IN_UNITY_EDITOR)
            {
                ServerLogger.LogWarning("Destroying SteamInitializer.");
                Destroy(gameObject);
                return;
            }
#endif

            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }


        private void Start()
        {
#if UNITY_SERVER
            return;
#elif UNITY_EDITOR
            if (GameServer.START_SERVER_IN_UNITY_EDITOR) return;
#endif
            NotificationSystem.Push("Initializing Steam ...", false);
            if (SteamManager.Initialized)
            {
                var personaName = SteamFriends.GetPersonaName();

                NotificationSystem.Push("Successfully initialized Steam. Welcome " + personaName + "!", true);
                mainMenuCanvas.SetActive(true); //TODO: Game manager that handles things like this with the event below?
                initializedSteam.Invoke();
            }
            else
            {
                NotificationSystem.Push("Couldn't initialize Steam. Are you sure Steam is open?", true);
            }
        }
    }
}