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
    public Material waterMaterial;
    [HideInInspector] public GameObject terrain;
    [HideInInspector] public GameObject mapObject;

    public void GenerateChunks()
    {
        ClearChunk();

        terrain = new GameObject("Terrain");
        terrain.transform.parent = transform;
        terrain.transform.localPosition = Vector3.zero;
        List<Vector2> chunkNoiseMinMax = new List<Vector2>();
        List<Vector2> chunkMapMinMax = new List<Vector2>();
        int meshSize = setting.chunkSideLength;
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
        foreach(TerrainChunk chunk in terrainChunkList)
        {
            chunk.Create(new Vector2(noisePeakMin, noisePeakMax));
            chunkMapMinMax.Add(chunk.mapMinMax);
        }
        mapPeakMin = chunkMapMinMax.Min(x => x.x);
        mapPeakMax = chunkMapMinMax.Max(x => x.y);
        terrain.transform.localScale = new Vector3(setting.mapSize, 1, setting.mapSize);
        texture = MapImage.Generate(setting, terrainChunkList.Select(x => x.mapHeight).ToList());

        GameObject water = GameObject.CreatePrimitive(PrimitiveType.Plane);
        water.name = "Water";
        water.transform.parent = transform;
        water.transform.localScale = new Vector3(setting.mapSideLength * setting.mapSize / 10f, 1, setting.mapSideLength * setting.mapSize / 10f);
        float waterHeight = setting.layers[2].height * mapPeakMax;
        water.transform.localPosition = new Vector3(setting.mapSideLength*setting.mapSize / 2f, waterHeight, setting.mapSideLength * setting.mapSize / 2f);
        Renderer waterRender = water.transform.GetComponent<Renderer>();
        waterRender.sharedMaterial = waterMaterial;
        Collider waterCollider = water.transform.GetComponent<Collider>();
        DestroyImmediate(waterCollider);

        UpdateMaterial();


        PDS();
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
        terrain.transform.localPosition = Vector3.zero;
        foreach (MapSetting.MapObject mapObj in setting.mapObjects)
        {
            foreach (MapSetting.MapObjectDistribute dist in mapObj.distribute)
            {
                List<Vector2> points = PoissonDiscSampling.GeneratePoints(dist.radius * setting.mapSize, new Vector2(setting.mapSideLength * setting.mapSize, setting.mapSideLength * setting.mapSize));
                RaycastHit raycastHit;
                float heightTemp;
                for (int i = 0; i < points.Count; i++)
                {
                    Physics.Raycast(new Vector3(points[i].x, mapPeakMax + 1, points[i].y), Vector3.down, out raycastHit);
                    heightTemp = raycastHit.point.y;
                    if (heightTemp <= dist.range.y * mapPeakMax / 100 && heightTemp >= dist.range.x * mapPeakMax / 100) 
                    {
                        GameObject newGO = Instantiate(mapObj.objects[Random.Range(0, mapObj.objects.Length)], mapObject.transform);
                        newGO.transform.position = new Vector3(points[i].x, raycastHit.point.y, points[i].y);
                        newGO.transform.localScale *= setting.mapSize;
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
                for(int y = 0; y < size; y++)
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
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        terrain = target as TerrainGenerator;

        EditorGUILayout.LabelField(string.Format("地圖尺寸 : {0}x{0}  地圖高度 : {1}~{2}  Noise範圍 : {3:0.00}~{4:0.00}",
          terrain.setting.mapSideLength, terrain.mapPeakMin, terrain.mapPeakMax, terrain.noisePeakMin, terrain.noisePeakMax));

        //EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Generate"))
            terrain.GenerateChunks();
        if (GUILayout.Button("Clear"))
            terrain.ClearChunk();
        if (GUILayout.Button("Update Material"))
            terrain.UpdateMaterial();
        //EditorGUILayout.EndHorizontal();

        heightCurve = terrain.setting.heightCurve;
        if (terrain.setting.layers.Count < heightCurve.keys.Length - 1)
        {
            for (int i = 0; i < heightCurve.keys.Length - 1; i++)
            {
                if (!terrain.setting.layers.Exists(x => x.height == heightCurve.keys[i].value))
                    terrain.setting.layers.Add(new MapSetting.Layer(heightCurve.keys[i].value));
            }
        }
        else if (terrain.setting.layers.Count > heightCurve.keys.Length - 1)
        {
            for (int i = 0; i < terrain.setting.layers.Count; i++)
            {
                if (!heightCurve.keys.ToList().Exists(x => x.value == terrain.setting.layers[i].height))
                    terrain.setting.layers.Remove(terrain.setting.layers[i]);
            }
        }
        EditorGUILayout.CurveField(heightCurve, GUILayout.MinHeight(100f));

        terrain.setting.layers.Sort((x, y) => { return x.height.CompareTo(y.height); });
        float[] layerHeights = terrain.CountHeightLayer();
        for (int i = 0; i < terrain.setting.layers.Count ; i++)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(string.Format("區域{0}-高度:{1};佔比:{2}", i + 1, terrain.setting.layers[i].height.ToString("f2"), layerHeights[i].ToString("f2")), GUILayout.MaxWidth(160f));
            Color color = EditorGUILayout.ColorField(terrain.setting.layers[i].color, GUILayout.MinWidth(80f));
            EditorGUILayout.EndHorizontal();
            terrain.setting.layers[i].color = color;
            terrain.setting.layers[i].height = heightCurve.keys[i].value;
        }
        GUILayout.Label(terrain.texture);
    }


}
