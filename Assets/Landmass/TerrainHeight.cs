using UnityEngine;

public static class NoiseHeight
{
    public static HeightData Generate(MapSetting setting, Vector2 sampleCentre)
    {
        int size = setting.ChunkVertices;
        float[,] noise = new float[size, size];

        System.Random prng = new System.Random(setting.seed);
        Vector2[] octaveOffsets = new Vector2[setting.octaves];

        float maxPossibleHeight = 0;
        float amplitude = 1;
        float frequency = 1;

        for (int i = 0; i < setting.octaves; i++)
        {
            float offsetX = prng.Next(-100000, 100000) + setting.offset.x + sampleCentre.x;
            float offsetY = prng.Next(-100000, 100000) + setting.offset.y + sampleCentre.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);

            maxPossibleHeight += amplitude;
            amplitude *= setting.persistance;
        }
        float noiseMin = float.MaxValue;
        float noiseMax = float.MinValue;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                amplitude = 1;
                frequency = 1;
                float noiseHeight = 0;

                for (int i = 0; i < setting.octaves; i++)
                {
                    float sampleX = (x + octaveOffsets[i].x) / setting.noiseScale * frequency;
                    float sampleY = (y + octaveOffsets[i].y) / setting.noiseScale * frequency;

                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY);
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= setting.persistance;
                    frequency *= setting.lacunarity;
                }

                noise[x, y] = noiseHeight / maxPossibleHeight;
                noiseMin = Mathf.Min(noise[x, y], noiseMin);
                noiseMax = Mathf.Max(noise[x, y], noiseMax);
            }
        }
        return new HeightData(noise, noiseMin, noiseMax);
    }
}

public static class MapHeight
{
    public static HeightData Generate(MapSetting setting, Vector2 range, float[,] noise)
    {
        int size = setting.ChunkVertices;
        float[,] map = new float[size, size];

        float mapMin = float.MaxValue;
        float mapMax = float.MinValue;
        AnimationCurve heightCurve = new AnimationCurve(setting.heightCurve.keys);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                map[x, y] = heightCurve.Evaluate(Mathf.InverseLerp(range.x, range.y, noise[x, y])) * setting.mapHeight * setting.mapScale;
                mapMin = Mathf.Min(map[x, y], mapMin);
                mapMax = Mathf.Max(map[x, y], mapMax);
            }
        }
        return new HeightData(map, mapMin, mapMax);
    }
}

public class HeightData
{
    public float maxValue;
    public float minValue;
    public float[,] values;

    public HeightData(float[,] values, float minValue, float maxValue)
    {
        this.values = values;
        this.maxValue = maxValue;
        this.minValue = minValue;
    }
}
