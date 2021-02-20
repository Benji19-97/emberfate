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
        }

        public string name;
        public int traitCollectionIdx;
        public int traitIdx;
        public TraitVersion traitVersion;
        public float[] valuesPowerMin;
        public float[] valuesPowerMax;
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
            // switch (traitVersion)
            // {
            //     case TraitVersion.AddsRemovesRangeFlat:
            //         break;
            //     case TraitVersion.AddsRemovesFixedFlat:
            //         break;
            //     case TraitVersion.AddsRemovesFixedPercentage:
            //         break;
            //     case TraitVersion.IncreasesReducesFixedPercentage:
            //         break;
            //     case TraitVersion.MoreLessFixedPercentage:
            //         break;
            // }

            return 0f;
        }
    }
}
