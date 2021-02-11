using Steamworks;
using UnityEngine;

namespace Runtime
{
    public class SteamInitializer : MonoBehaviour
    {
        [SerializeField] private GameObject mainMenuCanvas;
        
        void Start()
        {
            if (SteamManager.Initialized)
            {
                string personaName = SteamFriends.GetPersonaName();
#if UNITY_EDITOR
                Debug.Log("Welcome " + personaName);
                mainMenuCanvas.SetActive(true);
#endif
            }
            else
            {
#if UNITY_EDITOR
                Debug.Log("Please log into Steam!");
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
        }
    }
}