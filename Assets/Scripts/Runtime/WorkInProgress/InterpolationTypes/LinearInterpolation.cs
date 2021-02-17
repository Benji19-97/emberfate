namespace Runtime.WorkInProgress
{
    public class LinearInterpolation : IInterpolationType
    {
        public float GetInterpolatedValue(float roll, float min, float max)
        {
            return (max - min) * roll + min;
        }
    }
}