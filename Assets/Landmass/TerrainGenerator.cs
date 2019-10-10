using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.AI;
[ExecuteInEditMode]
public class TerrainGenerator : MonoBehaviour
{
    public MapSetting setting;
    public bool autoUpdateMaterial;
    [HideInInspector] public List<TerrainChunk> chunkList;
    [HideInInspector] public float noisePeakMin;
    [HideInInspector] public float noisePeakMax;
    [HideInInspector] public float mapPeakMin;
    [HideInInspector] public float mapPeakMax;
    [HideInInspector] public Material terrainMaterial;
    [HideInInspector] public Material lakeMaterial;
    public GameObject terrain;
    public GameObject objects;
    public GameObject lake;
    void Update()
    {
        if (autoUpdateMaterial && setting != null)
            UpdateMaterial();
    }
    public void Generate()
    {
        ClearChunks();
        ClearNav();
        ClearMapObject();
        GenerateChunks();
        GenerateLake();
        GenerateMapObject();
        GenerateNavMesh();
        UpdateMaterial();
    }
    public void GenerateChunks()
    {
        terrain = new GameObject("Terrain");
        terrain.transform.parent = transform;
        chunkList = new List<TerrainChunk>();
        int meshSize = setting.chunkMesh;
        noisePeakMin = float.MaxValue;
        noisePeakMax = float.MinValue;
        mapPeakMin = float.MaxValue;
        mapPeakMax = float.MinValue;
        for (int y = 0; y < setting.mapDimension; y++)
        {
            for (int x = 0; x < setting.mapDimension; x++)
            {
                TerrainChunk newChunk = new TerrainChunk(new Vector2(x * meshSize, y * meshSize), setting, terrain.transform, terrainMaterial);
                chunkList.Add(newChunk);
                newChunk.ComputeNoise();
                noisePeakMin = Mathf.Min(noisePeakMin, newChunk.noiseHeight.minValue);
                noisePeakMax = Mathf.Max(noisePeakMax, newChunk.noiseHeight.maxValue);
            }
        }
        foreach (TerrainChunk chunk in chunkList)
        {
            chunk.Create(new Vector2(noisePeakMin, noisePeakMax));
            mapPeakMin = Mathf.Min(mapPeakMin, chunk.mapHeight.minValue);
            mapPeakMax = Mathf.Max(mapPeakMax, chunk.mapHeight.maxValue);
        }
    }
    public void GenerateLake()
    {
        if (lakeMaterial == null || setting.lakeLayer == 0) return;
        lake = GameObject.CreatePrimitive(PrimitiveType.Plane);
        lake.name = "Lake";
        lake.transform.parent = transform;
        lake.transform.localScale = new Vector3(setting.MapSideLength / 10f, 1, setting.MapSideLength / 10f);
        float height = setting.layers[Mathf.Min(setting.lakeLayer, setting.layers.Count - 1)].height * mapPeakMax;
        lake.transform.localPosition = new Vector3(setting.MapSideLength / 2f, height, setting.MapSideLength / 2f);
        lake.transform.GetComponent<Renderer>().sharedMaterial = lakeMaterial;
        DestroyImmediate(lake.transform.GetComponent<Collider>());
    }
    public void GenerateMapObject()
    {
        objects = new GameObject("Objects");
        objects.transform.parent = transform;
        objects.transform.localPosition = terrain.transform.localPosition;
        float regionX = setting.MapSideLength + objects.transform.localPosition.x;
        float regionY = setting.MapSideLength + objects.transform.localPosition.z;
        Vector2 regionSize = new Vector2(regionX, regionY);
        MapObjectGenerator.Generate(regionSize, mapPeakMax, objects.transform, setting.objectsDistribution);
    }
    public void ClearChunks()
    {
        if (chunkList != null)
            chunkList.Clear();
        DestroyImmediate(terrain);
    }
    public void ClearMapObject()
    {
        DestroyImmediate(objects);
    }
    public void ClearNav()
    {
        NavMesh.RemoveAllNavMeshData();
    }
    public void CreateMaterial()
    {
        terrainMaterial = new Material(Shader.Find("Custom/Terrain"));
    }
    public void UpdateMaterial()
    {
        if (terrain != null && setting.layers.Count > 0)
        {
            if (terrainMaterial == null)
                CreateMaterial();
            terrainMaterial.SetInt("layerCount", setting.layers.Count);
            terrainMaterial.SetColorArray("baseColors", setting.layers.Select(x => x.color).ToArray());
            terrainMaterial.SetFloatArray("baseStartHeights", setting.layers.Select(x => x.height).ToArray());
            terrainMaterial.SetFloatArray("baseBlends", setting.layers.Select(x => x.blendStrength).ToArray());
            terrainMaterial.SetFloat("minHeight", mapPeakMin + terrain.transform.position.y);
            terrainMaterial.SetFloat("maxHeight", mapPeakMax + terrain.transform.position.y);
        }
    }
    public void GenerateNavMesh()
    {
        Bounds bunds = new Bounds()
        {
            min = new Vector3(0, -0.1f, 0),
        };
        if (setting.mountainLayer > -1)
            bunds.max = new Vector3(setting.MapSideLength, setting.layers[setting.mountainLayer].height * mapPeakMax, setting.MapSideLength);
        else
            bunds.max = new Vector3(setting.MapSideLength, mapPeakMax, setting.MapSideLength);
        List<NavMeshBuildSource> meshBuildSources = chunkList.Select(a => a.navMesh).ToList();
        NavMeshBuildSettings settings = NavMesh.GetSettingsByIndex(0);
        NavMesh.AddNavMeshData(NavMeshBuilder.BuildNavMeshData(settings, meshBuildSources, bunds, bunds.min, Quaternion.identity));
    }
}