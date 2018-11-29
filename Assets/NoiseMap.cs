using UnityEngine;

public static class NoiseMap
{
    public static NoiseMapHeight Generate(MapSetting setting, Vector2 sampleCentre)
    {
        int size = setting.numVertsPerLine;
        float[,] noise = new float[size, size];
        float[,] map = new float[size, size];

        System.Random prng = new System.Random(setting.seed);
        Vector2[] octaveOffsets = new Vector2[setting.octaves];
        AnimationCurve heightCurve = new AnimationCurve(setting.heightCurve.keys);

        float maxPossibleHeight = 0;
        float amplitude = 1;
        float frequency = 1;

        for (int i = 0; i < setting.octaves; i++)
        {
            float offsetX = prng.Next(-100000, 100000) + setting.offset.x + sampleCentre.x;
            float offsetY = prng.Next(-100000, 100000) - setting.offset.y - sampleCentre.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);

            maxPossibleHeight += amplitude;
            amplitude *= setting.persistance;
        }
        float noiseMin = float.MaxValue;
        float noiseMax = float.MinValue;
        float mapMin = float.MaxValue;
        float mapMax = float.MinValue;
        float half = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {

                amplitude = 1;
                frequency = 1;
                float noiseHeight = 0;

                for (int i = 0; i < setting.octaves; i++)
                {
                    float sampleX = (x - half + octaveOffsets[i].x) / setting.noiseScale * frequency;
                    float sampleY = (y - half + octaveOffsets[i].y) / setting.noiseScale * frequency;

                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY);
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= setting.persistance;
                    frequency *= setting.lacunarity;
                }

                noise[x, y] = noiseHeight / maxPossibleHeight;
                map[x, y] = noise[x, y] * heightCurve.Evaluate(noise[x, y]) * setting.heightMultiplier;
                noiseMin = Mathf.Min(noise[x, y], noiseMin);
                noiseMax = Mathf.Max(noise[x, y], noiseMax);
                mapMin = Mathf.Min(map[x, y], mapMin);
                mapMax = Mathf.Max(map[x, y], mapMax);
            }
        }

        NoiseMapHeight noiseMapHeight;
        noiseMapHeight.noise = new Height(noise, noiseMin, noiseMax);
        noiseMapHeight.map = new Height(map, mapMin, mapMax);
        return noiseMapHeight;
    }
}

public class Height
{
    public float maxValue;
    public float minValue;
    public float[,] values;
    public Height(float[,] values, float minValue, float maxValue)
    {
        this.values = values;
        this.maxValue = maxValue;
        this.minValue = minValue;
    }
}

public struct NoiseMapHeight
{
    public Height noise;
    public Height map;
}