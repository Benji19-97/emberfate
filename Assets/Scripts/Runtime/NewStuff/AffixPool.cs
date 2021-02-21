using System.Collections.Generic;
using UnityEngine;

namespace Runtime.NewStuff
{
    [CreateAssetMenu(fileName = "NewAffixPool", menuName = "Data/Create Affix Pool", order = 0)]
    public class AffixPool : ScriptableObject
    {
        public AffixCollection collection;
        public List<AffixReference> affixes;
    }
}