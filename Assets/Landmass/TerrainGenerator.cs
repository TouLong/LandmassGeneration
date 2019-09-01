using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

public class TerrainGenerator : MonoBehaviour
{
    [HideInInspector] public List<TerrainChunk> terrainChunkList = new List<TerrainChunk>();
    [HideInInspector] public float noisePeakMin;
    [HideInInspector] public float noisePeakMax;
    [HideInInspector] public float mapPeakMin;
    [HideInInspector] public float mapPeakMax;
    [HideInInspector] public Texture2D texture;
    public MapSetting setting;
    public Material mapMaterial;
    [Range(-1, 10)]
    public int water;
    public Material waterMaterial;
    [HideInInspector] public GameObject terrain;
    [HideInInspector] public GameObject mapObject;

    public void GenerateChunks()
    {
        ClearChunk();
        Vector3 mapOffset = new Vector3(setting.MapSideLength, 0, setting.MapSideLength);
        terrain = new GameObject("Terrain");
        terrain.transform.parent = transform;
        List<Vector2> chunkNoiseMinMax = new List<Vector2>();
        List<Vector2> chunkMapMinMax = new List<Vector2>();
        int meshSize = setting.chunkMesh;
        for (int y = 0; y < setting.mapDimension; y++)
        {
            for (int x = 0; x < setting.mapDimension; x++)
            {
                Vector2 chunkPos = new Vector2(x * meshSize, y * meshSize);
                TerrainChunk newChunk = new TerrainChunk(chunkPos, setting, terrain.transform, mapMaterial);
                terrainChunkList.Add(newChunk);
                chunkNoiseMinMax.Add(newChunk.noiseMinMax);
            }
        }
        noisePeakMin = chunkNoiseMinMax.Min(x => x.x);
        noisePeakMax = chunkNoiseMinMax.Max(x => x.y);
        foreach (TerrainChunk chunk in terrainChunkList)
        {
            chunk.Create(new Vector2(noisePeakMin, noisePeakMax));
            chunkMapMinMax.Add(chunk.mapMinMax);
        }
        mapPeakMin = chunkMapMinMax.Min(x => x.x);
        mapPeakMax = chunkMapMinMax.Max(x => x.y);
        terrain.transform.localScale = new Vector3(setting.mapScale, 1, setting.mapScale);
        texture = MapImage.Generate(setting, terrainChunkList.Select(x => x.mapHeight).ToList());

        if (water >= 0)
        {
            GameObject waterGO = GameObject.CreatePrimitive(PrimitiveType.Plane);
            waterGO.name = "Water";
            waterGO.transform.parent = transform;
            waterGO.transform.localScale = new Vector3(setting.MapSideLength / 10f, 1, setting.MapSideLength / 10f);
            float waterHeight = setting.layers[Mathf.Min(water, setting.layers.Count - 1)].height * mapPeakMax;
            waterGO.transform.localPosition = new Vector3(setting.MapSideLength / 2f, waterHeight, setting.MapSideLength / 2f);
            Renderer waterRender = waterGO.transform.GetComponent<Renderer>();
            waterRender.sharedMaterial = waterMaterial;
            Collider waterCollider = waterGO.transform.GetComponent<Collider>();
            DestroyImmediate(waterCollider);
        }

        UpdateMaterial();

        PDS();
    }

    public void RandomGenerateChunks()
    {
        setting.seed = Random.Range(-1000, 1000);
        GenerateChunks();
    }

