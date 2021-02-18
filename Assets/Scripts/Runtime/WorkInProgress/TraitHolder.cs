using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Events;

namespace Runtime.WorkInProgress
{
    public class TraitHolder
    {
        public UnityEvent TraitsChangedEvent { get; private set; }

        private readonly Dictionary<QueryPriority, Dictionary<TraitCategory, List<Trait>>> _traits;

        public TraitHolder()
        {
            _traits = new Dictionary<QueryPriority, Dictionary<TraitCategory, List<Trait>>>();

            foreach (var priority in (QueryPriority[]) Enum.GetValues(typeof(QueryPriority)))
            {
                _traits.Add(priority, new Dictionary<TraitCategory, List<Trait>>());

                foreach (var traitCategory in (TraitCategory[]) Enum.GetValues(typeof(TraitCategory)))
                {
                    _traits[priority].Add(traitCategory, new List<Trait>());
                }
            }

            TraitsChangedEvent = new UnityEvent();
        }

        public float QueryTotalValue(ref Query query)
        {
            foreach (var category in query.Categories)
            {
                foreach (var trait in _traits[QueryPriority.Basic][category])
                {
                    trait.CheckIfFulfillsQuery(ref query);
                }
            }
            
            foreach (var category in query.Categories)
            {
                foreach (var trait in _traits[QueryPriority.Per][category])
                {
                    trait.CheckIfFulfillsQuery(ref query);
                }
            }
            
            foreach (var category in query.Categories)
            {
                foreach (var trait in _traits[QueryPriority.If][category])
                {
                    trait.CheckIfFulfillsQuery(ref query);
                }
            }
            
            foreach (var category in query.Categories)
            {
                foreach (var trait in _traits[QueryPriority.While][category])
                {
                    trait.CheckIfFulfillsQuery(ref query);
                }
            }

            return query.Result;
        }
        
        
        public void AddTrait(Trait trait)
        {
            _traits[trait.Value.queryPriority][trait.Category].Add(trait);
            TraitsChangedEvent.Invoke();
        }
        
        public void AddTraits(Trait[] traits)
        {
            foreach (var trait in traits)
            {
                _traits[trait.Value.queryPriority][trait.Category].Add(trait);
            }
            TraitsChangedEvent.Invoke();
        }

        public void RemoveTrait(Trait trait)
        {
            _traits[trait.Value.queryPriority][trait.Category].Remove(trait);
            TraitsChangedEvent.Invoke();
        }
        
        public void RemoveTraits(Trait[] traits)
        {
            foreach (var trait in traits)
            {
                _traits[trait.Value.queryPriority][trait.Category].Remove(trait);
            }
            TraitsChangedEvent.Invoke();
        }
        
    }
}