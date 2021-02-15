using System;
using Mirror;
using Runtime.UI;
using Steamworks;
using UnityEngine;
using UnityEngine.Events;

namespace Runtime
{
    public class SteamInitializer : MonoBehaviour
    {
        public static SteamInitializer Instance;

        [SerializeField] private GameObject mainMenuCanvas;

        public UnityEvent initializedSteam;

        private void Awake()
        {
#if UNITY_SERVER
            Destroy(gameObject);
#endif

#if UNITY_EDITOR
            if (GameServer.START_SERVER_IN_UNITY_EDITOR)
            {
                Destroy(gameObject);
                return;
            }
#endif

            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }


        void Start()
        {
#if UNITY_SERVER
            return;
#endif
            NotificationSystem.Instance.Push("Initializing Steam ...", false);
            if (SteamManager.Initialized)
            {
                string personaName = SteamFriends.GetPersonaName();

                NotificationSystem.Instance.Push("Successfully initialized Steam. Welcome " + personaName + "!", true);
                mainMenuCanvas.SetActive(true); //TODO: Game manager that handles things like this with the event below?
                initializedSteam.Invoke();
            }
            else
            {
#if UNITY_EDITOR
                NotificationSystem.Instance.Push("Couldn't initialize Steam. Are you sure Steam is open?", true);
                // UnityEditor.EditorApplication.isPlaying = false;
                return;
#endif
                // Application.Quit();
            }
        }
    }
}