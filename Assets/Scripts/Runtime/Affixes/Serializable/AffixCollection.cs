using Runtime.NewStuff;
using Runtime.Traits.Serializable;
using UnityEngine;

namespace Runtime.Affixes.Serializable
{
    [CreateAssetMenu(fileName = "NewAffixCollection", menuName = "Game Data/Create Affix Collection", order = 0)]
    public class AffixCollection : ScriptableObject
    {
        public TraitCollectionLibrary traitCollectionLibrary;
        public Affix[] affixes;
    }
}