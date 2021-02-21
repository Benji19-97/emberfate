using System;
using System.Collections.Generic;
using Runtime.WorkInProgress;

namespace Runtime.NewStuff
{
    public enum TraitVersion
    {
        AddsRemovesRangeFlat,
        AddsRemovesFixedFlat,
        AddsRemovesFixedPercentage,
        IncreasesReducesFixedPercentage,
        MoreLessFixedPercentage
    }

    [Serializable]
    public class Trait
    {
        public Trait()
        {
            name = "";
            effect = null;
            modifier = null;
            versions = 0;
            category = TraitCategory.Attribute;
            tags = new List<TraitTag>();
            notes = "";
            isLocked = false;
        }

        public string name;
        public Effect effect;
        public Modifier modifier;
        public int versions;
        public TraitCategory category;
        public List<TraitTag> tags; 
        public string notes;
        public bool isLocalModifier;
        public bool isLocked;
        
        public static List<TraitVersion> ReturnSelectedElements(int versions)
        {
            List<TraitVersion> selectedElements = new List<TraitVersion>();

            var enums = (TraitVersion[]) Enum.GetValues(typeof(TraitVersion));
            
            for (int i = 0; i < enums.Length; i++)
            {
                int layer = 1 << i;
                if ((versions & layer) != 0)
                {
                    selectedElements.Add(enums[i]);
                }
            }
 
            return selectedElements;
        }
    }
}