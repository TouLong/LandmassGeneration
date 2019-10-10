using UnityEngine;
using System.Collections.Generic;
using System.Linq;
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
    public AnimationCurve heightCurve;
    public int lakeLayer = -1;
    public int mountainLayer = -1;
    public List<Layer> layers;
    public List<ObjectDistribution> objectsDistribution;
    public int ChunkVertices => chunkMesh + 3;
    public int MapSideMesh => chunkMesh * mapDimension;
    public int MapSideLength => chunkMesh * mapDimension * mapScale;
    public void CopyTo(ref MapSetting setting)
    {
        setting.mapDimension = mapDimension;
        setting.chunkMesh = chunkMesh;
        setting.mapHeight = mapHeight;
        setting.mapScale = mapScale;
        setting.noiseScale = noiseScale;
        setting.octaves = octaves;
        setting.persistance = persistance;
        setting.lacunarity = lacunarity;
        setting.seed = seed;
        setting.offset = offset;
        setting.heightCurve = new AnimationCurve(heightCurve.keys);
        setting.lakeLayer = lakeLayer;
        setting.mountainLayer = mountainLayer;
        setting.layers = layers.Select(a => a.Clone()).ToList();
        setting.objectsDistribution = objectsDistribution.Select(a => a.Clone()).ToList();
    }
    [System.Serializable]
    public class Layer
    {
        public Color color;
        public float height;
        public float blendStrength;
        public Layer(float height)
        {
            this.height = height;
        }
        public Layer(Color color, float height, float blendStrength)
        {
            this.color = color;
            this.height = height;
            this.blendStrength = blendStrength;
        }
        public Layer Clone()
        {
            return new Layer(color, height, blendStrength);
        }
    }
    [System.Serializable]
    public class ObjectDistribution
    {
        [System.Serializable]
        public class Distribution
        {
            public Vector2 region;
            public float radius = 100;
            public Distribution()
            {
            }
            public Distribution(Vector2 region, float radius)
            {
                this.region = region;
                this.radius = radius;
            }
            public Distribution Clone()
            {
                return new Distribution(region, radius);
            }
        }
        public string groupName;
        public List<GameObject> objects = new List<GameObject>();
        public List<Distribution> distributions = new List<Distribution>();
        public ObjectDistribution()
        {
        }
        public ObjectDistribution Clone()
        {
            ObjectDistribution objectDistribution = new ObjectDistribution();
            objectDistribution.groupName = groupName;
            objectDistribution.objects = objects;
            objectDistribution.distributions = distributions.Select(a => a.Clone()).ToList();
            return objectDistribution;
        }
    }
}
