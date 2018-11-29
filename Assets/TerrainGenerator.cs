using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

public class TerrainGenerator : MonoBehaviour
{
    MapSetting setting;
    Material mapMaterial;
    List<Vector2> chunkNoiseMinMax = new List<Vector2>();
    List<Vector2> chunkMapMinMax = new List<Vector2>();
    public Vector2 terrainNoiseMinMax;
    public Vector2 terrainMapMinMax;
    List<TerrainChunk> terrainChunkList = new List<TerrainChunk>();

    private bool Initialization()
    {
        if (mapMaterial == null)
            if (Shader.Find("Custom/Terrain") != null)
                mapMaterial = new Material(Shader.Find("Custom/Terrain"));
            else
            {
                Debug.Log("揾吾到著色器");
                return false;
            }
        if (setting == null)
            if (GetComponent<MapSetting>() != null)
                setting = GetComponent<MapSetting>();
            else
            {
                Debug.Log("冇MapSetting");
                return false;
            }
        return true;
    }

    public void GenerateChunks()
    {
        if (!Initialization())
        {
            return;
        }
        int meshSize = setting.meshWorldSize;
        ClearChunk();
        for (int y = 0; y < setting.numChunk; y++)
        {
            for (int x = 0; x < setting.numChunk; x++)
            {
                Vector2 chunkCoord = new Vector2((x - (setting.numChunk - 1) / 2f) * meshSize, (y - (setting.numChunk - 1) / 2f) * meshSize);
                TerrainChunk newChunk = new TerrainChunk(chunkCoord, setting, transform, mapMaterial);
                terrainChunkList.Add(newChunk);
                chunkNoiseMinMax.Add(newChunk.noiseMinMax);
                chunkMapMinMax.Add(newChunk.mapMinMax);
            }
        }
        terrainNoiseMinMax = new Vector2(chunkNoiseMinMax.Min(x => x.x), chunkNoiseMinMax.Max(x => x.y));
        terrainMapMinMax = new Vector2(chunkMapMinMax.Min(x => x.x), chunkMapMinMax.Max(x => x.y));
        UpdateMaterial();
    }

    public void ClearChunk()
    {
        chunkMapMinMax.Clear();
        chunkNoiseMinMax.Clear();
        terrainChunkList.Clear();
        while (transform.childCount != 0)
            DestroyImmediate(transform.GetChild(0).gameObject);
    }


    public void UpdateMaterial()
    {
        mapMaterial.SetInt("layerCount",setting.layers.Length);
        mapMaterial.SetColorArray("baseColors", setting.layers.Select(x => x.color).ToArray());
        mapMaterial.SetFloatArray("baseStartHeights", setting.layers.Select(x => x.heights).ToArray());
        mapMaterial.SetFloatArray("baseBlends", setting.layers.Select(x => x.blendStrength).ToArray());

        mapMaterial.SetFloat("minHeight", terrainMapMinMax.x);
        mapMaterial.SetFloat("maxHeight", terrainMapMinMax.y);
    }

}

[CustomEditor(typeof(TerrainGenerator))]
public class TerrainGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        TerrainGenerator terrainGenerator = (TerrainGenerator)target;

        if (GUILayout.Button("Generate"))
        {
            terrainGenerator.GenerateChunks();
        }
        if (GUILayout.Button("Update Material"))
        {
            terrainGenerator.UpdateMaterial();
        }
        if (GUILayout.Button("Clear"))
        {
            terrainGenerator.ClearChunk();
        }

    }
}
