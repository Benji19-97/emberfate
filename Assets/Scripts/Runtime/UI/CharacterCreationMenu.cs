using System;
using UnityEngine;
using UnityEngine.UI;

namespace Runtime.UI
{
    public class CharacterCreationMenu : MonoBehaviour
    {
        [SerializeField] private GameObject characterMenu;
        
        [SerializeField] private InputField nameInputField;
        [SerializeField] private Dropdown classDropdown;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;

        private void Start()
        {
            confirmButton.onClick.AddListener(OnConfirm);
            cancelButton.onClick.AddListener(OnCancel);
            CharacterService.Instance.characterCreationAnswer.AddListener(OnCancel);
        }

        private void OnConfirm()
        {
            CharacterService.Instance.SendCharacterCreationRequest(new CharacterInfo()
            {
                name = nameInputField.text,
                @class = classDropdown.options[classDropdown.value].text
            });
            
            nameInputField.interactable = false;
            classDropdown.interactable = false;
        }

        private void OnCancel()
        {
            ResetValues();
            characterMenu.SetActive(true);
            gameObject.SetActive(false);
        }

        private void ResetValues()
        {
            nameInputField.interactable = true;
            classDropdown.interactable = true;
            nameInputField.text = "";
            classDropdown.value = 0;
        }
        
        
        
    }
}