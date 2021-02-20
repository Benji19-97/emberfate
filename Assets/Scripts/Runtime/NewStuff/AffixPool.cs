using System;

namespace Runtime.NewStuff
{
    [Serializable]
    public struct AffixReference
    {
        public int affixIndex;
        public int weighting;
    }
    
    [Serializable]
    public class AffixPool
    {
        public AffixCollection affixCollection;
    }
}