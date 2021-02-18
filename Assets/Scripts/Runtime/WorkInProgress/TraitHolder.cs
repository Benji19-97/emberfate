using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Events;

namespace Runtime.WorkInProgress
{
    public class TraitHolder
    {
        public UnityEvent TraitsChangedEvent { get; private set; }

        private readonly Dictionary<QueryPriority, Dictionary<TraitCategory, List<InstantiatedTrait>>> _traits;

        public TraitHolder()
        {
            _traits = new Dictionary<QueryPriority, Dictionary<TraitCategory, List<InstantiatedTrait>>>();

            foreach (var priority in (QueryPriority[]) Enum.GetValues(typeof(QueryPriority)))
            {
                _traits.Add(priority, new Dictionary<TraitCategory, List<InstantiatedTrait>>());

                foreach (var traitCategory in (TraitCategory[]) Enum.GetValues(typeof(TraitCategory)))
                {
                    _traits[priority].Add(traitCategory, new List<InstantiatedTrait>());
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
        
        
        public void AddTrait(InstantiatedTrait instantiatedTrait)
        {
            _traits[instantiatedTrait.Value.QueryPriority][instantiatedTrait.Category].Add(instantiatedTrait);
            TraitsChangedEvent.Invoke();
        }
        
        public void AddTraits(InstantiatedTrait[] traits)
        {
            foreach (var trait in traits)
            {
                _traits[trait.Value.QueryPriority][trait.Category].Add(trait);
            }
            TraitsChangedEvent.Invoke();
        }

        public void RemoveTrait(InstantiatedTrait instantiatedTrait)
        {
            _traits[instantiatedTrait.Value.QueryPriority][instantiatedTrait.Category].Remove(instantiatedTrait);
            TraitsChangedEvent.Invoke();
        }
        
        public void RemoveTraits(InstantiatedTrait[] traits)
        {
            foreach (var trait in traits)
            {
                _traits[trait.Value.QueryPriority][trait.Category].Remove(trait);
            }
            TraitsChangedEvent.Invoke();
        }
        
        public InstantiatedTrait[] GetAllTraits(TraitCategory category)
        {
            InstantiatedTrait[] traits = Array.Empty<InstantiatedTrait>();
            foreach (var priority in (QueryPriority[]) Enum.GetValues(typeof(QueryPriority)))
            {
                traits.Concat(_traits[priority][category]);
            }
            return traits;
        }

        public InstantiatedTrait[] GetTraits(TraitCategory category, QueryPriority priority)
        {
            return _traits[priority][category].ToArray();
        }
        
        
    }
}