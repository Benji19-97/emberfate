namespace Runtime.WorkInProgress
{
    public interface IInterpolationType
    {
        float GetInterpolatedValue(float roll, float min, float max);
    }
}