using System;
using System.Collections.Generic;
using Runtime.NewStuff;
using Runtime.Traits.Enums;

namespace Runtime.Traits.Serializable
{
    [Serializable]
    public class Trait
    {
        public Trait()
        {
            name = "New Trait";
            effect = null;
            modifier = null;
            valueTypes = 0;
            category = TraitCategory.Miscellaneous;
            tags = new List<TraitTag>();
            notes = "";
            isLocalModifier = false;
            isLocked = false;
        }

        //Don't change naming
        public string name;
        public Effect effect;
        public Modifier modifier;
        public int valueTypes;
        public TraitCategory category;
        public List<TraitTag> tags; 
        public string notes;
        public bool isLocalModifier;
        public bool isLocked;
        
        
        public static List<TraitValueType> ReturnSelectedElements(int versions)
        {
            List<TraitValueType> selectedElements = new List<TraitValueType>();

            var enums = (TraitValueType[]) Enum.GetValues(typeof(TraitValueType));
            
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