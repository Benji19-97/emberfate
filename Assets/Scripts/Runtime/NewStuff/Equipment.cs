using System;
using System.Collections.Generic;

namespace Runtime.NewStuff
{

    [Serializable]
    public struct SerializedAffix
    {
        public int affixPoolIndex;
        public float affixRoll;
    }
    
    public class Equipment
    {
        public Equipment()
        {
            Affixes = new List<SerializedAffix>();
        }

        public int Rarity;
        public int Quality;
        public int ItemBaseIndex;
        public List<SerializedAffix> Affixes;
        [NonSerialized] public List<LocalModifier> LocalModifiers;
    }


}