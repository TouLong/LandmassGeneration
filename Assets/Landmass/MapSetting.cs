using UnityEngine;
using System.Collections.Generic;

public class MapSetting : MonoBehaviour
{
    [Range(1, 10)]
    public int mapDimension = 1;
    [Range(10, 250)]
    public int chunkMesh = 1;
    [Range(5, 50)]
    public int mapHeight = 25;
    [Range(1, 20)]
    public int mapScale = 1;
    [Range(25, 250)]
    public int noiseScale = 25;
    [Range(1, 10)]
    public int octaves = 1;
    [Range(0, 1)]
    public float persistance = 0.25f;
    [Range(1, 5)]
    public float lacunarity = 2.5f;
    public int seed;
    public Vector2 offset;


    public int ChunkVertices => chunkMesh + 3;
    public int MapSideMesh => chunkMesh * mapDimension;
    public int MapSideLength => chunkMesh * mapDimension * mapScale;

    public List<MapObject> mapObjects = new List<MapObject>();
    [HideInInspector] public AnimationCurve heightCurve;
    [HideInInspector] public List<Layer> layers = new List<Layer>();
    [System.Serializable]
    public class Layer
    {
        public Color color;
        public float height;
        public float blendStrength;
        //public Texture2D texture;
        //public float textureScale;
        public Layer()
        {
        }
        public Layer(float height)
        {
            this.height = height;
        }
    }

    [System.Serializable]
    public struct MapObject
    {
        public GameObject[] objects;
        public MapObjectDistribute[] distribute;
    }
    [System.Serializable]
    public struct MapObjectDistribute
    {
        public Vector2 range;
        public float radius;
    }
}
