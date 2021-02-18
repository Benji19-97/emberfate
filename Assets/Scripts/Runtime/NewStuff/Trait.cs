using System;
using System.Collections.Generic;
using Runtime.WorkInProgress;
using UnityEngine;

// ReSharper disable InconsistentNaming

namespace Runtime.NewStuff
{
    public struct Effect
    {
    }

    public struct Modifer
    {
        
    }

    [CreateAssetMenu(menuName = "Traits/Create Trait Collection", fileName = "TraitCollection", order = 0)]
    public class TraitCollection : ScriptableObject
    {
        public Trait[] Traits;
    }


    [Serializable]
    public struct Trait
    {
        public int Effect; //Choose from List (has it's own editor somewhere else)
        public int Modifer; //Choose from List (has it's own editor somewhere else)
        public TraitOperation Operation; //Enum Field
        public TraitCategory Category; //Enum Field
        public TraitTag[] Tags; //Needs separate field implementation
        public float[] Values; //Needs separate field implementation
        public bool Percentage; //Boolean Field
        public string ReadableFormat; //Text Field
        public string Notes; //Text Field
    }
}