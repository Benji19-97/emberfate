using System;
using System.Collections.Generic;
using Runtime.WorkInProgress;

namespace Runtime.NewStuff
{
    [Serializable]
    public class LocalModifier
    {
        public List<TraitTag> tags;
        public Modifier modifier;
        public float value;

        public float GetValue()
        {
            return modifier != null ? modifier.ApplyModifier(value) : value;
        }
    }
}