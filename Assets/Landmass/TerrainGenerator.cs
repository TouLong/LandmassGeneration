using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.AI;
[ExecuteInEditMode]
public class TerrainGenerator : MonoBehaviour
{
    public MapSetting setting;
    public bool autoUpdateMaterial;
    public bool generateMapObject;
    [HideInInspector] public List<TerrainChunk> chunkList = new List<TerrainChunk>();
    [HideInInspector] public float noisePeakMin = float.MinValue;
    [HideInInspector] public float noisePeakMax = float.MaxValue;
    [HideInInspector] public float mapPeakMin = float.MinValue;
    [HideInInspector] public float mapPeakMax = float.MaxValue;
    [HideInInspector] public float[] distributionRatio;
    [HideInInspector] public GameObject terrain;
    [HideInInspector] public GameObject mapObject;
    Bounds bounds;

    void Update()
    {
        if (autoUpdateMaterial && setting != null)
            UpdateMaterial();
    }
    public void Generate()
    {
        Clear();
        GenerateChunks();
        if (setting.waterLayer >= 0)
            GenerateWater();
        UpdateMaterial();
        if (generateMapObject)
            GenerateMapObject();
        GenerateNavMesh();
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
        chunkList.Clear();
        NavMesh.RemoveAllNavMeshData();
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
        distributionRatio = new float[setting.layers.Count];
        foreach (TerrainChunk chunk in chunkList)
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
    public void GenerateNavMesh()
    {
        bounds = new Bounds()
        {
            min = Vector3.zero,
        };
        if (setting.mountainLayer > -1)
            bounds.max = new Vector3(setting.MapSideLength, setting.layers[setting.mountainLayer].height * mapPeakMax, setting.MapSideLength);
        else
            bounds.max = new Vector3(setting.MapSideLength, mapPeakMax, setting.MapSideLength);
        List<NavMeshBuildSource> meshBuildSources = chunkList.Select(a => a.navMesh).ToList();
        List<MeshFilter> mfs = mapObject.GetComponentsInChildren<MeshFilter>().ToList();
        foreach (MeshFilter mf in mfs)
        {
            meshBuildSources.Add(new NavMeshBuildSource()
            {
                shape = NavMeshBuildSourceShape.Mesh,
                sourceObject = mf.sharedMesh,
                transform = mf.transform.localToWorldMatrix,
            });
        }
        NavMeshBuildSettings settings = NavMesh.GetSettingsByIndex(0);
        NavMesh.AddNavMeshData(NavMeshBuilder.BuildNavMeshData(settings, meshBuildSources, bounds, bounds.min, Quaternion.identity));
    }
    void OnDrawGizmosSelected()
    {
        Bounds gizmosBounds = new Bounds()
        {
            min = Vector3.zero,
        };
        if (setting.layers.Count > 0 && setting.mountainLayer > -1)
            gizmosBounds.max = new Vector3(setting.MapSideLength, setting.layers[setting.mountainLayer].height * mapPeakMax, setting.MapSideLength);
        else
            gizmosBounds.max = new Vector3(setting.MapSideLength, mapPeakMax, setting.MapSideLength);

        Gizmos.color = Color.black;
        Gizmos.DrawWireCube(gizmosBounds.center, gizmosBounds.size);
    }
    //void OnValidate()
    //{
    //    UpdateMaterial();
    //}
}