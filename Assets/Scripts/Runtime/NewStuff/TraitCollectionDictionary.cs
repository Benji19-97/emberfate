using UnityEngine;

namespace Runtime.NewStuff
{
    [CreateAssetMenu(fileName = "TraitCollectionDictionary", menuName = "Data/Create Trait Collection Dictionary", order = 0)]
    public class TraitCollectionDictionary : ScriptableObject
    {
        public TraitCollection[] traitCollections;
    }
}