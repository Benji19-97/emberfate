// namespace Runtime.WorkInProgress
// {
//     public class TraitValueFixed : TraitValue
//     {
//         private readonly float _value;
//
//         public TraitValueFixed(QueryPriority queryPriority, InstantiatedTrait instantiatedTrait, float value) : base(queryPriority, instantiatedTrait)
//         {
//             _value = value;
//         }
//
//         public override void ApplyValue(ref Query query, float roll = 1, int power = 1)
//         {
//             query.Result += _value;
//         }
//     }
// }