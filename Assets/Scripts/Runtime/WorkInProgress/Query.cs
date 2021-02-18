namespace Runtime.WorkInProgress
{
    public class Query
    {
        public TraitCategory[] Categories;
        
        /// <summary>Type of trait the query looks for.</summary>
        public TraitOperation TraitOperation;

        /// <summary>Tags which the traits' tags match against.</summary>
        public TraitTag[] Tags;

        /// <summary>Tags the trait must have.</summary>
        public TraitTag[] MustHaveTags;

        /// <summary>Result of the query, modified by the traits.</summary>
        public float Result = 0f;
    }
}