using System;
using System.Collections.Generic;
using Runtime.WorkInProgress;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// ReSharper disable InconsistentNaming

namespace Runtime.NewStuff
{
    [Serializable]
    public abstract class Effect : ScriptableObject
    {
    }

    [Serializable]
    public abstract class Modifier : ScriptableObject
    {
    }

    public enum ValueType
    {
        Fixed,
        Range
    }

    [CreateAssetMenu(menuName = "Traits/Create Trait Collection", fileName = "TraitCollection", order = 0)]
    public class TraitCollection : ScriptableObject
    {
        public Trait[] Traits = Array.Empty<Trait>();
    }


    [Serializable]
    public struct Trait
    {
        public string Name;
        public Effect Effect; //Choose from List (has it's own editor somewhere else)
        public Modifier Modifier; //Choose from List (has it's own editor somewhere else)
        public TraitOperator @operator; //Enum Field
        public TraitCategory Category; //Enum Field
        public TraitTag[] Tags; //Needs separate field implementation
        public ValueType ValueType;
        public bool IsPercentage; //Boolean Field
        public string Notes; //Text Field
    }
}