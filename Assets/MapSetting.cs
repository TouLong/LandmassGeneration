using UnityEngine;
using System.Collections.Generic;

public class MapSetting : MonoBehaviour
{
    [Range(1, 10)]
    public int mapDimension = 1;
    [Range(1, 25)]
    public int chunkSizeUnits10 = 1;
    [Range(25, 250)]
    public int noiseScale = 25;
    [Range(1, 5)]
    public int octaves = 1;
    [Range(0, 1)]
    public float persistance = 0.5f;
    [Range(1, 3)]
    public float lacunarity = 2;
    [Range(25, 250)]
    public int heightScale = 25;
    [Range(1, 10)]
    public int mapSize = 1;
    public int seed;
    public Vector2 offset;


    public int chunkVertexs
    {
        get
        {
            return 10 * chunkSizeUnits10 + 3;
        }
    }
    public int chunkSideLength
    {
        get { return (chunkVertexs - 3); }
    }
    public int mapSideLength
    {
        get { return (chunkVertexs - 3) * mapDimension; }
    }

    public AnimationCurve heightCurve;
    public List<Layer> layers = new List<Layer>();
    [System.Serializable]
    public class Layer
    {
        public Color color;
        [Range(0, 1)]
        public float height;
        [Range(0, 1)]
        public float blendStrength;
        public GameObject[] mapObject;
        //public Texture2D texture;
        //public float textureScale;
        public Layer()
        {
        }
        public Layer(float height)
        {
            this.height = height;
        }
        public Layer(Color color)
        {
            this.color = color;
        }
        public Layer(float height, Color color)
        {
            this.height = height;
            this.color = color;
        }
    }
}
