using UnityEngine;

namespace Runtime.Traits.Serializable
{
    [CreateAssetMenu( menuName = "Game Data/Create Trait Collection Library", fileName = "TraitCollectionLibrary", order = 0)]
    public class TraitCollectionLibrary : ScriptableObject
    {
        public TraitCollection[] collections;
    }
}