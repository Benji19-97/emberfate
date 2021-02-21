using System;

namespace Runtime.Affixes.Serializable
{
    [Serializable]
    public class AffixReference
    {
        public AffixReference()
        {
            affixIndex = 0;
            weighting = 0;
        }
        
        public int affixIndex;
        public int weighting;
    }
}