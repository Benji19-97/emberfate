using Runtime.Models;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Runtime.UI
{
    public class CharacterButton : MonoBehaviour
    {
#pragma warning disable 649
        private static readonly UnityEvent ResetSelection = new UnityEvent();
#pragma warning restore 649

        [SerializeField] private Button selectButton;
        [SerializeField] private Image buttonBackgroundImage;
        [SerializeField] private Text nameText;
        [SerializeField] private Text infoText;
        [SerializeField] private Color selectedColor;

        private Character _attachedCharacter;

        private Color _defaultColor;

        public static string selectedCharacterId { get; private set; }

        private void Start()
        {
            _defaultColor = buttonBackgroundImage.color;
            ResetSelection.AddListener(OnResetSelection);
            selectButton.onClick.AddListener(OnPress);
        }

        private void OnResetSelection()
        {
            buttonBackgroundImage.color = _defaultColor;
        }

        public void SetCharacter(Character character)
        {
            _attachedCharacter = character;
            nameText.text = character.name;
            // infoText.text = "Level " + info.level + " " + info.@class;
            infoText.text = character.ownerSteamId;
        }

        private void OnPress()
        {
            ResetSelection.Invoke();
            selectedCharacterId = _attachedCharacter.id;
            buttonBackgroundImage.color = selectedColor;
        }
    }
}