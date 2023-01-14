public static class FloatExtensions
{
    public static bool Between(this float value, float min, float max)
    {
        return value >= min && value <= max;
    }
}
