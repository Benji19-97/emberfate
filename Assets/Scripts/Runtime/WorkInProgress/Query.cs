namespace Runtime.WorkInProgress
{
    public class Query
    {
        /// <summary>Actor that executes this query.</summary>
        public Actor Actor;
        
        /// <summary>Type of trait the query looks for.</summary>
        public TraitType TraitType;

        /// <summary>Tags which the traits' tags match against.</summary>
        public TraitTag[] Tags;

        /// <summary>Tags the trait must have.</summary>
        public TraitTag[] MustHaveTags;

        /// <summary>Result of the query, modified by the traits.</summary>
        public float Result;
    }
}