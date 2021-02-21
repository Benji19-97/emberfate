using System;
using System.Collections.Generic;
using Runtime.Affixes.Serializable;

namespace Runtime.NewStuff
{
    [Serializable]
    public struct AffixPoolReference
    {
        public AffixPool affixPool;
        public int weighting;
    }


    
    [Serializable]
    public class BaseItem
    {
        public AffixCollection affixCollection;

        public int inherentAffixIndex;
        public int implicitAffixIndex;
        
        public int[] scaling;
        public List<AffixPoolReference> affixPools;
    }
}