using System.Collections.Generic;
using UnityEngine;

namespace Runtime.NewStuff
{
    [CreateAssetMenu(fileName = "NewAffixCollection", menuName = "Data/Create Affix Collection", order = 0)]
    public class AffixCollection : ScriptableObject
    {
        public TraitCollectionDictionary traitCollectionDictionary;
        public Affix[] affixes;
    }
}