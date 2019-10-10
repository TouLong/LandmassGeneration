using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;
using System;

public class TerrainEditor : EditorWindow
{
    TerrainGenerator terrain;
    GUILayoutOption[] btnOption = new GUILayoutOption[] { GUILayout.MaxWidth(200), GUILayout.Height(20) };
    MapSetting setting;
    MapSetting exportSetting;
    Vector2 scrollPos;
    Texture2D mapImage;
    [MenuItem("Window/Terrain Editor")]
    public static void ShowWindow()
    {
        GetWindow<TerrainEditor>("Terrain Editor");
    }
    public bool GetTerrain()
    {
        terrain = FindObjectOfType<TerrainGenerator>();
        return terrain != null;
    }
    public bool GetSetting()
    {
        setting = terrain.setting;
        return setting != null;
    }
    void OnGUI()
    {
        minSize = new Vector2(400, 100);

        if (!GetTerrain())
        {
            GUILayout.Label("找不到Terrain Generator");
            return;
        }
        terrain.setting = EditorGUILayout.ObjectField("設定檔", terrain.setting, typeof(MapSetting), true) as MapSetting;
        if (!GetSetting())
        {
            GUILayout.Label("Terrain Generator沒Setting檔");
            return;
        }

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        #region Main And Image
        GUILayout.Label(string.Format("地圖尺寸: {0}x{0}x{1}, 地圖網格: {2}x{2}, Noise範圍: {3:0.00}~{4:0.00} ",
        setting.MapSideLength, terrain.mapPeakMax, setting.MapSideMesh, terrain.noisePeakMin, terrain.noisePeakMax));
        GUILayout.Label(mapImage);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.EndHorizontal();
        if (GUILayout.Button("生成", btnOption)) GenerateAll();
        if (GUILayout.Button("生成(保留物件)", btnOption)) GenerateChunk();
        if (GUILayout.Button("隨機生成", btnOption)) Random();
        if (GUILayout.Button("生成導航", btnOption)) NavMesh();
        if (GUILayout.Button("清除", btnOption)) Clear();
        #endregion
        GUILine();

        #region Objects And Distribution
        ListField("地圖物件", setting.objectsDistribution.Count,
                    () => { setting.objectsDistribution.Add(new MapSetting.ObjectDistribution()); },
                    () => { setting.objectsDistribution.RemoveAt(setting.objectsDistribution.Count - 1); });
        for (int i = 0; i < setting.objectsDistribution.Count; i++)
        {
            GUILayout.BeginVertical("Box");
            EditorGUI.indentLevel = 1;
            setting.objectsDistribution[i].groupName = EditorGUILayout.TextField("分類", setting.objectsDistribution[i].groupName);
            List<GameObject> objects = setting.objectsDistribution[i].objects;
            if (objects != null)
            {
                ListField("物件", objects.Count,
                () =>
                {
                    objects.Add(terrain.gameObject);
                    objects[objects.Count - 1] = null;
                },
                () => { objects.RemoveAt(objects.Count - 1); });
                for (int j = 0; j < objects.Count; j++)
                {
                    objects[j] = EditorGUILayout.ObjectField(objects[j], typeof(GameObject), true) as GameObject;
                }
            }
            List<MapSetting.ObjectDistribution.Distribution> distributions = setting.objectsDistribution[i].distributions;
            if (distributions != null)
            {
                ListField("分佈", distributions.Count,
                        () => { distributions.Add(new MapSetting.ObjectDistribution.Distribution()); },
                        () => { distributions.RemoveAt(distributions.Count - 1); });
                for (int j = 0; j < distributions.Count; j++)
                {
                    EditorGUIUtility.labelWidth = 230;
                    distributions[j].radius = EditorGUILayout.FloatField(string.Format("分佈範圍(高度): {0:0.00} ~ {1:0.00} | 分散度: ", distributions[j].region.x, distributions[j].region.y), distributions[j].radius);
                    EditorGUIUtility.labelWidth = 0;
                    EditorGUILayout.MinMaxSlider(ref distributions[j].region.x, ref distributions[j].region.y, terrain.mapPeakMin, terrain.mapPeakMax);
                }
            }
            EditorGUI.indentLevel = 0;
            EditorGUIUtility.labelWidth = 0;
            GUILayout.EndVertical();
        }

        EditorGUI.indentLevel = 0;
        if (GUILayout.Button("更新地圖物件", btnOption)) GenerateMapObject();
        #endregion
        GUILine();

        #region Layer
        EditorGUILayout.ObjectField("地形材質", terrain.terrainMaterial, typeof(Material), true);
        EditorGUILayout.ObjectField("湖面材質", terrain.lakeMaterial, typeof(Material), true);
        AnimationCurve heightCurve = setting.heightCurve;
        if (setting.layers.Count < heightCurve.keys.Length - 1)
        {
            for (int i = 0; i < heightCurve.keys.Length - 1; i++)
            {
                if (!setting.layers.Exists(x => x.height == heightCurve.keys[i].value))
                    setting.layers.Add(new MapSetting.Layer(heightCurve.keys[i].value));
            }
            terrain.CreateMaterial();
            terrain.UpdateMaterial();
        }
        else if (setting.layers.Count > heightCurve.keys.Length - 1)
        {
            for (int i = 0; i < setting.layers.Count; i++)
            {
                if (!heightCurve.keys.ToList().Exists(x => x.value == setting.layers[i].height))
                    setting.layers.Remove(setting.layers[i]);
            }
            terrain.UpdateMaterial();
        }
        int[] layerIndeices = new int[setting.layers.Count + 1];
        string[] layerLabel = new string[setting.layers.Count + 1];
        layerIndeices[0] = -1;
        layerLabel[0] = "null";
        for (int i = 0; i < setting.layers.Count; i++)
        {
            MapSetting.Layer layer = setting.layers[i];
            layer.height = heightCurve.keys[i].value;
            float rangeMin = layer.height * terrain.mapPeakMax;
            float rangeMax = i == setting.layers.Count - 1 ? terrain.mapPeakMax : setting.layers[i + 1].height * terrain.mapPeakMax;
            string label = string.Format("區域{0}-高度:{1:0.00}~{2:0.00}", i + 1, rangeMin, rangeMax);
            GUILayout.Label(label, GUILayout.MaxWidth(210f));
            EditorGUILayout.BeginHorizontal();
            layer.blendStrength = EditorGUILayout.Slider(layer.blendStrength, 0, i == 0 ? 0 : 1, GUILayout.MinWidth(80f));
            layer.color = EditorGUILayout.ColorField(layer.color, GUILayout.MinWidth(20f));
            EditorGUILayout.EndHorizontal();
            layerIndeices[i + 1] = i;
            layerLabel[i + 1] = label;
        }
        EditorGUILayout.BeginHorizontal();
        setting.lakeLayer = EditorGUILayout.IntPopup("湖面", setting.lakeLayer, layerLabel, layerIndeices);
        setting.mountainLayer = EditorGUILayout.IntPopup("山區", setting.mountainLayer, layerLabel, layerIndeices);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.CurveField(heightCurve, GUILayout.MinHeight(100f));
        if (GUILayout.Button("隨機顏色", btnOption)) RandomLayerColor();
        if (GUILayout.Button("更新材質", btnOption)) UpdateMaterial();
        #endregion
        GUILine();

        #region Parameter
        setting.mapDimension = EditorGUILayout.IntSlider("Map Dimension", setting.mapDimension, 1, 10);
        setting.chunkMesh = EditorGUILayout.IntSlider("Chunk Mesh", setting.chunkMesh, 10, 250);
        setting.mapHeight = EditorGUILayout.IntSlider("Map Height", setting.mapHeight, 5, 50);
        setting.mapScale = EditorGUILayout.IntSlider("Map Scale", setting.mapScale, 1, 20);
        setting.noiseScale = EditorGUILayout.IntSlider("Noise Scale", setting.noiseScale, 25, 250);
        setting.octaves = EditorGUILayout.IntSlider("Octaves", setting.octaves, 1, 5);
        setting.persistance = EditorGUILayout.Slider("Persistance", setting.persistance, 0, 1);
        setting.lacunarity = EditorGUILayout.Slider("Lacunarity", setting.lacunarity, 1, 5);
        setting.seed = EditorGUILayout.IntField("Seed", setting.seed);
        setting.offset = EditorGUILayout.Vector2Field("Offset", setting.offset);
        if (GUILayout.Button("生成", btnOption)) GenerateAll();
        if (GUILayout.Button("隨機生成", btnOption)) Random();
        #endregion
        GUILine();

        EditorGUILayout.EndScrollView();
        EditorUtility.SetDirty(setting);

    }
    void GenerateAll()
    {
        terrain.ClearChunks();
        terrain.ClearNav();
        terrain.ClearMapObject();
        terrain.GenerateChunks();
        terrain.GenerateLake();
        terrain.GenerateMapObject();
        terrain.GenerateNavMesh();
        UpdateMaterial();
        mapImage = MapImage.Generate(setting, terrain.chunkList.Select(a => a.mapHeight).ToList());
    }
    void Random()
    {
        setting.seed = UnityEngine.Random.Range(-1000, 1000);
        GenerateAll();
    }
    void GenerateChunk()
    {
        terrain.ClearChunks();
        terrain.ClearNav();
        terrain.GenerateChunks();
        terrain.GenerateLake();
    }
    void Clear()
    {
        terrain.ClearChunks();
        terrain.ClearNav();
        terrain.ClearMapObject();
        mapImage = new Texture2D(0, 0);
    }
    void NavMesh()
    {
        terrain.ClearNav();
        terrain.GenerateNavMesh();
    }
    void GenerateMapObject()
    {
        terrain.ClearMapObject();
        terrain.GenerateMapObject();
    }
    void UpdateMaterial()
    {
        terrain.UpdateMaterial();
    }
    void RandomLayerColor()
    {
        for (int i = 0; i < setting.layers.Count; i++)
        {
            setting.layers[i].color.r = UnityEngine.Random.value;
            setting.layers[i].color.b = UnityEngine.Random.value;
            setting.layers[i].color.g = UnityEngine.Random.value;
        }
        terrain.UpdateMaterial();
    }
    void ListField(string label, int count, Action add, Action remove)
    {
        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label + ": " + count.ToString());
        if (GUILayout.Button("+", GUILayout.MaxWidth(20)))
            add();
        if (GUILayout.Button("-", GUILayout.MaxWidth(20)) && count > 0)
            remove();
        GUILayout.EndHorizontal();
    }
    static void GUILine()
    {
        EditorGUILayout.Space();
        Rect rect = EditorGUILayout.GetControlRect(false, 1);
        rect.height = 1;
        EditorGUI.DrawRect(rect, Color.gray);
        EditorGUILayout.Space();
    }

}
