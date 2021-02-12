using UnityEngine;

namespace Runtime.UI
{
    public class CharacterMenu : MonoBehaviour
    {
        [SerializeField] private GameObject content;
        [SerializeField] private GameObject characterButtonPrefab;

        private void Start()
        {
            CharacterService.Instance.characterListChanged.AddListener(OnGetCharacters);
        }

        private void OnGetCharacters()
        {
            foreach (Transform child in content.transform)
            {
                Debug.Log("Delete " + child.gameObject.name);
                Destroy(child.gameObject);
            }

            foreach (var characterInfo in CharacterService.Instance.characterInfos)
            {
                var button = Instantiate(characterButtonPrefab, content.transform);
                button.GetComponent<CharacterButton>().SetCharacter(characterInfo);
            }
        }
    
    }
}
