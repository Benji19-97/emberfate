using System;
using System.Collections.Generic;
using System.Linq;

namespace Runtime.NewStuff
{
    public enum AffixType
    {
        Inherent,
        Implicit,
        Prefix,
        Suffix
    }

    [Serializable]
    public class Affix
    {
        public Affix()
        {
            traitCollectionIdx = 0;
            traitIdx = 0;
            traitVersion = TraitVersion.AddsRemovesFixedFlat;
            valuesPowerMin = new[] {0f};
            valuesPowerMax = new[] {0f};
            readableFormat = "";
            affixType = AffixType.Prefix;
            name = "";
            isLocked = false;
        }

        public bool isLocked;
        public string name;
        public int traitCollectionIdx;
        public int traitIdx;
        public TraitVersion traitVersion;
        public float[] valuesPowerMin;
        public float[] valuesPowerMax;
        public float variance;
        public string readableFormat;
        public AffixType affixType;
        
        public string GetReadableFormat()
        {
            return "no readable format defined yet";
        }

        public float GetValue(TraitCollectionDictionary dictionary, float roll, List<LocalModifier> localModifiers)
        {
            var value = GetValueBeforeModifiers(roll);
            var trait = dictionary.traitCollections[traitCollectionIdx].traits[traitIdx];
            var modifier = trait.modifier;

            var localModifierValue = 1f;
            if (localModifiers != null && localModifiers.Any())
            {
                foreach (var localModifier in localModifiers)
                {
                    if (trait.tags.Intersect(localModifier.tags).Count() == localModifier.tags.Count)
                    {
                        localModifierValue += localModifier.GetValue();
                    }
                }
            }

            value *= localModifierValue;

            if (modifier == null)
            {
                return value;
            }
            else
            {
                return modifier.ApplyModifier(value);
            }
        }

        private float GetValueBeforeModifiers(float roll)
        {
            return 0f;
        }

        public float[] GetValueRolledAndScaled(int roll, int power)
        {
            float powerPercent = power / 60f;
            float rollPercent = roll / 100f;

            if (traitVersion == TraitVersion.AddsRemovesRangeFlat)
            {
                var value1 = (valuesPowerMax[0] - valuesPowerMin[0]) * powerPercent + valuesPowerMin[0];
                var possibleMin1 = value1 * (1f - variance / 2f);
                var possibleMax1 = value1 * (1f + variance / 2f);
                
                var rolledValue1 = (possibleMax1 - possibleMin1) * rollPercent + possibleMin1;
                
                var value2 = (valuesPowerMax[1] - valuesPowerMin[1]) * powerPercent + valuesPowerMin[1];
                var possibleMin2 = value2 * (1f - variance / 2f);
                var possibleMax2 = value2 * (1f + variance / 2f);
                
                var rolledValue2 = (possibleMax2 - possibleMin2) * rollPercent + possibleMin2;
                return new[] {rolledValue1, rolledValue2};
            }
            else
            {
                var value = (valuesPowerMax[0] - valuesPowerMin[0]) * powerPercent + valuesPowerMin[0];
                var possibleMin = value * (1f - variance / 2f);
                var possibleMax = value * (1f + variance / 2f);
                
                var rolledValue = (possibleMax - possibleMin) * rollPercent + possibleMin;
                return new[] {rolledValue};
            }
            
            
        }
    }
}
