using System;
using UnityEngine;

namespace Runtime.NewStuff
{
    [CreateAssetMenu(menuName = "Data/Create Trait Collection", fileName = "TraitCollection", order = 0)]
    public class TraitCollection : ScriptableObject
    {
        public Trait[] traits = Array.Empty<Trait>();
    }
}