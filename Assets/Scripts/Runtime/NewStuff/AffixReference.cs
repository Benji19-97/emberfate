using System;

namespace Runtime.NewStuff
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