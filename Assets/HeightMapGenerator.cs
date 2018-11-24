using UnityEngine;

public static class HeightMapGenerator  {

    public static HeightMap GenerateHeightMap(MapSetting setting, Vector2 center)
    {
        int width = setting.numVertsPerLine;
        int height = setting.numVertsPerLine;
        float[,] values = Noise.GenrateNoiseMap(width, height, center, setting);
        AnimationCurve curveThreadsafe = new AnimationCurve(setting.heightCurve.keys);

        float minValue = float.MinValue;
        float maxValue = float.MaxValue;

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                values[i, j] *= curveThreadsafe.Evaluate(values[i, j]) * setting.heightMultiplier;
                maxValue = Mathf.Max(maxValue, values[i, j]);
                minValue = Mathf.Min(minValue, values[i, j]);
            }
        }
        return new HeightMap(values, minValue, maxValue);
    }
}
public struct HeightMap
{
    public readonly float[,] values;
    public readonly float minValue;
    public readonly float maxValue;

    public HeightMap(float[,] values, float minValue, float maxValue)
    {
        this.values = values;
        this.minValue = minValue;
        this.maxValue = maxValue;
    }
}