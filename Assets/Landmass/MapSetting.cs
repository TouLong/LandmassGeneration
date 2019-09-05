using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "MapSetting", menuName = "Map Setting")]
public class MapSetting : ScriptableObject
{
    [Range(1, 10)]
    public int mapDimension = 1;
    [Range(10, 250)]
    public int chunkMesh = 100;
    [Range(5, 50)]
    public int mapHeight = 25;
    [Range(1, 20)]
    public int mapScale = 1;
    [Range(25, 250)]
    public int noiseScale = 25;
    [Range(1, 5)]
    public int octaves = 3;
    [Range(0, 1)]
    public float persistance = 0.25f;
    [Range(1, 5)]
    public float lacunarity = 2.5f;
    public int seed;
    public Vector2 offset;
    public AnimationCurve heightCurve = AnimationCurve.Linear(0, 0, 1, 1);
    public int waterLayer = -1;
    public Material mapMaterial;
    public Material waterMaterial;
    public List<Layer> layers = new List<Layer>();
    public List<ObjectDistribution> objectsDistribution = new List<ObjectDistribution>();
    public int ChunkVertices => chunkMesh + 3;
    public int MapSideMesh => chunkMesh * mapDimension;
    public int MapSideLength => chunkMesh * mapDimension * mapScale;
    public class Layer
    {
        public Color color;
        public float height;
        public float blendStrength;
        public Layer(float height)
        {
            this.height = height;
        }
    }
    public class ObjectDistribution
    {
        public class Distribution
        {
            public Vector2 region;
            public float radius = 100;
            public Distribution()
            {
            }
        }
        public string groupName;
        public List<GameObject> objects = new List<GameObject>();
        public List<Distribution> distributions = new List<Distribution>();
        public ObjectDistribution()
        {
        }
    }
}
