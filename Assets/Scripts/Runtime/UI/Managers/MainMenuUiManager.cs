using System;
using Runtime.Core;
using UnityEngine;

namespace Runtime.UI.Managers
{
    public class MainMenuUiManager : MonoBehaviour
    {
        public static MainMenuUiManager Instance;

        public GameObject loginMenu;
        public GameObject characterSelectionMenu;

        private void Awake()
        {
            Instance = this;
        }

        public void LogOut()
        {
#if !UNITY_SERVER
            EmberfateNetworkManager.Instance.Disconnect();
#endif
        }
    }
}