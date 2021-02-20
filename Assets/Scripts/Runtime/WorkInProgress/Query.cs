// namespace Runtime.WorkInProgress
// {
//     public class Query
//     {
//         public TraitCategory[] Categories;
//         
//         /// <summary>Type of trait the query looks for.</summary>
//         public TraitOperator TraitOperator;
//
//         /// <summary>Tags which the traits' tags match against.</summary>
//         public TraitTag[] Tags;
//
//         /// <summary>Tags the trait must have.</summary>
//         public TraitTag[] MustHaveTags;
//
//         /// <summary>Result of the query, modified by the traits.</summary>
//         public float Result = 0f;
//         
//         public static float GetTotalValue(Actor actor, TraitTag[] tags, TraitTag[] mustHaveTags)
//         {
//             var queryFlat = new Query()
//             {
//                 MustHaveTags = mustHaveTags,
//                 Tags = tags,
//                 TraitOperator = TraitOperator.AddsRemoves
//             };
//             var queryFlatResult = actor.TraitHolder.QueryTotalValue(ref queryFlat);
//
//             var queryIncrease = new Query()
//             {
//                 MustHaveTags = mustHaveTags,
//                 Tags = tags,
//                 TraitOperator = TraitOperator.IncreasesReduces
//             };
//             var queryIncreaseResult = actor.TraitHolder.QueryTotalValue(ref queryIncrease);
//
//             var queryMore = new Query()
//             {
//                 MustHaveTags = mustHaveTags,
//                 Tags = tags,
//                 TraitOperator = TraitOperator.MoreLess
//             };
//             var queryMoreResult = actor.TraitHolder.QueryTotalValue(ref queryMore);
//
//             return queryFlatResult * (1 + queryIncreaseResult) * (1 + queryMoreResult);
//         }
//     }
// }