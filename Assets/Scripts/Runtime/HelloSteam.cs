using Steamworks;
using UnityEngine;

namespace Runtime
{
    public class HelloSteam : MonoBehaviour
    {
        void Start()
        {
            if (SteamManager.Initialized)
            {
                string personaName = SteamFriends.GetPersonaName();
                Debug.Log(personaName);
            }
        }
    }
}