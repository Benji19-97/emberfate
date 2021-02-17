using System.Collections.Generic;

namespace Runtime.WorkInProgress
{
    public class TraitList
    {
        public TraitListKey Key;
        private List<Trait> _traits = new List<Trait>();

        public List<Trait> GetTraits()
        {
            return _traits;
        }

        public void AddTrait(Trait trait)
        {
            _traits.Add(trait);
        }

        public void RemoveTrait(Trait trait)
        {
            _traits.Remove(trait);
        }
    }
}