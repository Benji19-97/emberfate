using System;
using Runtime.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Runtime.UI
{
    public class ExitMenu : MonoBehaviour
    {
        [SerializeField] private Button exitToTitleButton;
        [SerializeField] private Button exitToCharacterSelectionButton;

        private void Start()
        {
            exitToTitleButton.onClick.AddListener(ExitToTitle);
            exitToCharacterSelectionButton.onClick.AddListener(ExitToCharacterSelection);
        }


        private void ExitToTitle()
        {
            EmberfateNetworkManager.Instance.StopClient();
            SceneManager.LoadScene("MainMenu");
        }

        private void ExitToCharacterSelection()
        {
            
        }
    }
}