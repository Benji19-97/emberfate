using System;
using UnityEngine;

namespace Runtime.NewStuff
{
    [Serializable]
    public abstract class Modifier : ScriptableObject
    {
        public abstract float ApplyModifier(float input);
    }
}