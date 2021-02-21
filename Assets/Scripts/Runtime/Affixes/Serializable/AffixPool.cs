using System.Collections.Generic;
using Runtime.NewStuff;
using UnityEngine;

namespace Runtime.Affixes.Serializable
{
    [CreateAssetMenu(fileName = "NewAffixPool", menuName = "Game Data/Create Affix Pool", order = 0)]
    public class AffixPool : ScriptableObject
    {
        public AffixCollection collection;
        public List<AffixReference> affixes;
    }
}