// using Steamworks;
//
// namespace Runtime.WorkInProgress
// {
//     public abstract class TraitValue
//     {
//         public QueryPriority QueryPriority { get; private set; }
//         private InstantiatedTrait _instantiatedTrait;
//
//
//         protected TraitValue(QueryPriority queryPriority, InstantiatedTrait instantiatedTrait)
//         {
//             QueryPriority = queryPriority;
//             _instantiatedTrait = instantiatedTrait;
//         }
//
//         public abstract void ApplyValue(ref Query query, float roll = 1f, int power = 1);
//     }
// }