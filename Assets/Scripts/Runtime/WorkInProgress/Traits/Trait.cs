using System;
using System.Linq;

namespace Runtime.WorkInProgress
{
    public abstract class Trait
    {
        public readonly TraitCategory Category;
        public readonly ITraitValue Value;

        private TraitOrigin _origin;
        private readonly TraitTag[] _tags;
        private readonly TraitOperation _operation;
        private EffectHandler _effectHandler;

        protected Trait(TraitTag[] tags, TraitOperation operation, ITraitValue value, TraitCategory category, TraitOrigin origin)
        {
            _tags = tags;
            _operation = operation;
            Value = value;
            Category = category;
            _origin = origin;
        }

        public virtual void CheckIfFulfillsQuery(ref Query query)
        {
            if (query.TraitOperation != _operation) return;

            if (query.MustHaveTags != null && _tags.Intersect(query.MustHaveTags).Count() != query.MustHaveTags.Length) return;

            var tags = query.Tags ?? Array.Empty<TraitTag>();
            var mustHaveTags = query.MustHaveTags ?? Array.Empty<TraitTag>();
            var allTags = tags.Concat(mustHaveTags);
            
            if (_tags.Intersect(allTags).Count() != _tags.Length) return;

            Value.ApplyValue(ref query);
        }
    }
}