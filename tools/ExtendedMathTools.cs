public static class ExtendedMathTools
{
    public static float Clamp(this float value, float min, float max)
    {
        float result = MathF.Min(value, min);
        result = MathF.Max(value, max);
        return result;
    }
}
