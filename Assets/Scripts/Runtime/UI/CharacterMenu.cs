using Runtime.Services;
using UnityEngine;

namespace Runtime.UI
{
    public class CharacterMenu : MonoBehaviour
    {
        [SerializeField] private GameObject content;
        [SerializeField] private GameObject characterButtonPrefab;
        [SerializeField] private GameObject characterCreationMenu;

        private void Start()
        {
            CharacterService.Instance.characterListChanged.AddListener(OnGetCharacters);
        }

        private void OnGetCharacters()
        {
            foreach (Transform child in content.transform) Destroy(child.gameObject);

            foreach (var characterInfo in CharacterService.Instance.clientSideCharacterList)
            {
                var button = Instantiate(characterButtonPrefab, content.transform);
                button.GetComponent<CharacterButton>().SetCharacter(characterInfo);
            }
        }

        public void ShowCharacterCreationMenu()
        {
            characterCreationMenu.SetActive(true);
            gameObject.SetActive(false);
        }

        public void DeleteCharacter()
        {
            if (!string.IsNullOrEmpty(CharacterButton.selectedCharacterId))
                //TODO: Context window to ask if you really want to delete, including a warning
                CharacterService.Instance.SendCharacterDeletionRequest(CharacterButton.selectedCharacterId);
        }

        public void OnPlay()
        {
            if (!string.IsNullOrEmpty(CharacterButton.selectedCharacterId))
                CharacterService.Instance.SendCharacterPlayRequest(CharacterButton.selectedCharacterId);
        }
    }
}