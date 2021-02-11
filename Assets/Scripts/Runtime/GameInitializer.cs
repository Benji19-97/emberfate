using Mirror;
using Steamworks;
using UnityEngine;

namespace Runtime
{
    public class GameInitializer : MonoBehaviour
    {
        [SerializeField] private GameObject mainMenuCanvas;

        void Start()
        {
#if UNITY_SERVER
            return;
#endif
            if (SteamManager.Initialized)
            {
                string personaName = SteamFriends.GetPersonaName();

                Debug.Log("Welcome " + personaName);
                mainMenuCanvas.SetActive(true);
            }
            else
            {
#if UNITY_EDITOR
                Debug.Log("Please log into Steam!");
                UnityEditor.EditorApplication.isPlaying = false;
                return;
#endif
                Application.Quit();
            }
        }
    }
}