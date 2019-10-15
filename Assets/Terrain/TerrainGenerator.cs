using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor;

[ExecuteInEditMode]
public class TerrainGenerator : MonoBehaviour
{
    public MapSetting setting;
    [HideInInspector] public Material terrainMaterial;
    [HideInInspector] public Material lakeMaterial;
    public float[,] noise;
    readonly string terrainGroup = "Terrain";
    readonly string objectGroup = "Objects";
    Transform terrain;
    List<Transform> chunks;
    Transform objects;
    Transform lake;
    void Start()
    {
        TerrainNavMesh.Load();
    }
    void Update()
    {
        if (setting != null)
            UpdateMaterial();
    }
    bool GetTerrain()
    {
        terrain = transform.Find(terrainGroup);
        return terrain != null;
    }
    bool GetChunks()
    {
        if (GetTerrain())
        {
            chunks = new List<Transform>();
            for (int i = 0; i < terrain.childCount; i++)
                chunks.Add(terrain.GetChild(i));
            return true;
        }
        else
        {
            return false;
        }
    }
    bool GetObjects()
    {
        objects = transform.Find(objectGroup);
        return objects != null;
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
        terrain = new GameObject(terrainGroup).transform;
        terrain.transform.parent = transform;
        chunks = new List<Transform>();
        TerrainHeight.Noise(out noise,
            setting.MapSideVertices, setting.seed, setting.octaves, setting.persistance, setting.lacunarity, setting.noiseScale, setting.offset);
        for (int x = 0; x < setting.mapDimension; x++)
        {
            for (int y = 0; y < setting.mapDimension; y++)
            {
                GameObject chunk = new GameObject("Terrain Chunk");
                chunk.transform.parent = terrain.transform;
                chunk.transform.localPosition = new Vector3(x * setting.ChunkSize, 0, y * setting.ChunkSize);
                Vector2Int offset = new Vector2Int(x * setting.chunkMesh, y * setting.chunkMesh);
                TerrainHeight.Evaluate(out float[,] heights, ref noise,
                     offset, setting.ChunkVertices, setting.mapScale, setting.mapHeight, setting.heightCurve);
                Mesh mesh = MeshGenerator.Generate(heights, setting.mapScale);
                chunk.AddComponent<MeshFilter>().mesh = mesh;
                chunk.AddComponent<MeshCollider>().sharedMesh = mesh;
                chunk.AddComponent<MeshRenderer>().material = terrainMaterial;
                GameObjectUtility.SetStaticEditorFlags(chunk, StaticEditorFlags.NavigationStatic);
                chunks.Add(chunk.transform);
            }
        }
        EditorUtility.SetDirty(this);
    }
    public void GenerateLake()
    {
        if (lakeMaterial == null || setting.lakeLayer == 0) return;
        lake = GameObject.CreatePrimitive(PrimitiveType.Plane).transform;
        lake.name = "Lake";
        lake.transform.parent = transform;
        lake.transform.localScale = new Vector3(setting.MapSideLength / 10f, 1, setting.MapSideLength / 10f);
        float height = setting.layers[Mathf.Min(setting.lakeLayer, setting.layers.Count - 1)].height * setting.MapHeight;
        lake.transform.localPosition = new Vector3(setting.MapSideLength / 2f, height, setting.MapSideLength / 2f);
        lake.transform.GetComponent<Renderer>().sharedMaterial = lakeMaterial;
        DestroyImmediate(lake.transform.GetComponent<Collider>());
    }
    public void GenerateMapObject()
    {
        objects = new GameObject(objectGroup).transform;
        objects.transform.parent = transform;
        objects.transform.localPosition = terrain.transform.localPosition;
        float regionX = setting.MapSideLength + objects.transform.localPosition.x;
        float regionY = setting.MapSideLength + objects.transform.localPosition.z;
        Vector2 regionSize = new Vector2(regionX, regionY);
        MapObjectGenerator.Generate(regionSize, setting.MapHeight, objects.transform, setting.objectsDistribution);
    }
    public void ClearChunks()
    {
        if (GetTerrain())
            DestroyImmediate(terrain.gameObject);
    }
    public void ClearMapObject()
    {
        if (GetObjects())
            DestroyImmediate(objects.gameObject);
    }
    public void CreateMaterial()
    {
        terrainMaterial = new Material(Shader.Find("Custom/Terrain"));
    }
    public void UpdateMaterial()
    {
        if (GetTerrain() && setting.layers.Count > 0)
        {
            if (terrainMaterial == null)
                CreateMaterial();
            terrainMaterial.SetInt("layerCount", setting.layers.Count);
            terrainMaterial.SetColorArray("baseColors", setting.layers.Select(x => x.color).ToArray());
            terrainMaterial.SetFloatArray("baseStartHeights", setting.layers.Select(x => x.height).ToArray());
            terrainMaterial.SetFloatArray("baseBlends", setting.layers.Select(x => x.blendStrength).ToArray());
            terrainMaterial.SetFloat("minHeight", terrain.position.y);
            terrainMaterial.SetFloat("maxHeight", setting.MapHeight + terrain.position.y);
        }
    }
    public void GenerateNavMesh()
    {
        if (GetChunks())
        {
            Bounds bounds = new Bounds()
            {
                min = new Vector3(0, -0.5f, 0),
            };
            if (setting.mountainLayer > -1)
                bounds.max = new Vector3(setting.MapSideLength, setting.layers[setting.mountainLayer].height * setting.MapHeight, setting.MapSideLength);
            else
                bounds.max = new Vector3(setting.MapSideLength, setting.MapHeight, setting.MapSideLength);
            TerrainNavMesh.Generate(bounds, chunks.Select(a => a.GetComponent<MeshFilter>()).ToList());
        }
    }
    public void ClearNav()
    {
        TerrainNavMesh.Clear();
    }
}