namespace Runtime.WorkInProgress
{
    public class TraitValueFixed :ITraitValue
    {
        private readonly float _value;

        public TraitValueFixed(float value)
        {
            _value = value;
        }
        public QueryPriority queryPriority => QueryPriority.Basic;
        public void ApplyValue(ref Query query, float roll = 1, int power = 1)
        {
            query.Result += _value;
        }
    }
}