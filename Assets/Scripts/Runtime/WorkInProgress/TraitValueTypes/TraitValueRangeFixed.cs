// namespace Runtime.WorkInProgress
// {
//     public class TraitValueRangeFixed : TraitValue
//     {
//         private readonly float _value;
//         private readonly float _variance;
//         private readonly IInterpolationType _interpolationType;
//
//         public TraitValueRangeFixed(QueryPriority queryPriority, InstantiatedTrait instantiatedTrait, float value, float variance, IInterpolationType interpolationType) : base(queryPriority, instantiatedTrait)
//         {
//             _value = value;
//             _variance = variance;
//             _interpolationType = interpolationType;
//         }
//
//         public override void ApplyValue(ref Query query, float roll = 1, int power = 1)
//         {
//             query.Result += _interpolationType.GetInterpolatedValue(roll, _value * (1f - _variance/2f), _value * (1f + _variance/2f));
//         }
//     }
// }