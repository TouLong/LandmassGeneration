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
    public GameObject terrain;
    List<GameObject> chunks;
    GameObject objects;
    GameObject lake;
    void Start()
    {
        TerrainNavMesh.Load();
    }
    void Update()
    {
        if (setting != null)
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
        chunks = new List<GameObject>();
        TerrainHeight.Noise(out noise,
            setting.MapSideVertices, setting.seed, setting.octaves, setting.persistance, setting.lacunarity, setting.noiseScale, setting.offset);
        for (int x = 0; x < setting.mapDimension; x++)
        {
            for (int y = 0; y < setting.mapDimension; y++)
            {
                GameObject meshObject = new GameObject("Terrain Chunk");
                meshObject.transform.parent = terrain.transform;
                meshObject.transform.localPosition = new Vector3(x * setting.ChunkSize, 0, y * setting.ChunkSize);
                Vector2Int offset = new Vector2Int(x * setting.chunkMesh, y * setting.chunkMesh);
                TerrainHeight.Evaluate(out float[,] heights, ref noise,
                     offset, setting.ChunkVertices, setting.mapScale, setting.mapHeight, setting.heightCurve);
                Mesh mesh = MeshGenerator.Generate(heights, setting.mapScale);
                meshObject.AddComponent<MeshFilter>().mesh = mesh;
                meshObject.AddComponent<MeshCollider>().sharedMesh = mesh;
                meshObject.AddComponent<MeshRenderer>().material = terrainMaterial;
                GameObjectUtility.SetStaticEditorFlags(meshObject, StaticEditorFlags.NavigationStatic);
                chunks.Add(meshObject);
            }
        }
        EditorUtility.SetDirty(this);
    }
    public void GenerateLake()
    {
        if (lakeMaterial == null || setting.lakeLayer == 0) return;
        lake = GameObject.CreatePrimitive(PrimitiveType.Plane);
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
        objects = new GameObject("Objects");
        objects.transform.parent = transform;
        objects.transform.localPosition = terrain.transform.localPosition;
        float regionX = setting.MapSideLength + objects.transform.localPosition.x;
        float regionY = setting.MapSideLength + objects.transform.localPosition.z;
        Vector2 regionSize = new Vector2(regionX, regionY);
        MapObjectGenerator.Generate(regionSize, setting.MapHeight, objects.transform, setting.objectsDistribution);
    }
    public void ClearChunks()
    {
        DestroyImmediate(terrain);
    }
    public void ClearMapObject()
    {
        DestroyImmediate(objects);
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
            terrainMaterial.SetFloat("minHeight", terrain.transform.position.y);
            terrainMaterial.SetFloat("maxHeight", setting.MapHeight + terrain.transform.position.y);
        }
    }
    public void GenerateNavMesh()
    {
        Bounds bounds = new Bounds()
        {
            min = new Vector3(0, -0.1f, 0),
        };
        if (setting.mountainLayer > -1)
            bounds.max = new Vector3(setting.MapSideLength, setting.layers[setting.mountainLayer].height * setting.MapHeight, setting.MapSideLength);
        else
            bounds.max = new Vector3(setting.MapSideLength, setting.MapHeight, setting.MapSideLength);
        TerrainNavMesh.Generate(bounds, chunks.Select(a => a.GetComponent<MeshFilter>()).ToList());
    }
    public void ClearNav()
    {
        TerrainNavMesh.Clear();
    }
}