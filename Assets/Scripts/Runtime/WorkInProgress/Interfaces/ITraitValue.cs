using Steamworks;

namespace Runtime.WorkInProgress
{
    public interface ITraitValue
    {
        QueryType QueryType
        {
            get;
        }
        
        void ApplyValue(ref Query query, float roll = 1f, int power = 1);
    }
}