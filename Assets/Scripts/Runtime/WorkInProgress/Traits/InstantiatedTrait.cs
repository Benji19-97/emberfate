using System;
using System.Linq;

namespace Runtime.WorkInProgress
{
    public abstract class InstantiatedTrait
    {
        public readonly TraitHolder TraitHolder;
        public readonly TraitCategory Category;
        public readonly TraitValue Value;
        public readonly EffectHandler EffectHandler;

        private TraitOrigin _origin;
        private readonly TraitTag[] _tags;
        private readonly TraitOperator _operator;

        protected InstantiatedTrait(TraitHolder traitHolder ,TraitTag[] tags, TraitOperator @operator, TraitValue value, TraitCategory category, TraitOrigin origin, EffectHandler effectHandler = null)
        {
            TraitHolder = traitHolder;
            Category = category;
            Value = value;
            EffectHandler = effectHandler;

            _tags = tags;
            _operator = @operator;
            _origin = origin;
        }

        public virtual void CheckIfFulfillsQuery(ref Query query)
        {
            if (query.TraitOperator != _operator) return;

            if (query.MustHaveTags != null && _tags.Intersect(query.MustHaveTags).Count() != query.MustHaveTags.Length) return;

            var tags = query.Tags ?? Array.Empty<TraitTag>();
            var mustHaveTags = query.MustHaveTags ?? Array.Empty<TraitTag>();
            var allTags = tags.Concat(mustHaveTags);
            
            if (_tags.Intersect(allTags).Count() != _tags.Length) return;

            Value.ApplyValue(ref query);
        }
    }
}