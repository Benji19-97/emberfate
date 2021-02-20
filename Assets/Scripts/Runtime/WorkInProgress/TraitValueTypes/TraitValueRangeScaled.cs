// namespace Runtime.WorkInProgress
// {
//     public class TraitValueRangeScaled : TraitValue
//     {
//         private const int MAXPowerLevel = 60;
//
//         private readonly float _valueAtMinimumPower;
//         private readonly float _valueAtMaximumPower;
//         private readonly float _variance;
//         private readonly IInterpolationType _interpolationType;
//         
//         public TraitValueRangeScaled(QueryPriority queryPriority, InstantiatedTrait instantiatedTrait, float valueAtMinimumPower, float valueAtMaximumPower, float variance, IInterpolationType interpolationType) : base(queryPriority, instantiatedTrait)
//         {
//             _valueAtMinimumPower = valueAtMinimumPower;
//             _valueAtMaximumPower = valueAtMaximumPower;
//             _variance = variance;
//             _interpolationType = interpolationType;
//         }
//         
//         public override void ApplyValue(ref Query query, float roll = 1, int power = 1)
//         {
//             var powerLevelPercent = power / (float) MAXPowerLevel;
//             var value = (_valueAtMaximumPower - _valueAtMinimumPower) * powerLevelPercent + _valueAtMinimumPower;
//             query.Result += _interpolationType.GetInterpolatedValue(roll, value * (1f - _variance/2f), value * (1f + _variance/2f));
//         }
//     }
// }