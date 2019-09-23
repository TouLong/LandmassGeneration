using UnityEngine;
using UnityEditor;
using UnityEngine.AI;
public class TerrainChunk
{
    readonly GameObject meshObject;
    readonly MapSetting setting;
    readonly Vector2 location;
    public NavMeshBuildSource navMesh;
    public HeightData noiseHeight;
    public HeightData mapHeight;

    public TerrainChunk(Vector2 center, MapSetting setting, Transform parent)
    {
        this.setting = setting;
        meshObject = new GameObject("Terrain Chunk");
        MeshRenderer meshRenderer = meshObject.AddComponent<MeshRenderer>();
        meshRenderer.material = setting.mapMaterial;
        meshObject.transform.parent = parent;
        meshObject.transform.localPosition = new Vector3(center.x, 0, center.y);
        location = center;
        GameObjectUtility.SetStaticEditorFlags(meshObject, StaticEditorFlags.NavigationStatic);
    }
    public HeightData ComputeNoise()
    {
        noiseHeight = NoiseHeight.Generate(setting, location);
        return noiseHeight;
    }
    public HeightData Create(Vector2 range)
    {
        mapHeight = MapHeight.Generate(setting, range, noiseHeight.values);
        Mesh mesh = MeshGenerator.GenerateTerrainMesh(mapHeight.values, setting.mapScale);
        MeshFilter meshFilter = meshObject.AddComponent<MeshFilter>();
        meshFilter.mesh = mesh;
        MeshCollider meshCollider = meshObject.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = mesh;
        navMesh = new NavMeshBuildSource()
        {
            shape = NavMeshBuildSourceShape.Mesh,
            sourceObject = meshFilter.sharedMesh,
            transform = meshFilter.transform.localToWorldMatrix,
        };
        return mapHeight;
    }
}