    public void ClearChunk()
    {
        terrainChunkList.Clear();
        while (transform.childCount != 0)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }
    }

    public void UpdateMaterial()
    {
        mapMaterial.SetInt("layerCount", setting.layers.Count);
        mapMaterial.SetColorArray("baseColors", setting.layers.Select(x => x.color).ToArray());
        mapMaterial.SetFloatArray("baseStartHeights", setting.layers.Select(x => x.height).ToArray());
        mapMaterial.SetFloatArray("baseBlends", setting.layers.Select(x => x.blendStrength).ToArray());

        mapMaterial.SetFloat("minHeight", mapPeakMin + terrain.transform.position.y);
        mapMaterial.SetFloat("maxHeight", mapPeakMax + terrain.transform.position.y);
    }

    public void PDS()
    {
        mapObject = new GameObject("Map Object");
        mapObject.transform.parent = transform;
        mapObject.transform.localPosition = terrain.transform.localPosition;
        float regionX = setting.MapSideLength + mapObject.transform.localPosition.x;
        float regionY = setting.MapSideLength + mapObject.transform.localPosition.z;
        Vector2 sampleRegionSize = new Vector2(regionX, regionY);
        foreach (MapSetting.MapObject mapObj in setting.mapObjects)
        {
            foreach (MapSetting.MapObjectDistribute dist in mapObj.distribute)
            {
                List<Vector2> points = PoissonDiscSampling.GeneratePoints(dist.radius, sampleRegionSize);
                float heightTemp;
                for (int i = 0; i < points.Count; i++)
                {
                    Physics.Raycast(new Vector3(points[i].x, mapPeakMax + 1, points[i].y), Vector3.down, out RaycastHit hit);
                    heightTemp = hit.point.y;
                    if (heightTemp <= dist.range.y * mapPeakMax / 100 && heightTemp >= dist.range.x * mapPeakMax / 100)
                    {
                        GameObject newGO = Instantiate(mapObj.objects[Random.Range(0, mapObj.objects.Length)], mapObject.transform);
                        newGO.transform.position = new Vector3(points[i].x, hit.point.y, points[i].y);
                    }
                }
            }
        }
    }

    public float[] CountHeightLayer()
    {
        List<float> layerHeight = new List<float>();

        for (int i = 0; i < setting.layers.Count; i++)
        {
            layerHeight.Add(setting.layers[i].height * (mapPeakMax - mapPeakMin) + mapPeakMin);
        }

        float[] heightCount = new float[setting.layers.Count];
        foreach (TerrainChunk chunk in terrainChunkList)
        {
            float[,] height = chunk.mapHeight.values;
            int size = height.GetLength(0);
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float v = height[x, y];
                    for (int i = heightCount.Length - 1; i >= 0; i--)
                    {
                        if (v >= layerHeight[i])
                        {
                            heightCount[i]++;
                            break;
                        }
                    }
                }
            }
        }

        float heightCountSum = heightCount.Sum();
        for (int i = 0; i < heightCount.Length; i++)
        {
            layerHeight[i] = heightCount[i] / heightCountSum;
        }
        return layerHeight.ToArray();
    }
}

[CustomEditor(typeof(TerrainGenerator))]
public class TerrainGeneratorEditor : Editor
{
    AnimationCurve heightCurve;
    TerrainGenerator terrain;
    MapSetting setting;
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        terrain = target as TerrainGenerator;
        setting = terrain.setting;
        EditorGUILayout.LabelField(string.Format("地圖尺寸: {0}x{0}x{1}, 地圖網格: {2}x{2}, Noise範圍: {3:0.00}~{4:0.00}",
          setting.MapSideLength, terrain.mapPeakMax, setting.MapSideMesh, terrain.noisePeakMin, terrain.noisePeakMax));

        if (GUILayout.Button("Generate"))
            terrain.GenerateChunks();
        if (GUILayout.Button("Random"))
            terrain.RandomGenerateChunks();
        if (GUILayout.Button("Clear"))
            terrain.ClearChunk();
        if (GUILayout.Button("Update Material"))
            terrain.UpdateMaterial();

        heightCurve = setting.heightCurve;
        if (setting.layers.Count < heightCurve.keys.Length - 1)
        {
            for (int i = 0; i < heightCurve.keys.Length - 1; i++)
            {
                if (!setting.layers.Exists(x => x.height == heightCurve.keys[i].value))
                    setting.layers.Add(new MapSetting.Layer(heightCurve.keys[i].value));
            }
        }
        else if (setting.layers.Count > heightCurve.keys.Length - 1)
        {
            for (int i = 0; i < setting.layers.Count; i++)
            {
                if (!heightCurve.keys.ToList().Exists(x => x.value == setting.layers[i].height))
                    setting.layers.Remove(setting.layers[i]);
            }
        }

        setting.layers.Sort((x, y) => { return x.height.CompareTo(y.height); });
        float[] layerHeights = terrain.CountHeightLayer();
        for (int i = 0; i < layerHeights.Length; i++)
        {
            MapSetting.Layer layer = setting.layers[i];
            layer.height = heightCurve.keys[i].value;
            //float rangeMin = layer.height * terrain.mapPeakMax;
            //float rangeMax = i == layerHeights.Length - 1 ? terrain.mapPeakMax : setting.layers[i + 1].height * terrain.mapPeakMax;
            float rangeMin = layer.height;
            float rangeMax = i == layerHeights.Length - 1 ? 1 : setting.layers[i + 1].height;
            EditorGUILayout.LabelField(string.Format("區域{0}-高度:{1:0.00}~{2:0.00}, 佔比:{3:0}%",
                i + 1, rangeMin, rangeMax, layerHeights[i] * 100), GUILayout.MaxWidth(200f));
            EditorGUILayout.BeginHorizontal();
            layer.blendStrength = EditorGUILayout.Slider(layer.blendStrength, 0, 1, GUILayout.MinWidth(40f));
            layer.color = EditorGUILayout.ColorField(layer.color, GUILayout.MinWidth(40f));
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.CurveField(heightCurve, GUILayout.MinHeight(100f));
        GUILayout.Label(terrain.texture);
    }
}
