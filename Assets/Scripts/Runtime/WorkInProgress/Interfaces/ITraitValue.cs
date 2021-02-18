using Steamworks;

namespace Runtime.WorkInProgress
{
    public interface ITraitValue
    {
        QueryPriority queryPriority
        {
            get;
        }
        
        void ApplyValue(ref Query query, float roll = 1f, int power = 1);
    }
}