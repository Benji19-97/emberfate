using System;
using System.Collections.Generic;
using Runtime.WorkInProgress;

namespace Runtime.NewStuff
{
    public enum TraitVersion
    {
        AddsRemovesRangeFlat = 0,
        AddsRemovesFixedFlat = 1,
        AddsRemovesFixedPercentage = 2,
        IncreasesReducesFixedPercentage = 3,
        MoreLessFixedPercentage = 4
    }

    [Serializable]
    public class Trait
    {
        public Trait()
        {
            name = "";
            effect = null;
            modifier = null;
            versions = new bool[0];
            category = TraitCategory.Attribute;
            tags = new List<TraitTag>();
            notes = "";
        }

        public string name;
        public Effect effect;
        public Modifier modifier;
        public bool[] versions;
        public TraitCategory category;
        public List<TraitTag> tags; 
        public string notes;
        public bool isLocalModifier;
    }
}