using System;
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

        private static string m_selectedCharacter;

        [SerializeField] private Button selectButton;
        [SerializeField] private Image buttonBackgroundImage;
        [SerializeField] private Text nameText;
        [SerializeField] private Text infoText;
        [SerializeField] private Color selectedColor;

        private Color _defaultColor;

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

        public void SetCharacter(CharacterInfo info)
        {
            nameText.text = info.name;
            infoText.text = "Level " + info.level + " " + info.@class;
        }

        private void OnPress()
        {
            ResetSelection.Invoke();
            m_selectedCharacter = nameText.text;
            buttonBackgroundImage.color = selectedColor;
        }
        
        
    
    }
}
