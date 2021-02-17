using System.Collections.Generic;
using System.Linq;
using UnityEngine.Events;

namespace Runtime.WorkInProgress
{
    public enum TraitListKey
    {
        Class,
        Actor,
        Buffs,
        Passives,
        Helmet,
        BodyArmor,
        Boots,
        Gloves,
        Belt,
        Ring1,
        Ring2,
        Amulet,
        MainHand,
        OffHand
    }
    
    public class TraitListContainer
    {
        public UnityEvent TraitsChangedEvent = new UnityEvent();
        
        private Dictionary<TraitListKey, TraitList> _traitLists = new Dictionary<TraitListKey, TraitList>();

        public void RegisterTraitList(TraitListKey key, TraitList list)
        {
            if (_traitLists.ContainsKey(key)) return;
            
            list.Key = key;
            _traitLists.Add(key, list);
            TraitsChangedEvent.Invoke();
        }

        public void DeregisterTraitList(TraitListKey key)
        {
            if (!_traitLists.ContainsKey(key)) return;
            
            _traitLists.Remove(key);
            TraitsChangedEvent.Invoke();
        }

        public void SwapTraitList(TraitListKey key, TraitList list)
        {
            if (!_traitLists.ContainsKey(key)) return;
            _traitLists.Remove(key);
            _traitLists.Add(key, list);
            TraitsChangedEvent.Invoke();
        }

        public TraitList GetTraitsList(TraitListKey key)
        {
            if (_traitLists.TryGetValue(key, out var value))
            {
                return value;
            };

            return null;
        }

        public float QueryTotalValue(ref Query query)
        {
            foreach (var trait in _traitLists.SelectMany(traitList => traitList.Value.GetTraits()))
            {
                trait.CheckIfFulfillsQuery(ref query, QueryType.Basic);
            }
            
            foreach (var trait in _traitLists.SelectMany(traitList => traitList.Value.GetTraits()))
            {
                trait.CheckIfFulfillsQuery(ref query, QueryType.Per);
            }
            
            foreach (var trait in _traitLists.SelectMany(traitList => traitList.Value.GetTraits()))
            {
                trait.CheckIfFulfillsQuery(ref query, QueryType.If);
            }
            
            foreach (var trait in _traitLists.SelectMany(traitList => traitList.Value.GetTraits()))
            {
                trait.CheckIfFulfillsQuery(ref query, QueryType.While);
            }

            return query.Result;
        }
        
    }
}