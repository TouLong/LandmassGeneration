using System.Collections.Generic;
using UnityEngine;
using System.Linq;
[ExecuteInEditMode]
public class TerrainGenerator : MonoBehaviour
{
    [HideInInspector] public List<TerrainChunk> terrainChunkList = new List<TerrainChunk>();
    [HideInInspector] public float noisePeakMin = float.MinValue;
    [HideInInspector] public float noisePeakMax = float.MaxValue;
    [HideInInspector] public float mapPeakMin = float.MinValue;
    [HideInInspector] public float mapPeakMax = float.MaxValue;
    [HideInInspector] public float[] distributionRatio;
    [HideInInspector] public Texture2D texture;
    public MapSetting setting;

    [HideInInspector] public GameObject terrain;
    [HideInInspector] public GameObject mapObject;
    public bool autoUpdateMaterial;
    public bool generateMapObject;
    void Update()
    {
        if (autoUpdateMaterial && setting != null)
            UpdateMaterial();
    }
    public void Generate()
    {
        Clear();
        GenerateChunks();
        texture = MapImage.Generate(setting, terrainChunkList.Select(x => x.mapHeight).ToList());
        if (setting.waterLayer >= 0)
            GenerateWater();
        UpdateMaterial();
        if (generateMapObject)
            GenerateMapObject();
    }
    public void RandomGenerate()
    {
        setting.seed = Random.Range(-1000, 1000);
        Generate();
    }
    public void GenerateChunks()
    {
        terrain = new GameObject("Terrain");
        terrain.transform.parent = transform;
        int meshSize = setting.chunkMesh;
        for (int y = 0; y < setting.mapDimension; y++)
        {
            for (int x = 0; x < setting.mapDimension; x++)
            {
                TerrainChunk newChunk = new TerrainChunk(new Vector2(x * meshSize, y * meshSize), setting, terrain.transform);
                terrainChunkList.Add(newChunk);
                newChunk.ComputeNoise();
                noisePeakMin = Mathf.Min(noisePeakMin, newChunk.noiseHeight.minValue);
                noisePeakMax = Mathf.Max(noisePeakMax, newChunk.noiseHeight.maxValue);
            }
        }
        distributionRatio = new float[setting.layers.Count];
        foreach (TerrainChunk chunk in terrainChunkList)
        {
            chunk.Create(new Vector2(noisePeakMin, noisePeakMax));
            mapPeakMin = Mathf.Min(mapPeakMin, chunk.mapHeight.minValue);
            mapPeakMax = Mathf.Max(mapPeakMax, chunk.mapHeight.maxValue);
        }
        ComputeRatio();
    }
    public void GenerateWater()
    {
        GameObject waterGO = GameObject.CreatePrimitive(PrimitiveType.Plane);
        waterGO.name = "Water";
        waterGO.transform.parent = transform;
        waterGO.transform.localScale = new Vector3(setting.MapSideLength / 10f, 1, setting.MapSideLength / 10f);
        float waterHeight = setting.layers[Mathf.Min(setting.waterLayer, setting.layers.Count - 1)].height * mapPeakMax;
        waterGO.transform.localPosition = new Vector3(setting.MapSideLength / 2f, waterHeight, setting.MapSideLength / 2f);
        Renderer waterRender = waterGO.transform.GetComponent<Renderer>();
        if (setting.waterMaterial != null)
            waterRender.sharedMaterial = setting.waterMaterial;
        Collider waterCollider = waterGO.transform.GetComponent<Collider>();
        DestroyImmediate(waterCollider);
    }
    public void GenerateMapObject()
    {
        if (mapObject != null)
            DestroyImmediate(mapObject);
        mapObject = new GameObject("Map Object");
        mapObject.transform.parent = transform;
        mapObject.transform.localPosition = terrain.transform.localPosition;
        float regionX = setting.MapSideLength + mapObject.transform.localPosition.x;
        float regionY = setting.MapSideLength + mapObject.transform.localPosition.z;
        Vector2 regionSize = new Vector2(regionX, regionY);
        MapObjectGenerator.Generate(regionSize, mapPeakMax, mapObject.transform, setting.objectsDistribution);
    }
    public void Clear()
    {
        terrainChunkList.Clear();
        while (transform.childCount != 0)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }
    }
    public void UpdateMaterial()
    {
        if (setting.mapMaterial != null && terrain != null && setting.layers.Count > 0)
        {
            setting.mapMaterial.SetInt("layerCount", setting.layers.Count);
            setting.mapMaterial.SetColorArray("baseColors", setting.layers.Select(x => x.color).ToArray());
            setting.mapMaterial.SetFloatArray("baseStartHeights", setting.layers.Select(x => x.height).ToArray());
            setting.mapMaterial.SetFloatArray("baseBlends", setting.layers.Select(x => x.blendStrength).ToArray());
            setting.mapMaterial.SetFloat("minHeight", mapPeakMin + terrain.transform.position.y);
            setting.mapMaterial.SetFloat("maxHeight", mapPeakMax + terrain.transform.position.y);
        }
    }
    public void ComputeRatio()
    {
        foreach (TerrainChunk chunk in terrainChunkList)
        {
            IEnumerable<float> heights = chunk.noiseHeight.values.Cast<float>();
            List<float> layerHeights = setting.layers.Select(a => a.height).ToList();
            for (int i = 0; i < layerHeights.Count; ++i)
            {
                if (i != layerHeights.Count - 1)
                    distributionRatio[i] += heights.Count(a => a > layerHeights[i] && a < layerHeights[i + 1]);
                else
                    distributionRatio[i] += heights.Count(a => a > layerHeights[i] && a < noisePeakMax);
            }
        }
        for (int i = 0; i < distributionRatio.Length; ++i)
        {
            distributionRatio[i] /= setting.MapSideMesh * setting.MapSideMesh;
        }
    }
    //void OnValidate()
    //{
    //    UpdateMaterial();
    //}
}