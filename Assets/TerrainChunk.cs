using UnityEngine;
public class TerrainChunk
{
    GameObject meshObject;
    MeshData meshData;
    MeshRenderer meshRenderer;
    MeshFilter meshFilter;
    MeshCollider meshCollider;
    NoiseMapHeight height;
    public Vector2 noiseMinMax;
    public Vector2 mapMinMax;

    public TerrainChunk(Vector2 coord, MapSetting setting, Transform parent, Material material)
    {
        meshObject = new GameObject("Terrain Chunk");
        meshRenderer = meshObject.AddComponent<MeshRenderer>();
        meshFilter = meshObject.AddComponent<MeshFilter>();
        meshCollider = meshObject.AddComponent<MeshCollider>();
        meshRenderer.material = material;

        meshObject.transform.position = new Vector3(coord.x, 0, coord.y);
        meshObject.transform.parent = parent;

        height = NoiseMap.Generate(setting, coord);
        noiseMinMax = new Vector2(height.noise.minValue, height.noise.maxValue);
        mapMinMax = new Vector2(height.map.minValue, height.map.maxValue);
        meshData = MeshGenerator.GenerateTerrainMesh(height.map.values, setting);
        Mesh mesh = meshData.CreateMesh();
        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;
    }

}
