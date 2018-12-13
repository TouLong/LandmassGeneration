using UnityEngine;

public class TerrainChunk
{
    GameObject meshObject;
    MeshData meshData;
    MeshRenderer meshRenderer;
    MeshFilter meshFilter;
    MeshCollider meshCollider;
    MapSetting setting;
    public Height noiseHeight;
    public Height mapHeight;
    public Vector2 noiseMinMax;
    public Vector2 mapMinMax;

    public TerrainChunk(Vector2 center, MapSetting setting, Transform parent, Material material)
    {
        this.setting = setting;
        meshObject = new GameObject("Terrain Chunk");
        meshRenderer = meshObject.AddComponent<MeshRenderer>();
        meshFilter = meshObject.AddComponent<MeshFilter>();
        meshCollider = meshObject.AddComponent<MeshCollider>();
        meshRenderer.material = material;

        meshObject.transform.parent = parent;
        meshObject.transform.localPosition = new Vector3(center.x, 0, center.y);

        noiseHeight = NoiseHeight.Generate(setting, center);
        noiseMinMax = new Vector2(noiseHeight.minValue, noiseHeight.maxValue);
    }

    public void Create(Vector2 range)
    {
        mapHeight = MapHeight.Generate(setting, range, noiseHeight.values);
        mapMinMax = new Vector2(mapHeight.minValue, mapHeight.maxValue);
        meshData = MeshGenerator.GenerateTerrainMesh(mapHeight.values, setting);
        Mesh mesh = meshData.CreateMesh();
        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;
    }

}
