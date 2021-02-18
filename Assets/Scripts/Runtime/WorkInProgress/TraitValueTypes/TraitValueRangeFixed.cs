namespace Runtime.WorkInProgress
{
    public class TraitValueRangeFixed : ITraitValue
    {
        private readonly float _value;
        private readonly float _variance;
        private readonly IInterpolationType _interpolationType;

        public TraitValueRangeFixed(float value, float variance, IInterpolationType interpolationType)
        {
            _value = value;
            _variance = variance;
            _interpolationType = interpolationType;
        }

        public QueryPriority queryPriority => QueryPriority.Basic; 
        public void ApplyValue(ref Query query, float roll = 1, int power = 1)
        {
            query.Result += _interpolationType.GetInterpolatedValue(roll, _value * (1f - _variance/2f), _value * (1f + _variance/2f));
        }
    }
}