using System.Linq;

namespace Runtime.WorkInProgress
{
    public abstract class Trait
    {
        private readonly TraitTag[] _tags;
        private readonly TraitType _type;
        private readonly ITraitValue _value;

        protected Trait(TraitTag[] tags, TraitType type, ITraitValue value)
        {
            _tags = tags;
            _type = type;
            _value = value;
        }

        public virtual void CheckIfFulfillsQuery(ref Query query, QueryType queryType)
        {
            if (query.TraitType != _type) return;

            if (queryType != _value.QueryType) return;

            if (query.MustHaveTags != null && _tags.Intersect(query.MustHaveTags).Count() != query.MustHaveTags.Length) return;

            if (_tags.Intersect(query.Tags).Count() != _tags.Length) return;

            _value.ApplyValue(ref query);
        }
    }
}