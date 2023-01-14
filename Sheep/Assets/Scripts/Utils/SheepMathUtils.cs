
public static class SheepMathUtils
{
    public static float Map(float value, float iMin, float iMax, float oMin, float oMax)
    {
        return oMin + (oMax - oMin) * ((value - iMin) / (iMax - iMin));
    }
}
