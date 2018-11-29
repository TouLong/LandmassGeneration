using UnityEngine;

public class MapSetting : MonoBehaviour
{
    [Range(1, 10)]
    public int chunkSizeUnits25 = 1;
    [Range(1, 10)]
    public int numChunk = 1;
    [Range(25, 250)]
    public int noiseScale = 25;
    [Range(1, 5)]
    public int octaves = 1;
    [Range(0, 1)]
    public float persistance = 0.5f;
    [Range(1, 3)]
    public float lacunarity = 2;
    [Range(25, 250)]
    public int heightMultiplier = 25;
    public int seed;
    public Vector2 offset;

    [Range(1, 10)]
    public int meshScale = 1;
    public AnimationCurve heightCurve;
    public bool useFalloff;
    public Layer[] layers;

    public int numVertsPerLine
    {
        get
        {
            return 25 * chunkSizeUnits25 + 1;
        }
    }
    public int meshWorldSize
    {
        get { return (numVertsPerLine - 3) * meshScale; }
    }

    [System.Serializable]
    public class Layer
    {
        public Color color;
        [Range(0, 1)]
        public float heights;
        [Range(0, 1)]
        public float blendStrength;
        //public Texture2D texture;
        //public float textureScale;
    }
}