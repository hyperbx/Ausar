namespace Ausar.Helpers
{
    internal class MathHelper
    {
        public static float Lerp(float in_start, float in_end, float in_time)
        {
            return in_start + (in_end - in_start) * in_time;
        }
    }
}
