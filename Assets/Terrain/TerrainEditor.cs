﻿using UnityEngine;
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
        GUILayout.Label(string.Format("地圖尺寸: {0}x{0}x{1}, 地圖網格: {2}x{2}",
        setting.MapSideLength, setting.MapHeight, setting.MapSideMesh));
        GUILayout.Label(mapImage);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("生成", GUILayout.MaxWidth(50));
        if (GUILayout.Button("全部", btnOption)) GenerateAll();
        if (GUILayout.Button("地形", btnOption)) GenerateChunks();
        if (GUILayout.Button("導航", btnOption)) GenerateNav();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("隨機生成", GUILayout.MaxWidth(50));
        if (GUILayout.Button("全部", btnOption)) GenerateRandomAll();
        if (GUILayout.Button("地形", btnOption)) GenerateRandomChunks();
        if (GUILayout.Button("物件", btnOption)) GenerateMapObject();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("清除", GUILayout.MaxWidth(50));
        if (GUILayout.Button("全部", btnOption)) ClearAll();
        if (GUILayout.Button("地形", btnOption)) ClearChunks();
        if (GUILayout.Button("物件", btnOption)) ClearMapObject();
        if (GUILayout.Button("導航", btnOption)) ClearNav();
        EditorGUILayout.EndHorizontal();
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
                    EditorGUILayout.MinMaxSlider(ref distributions[j].region.x, ref distributions[j].region.y, 0, setting.MapHeight);
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
            float rangeMin = layer.height * setting.MapHeight;
            float rangeMax = i == setting.layers.Count - 1 ? setting.MapHeight : setting.layers[i + 1].height * setting.MapHeight;
            string label = string.Format("區域{0}-高度:{1:0.00}~{2:0.00}", i + 1, rangeMin, rangeMax);
            GUILayout.Label(label, GUILayout.MaxWidth(210f));
            EditorGUILayout.BeginHorizontal();
            layer.blendStrength = EditorGUILayout.Slider(layer.blendStrength, 0, i == 0 ? 0 : 1, GUILayout.MinWidth(80f));
            layer.color = EditorGUILayout.ColorField(layer.color, GUILayout.MinWidth(20f));
            EditorGUILayout.EndHorizontal();
            layerIndeices[i + 1] = i;
            layerLabel[i + 1] = label;
        }
        EditorGUILayout.LabelField("高度分佈", GUILayout.MaxWidth(50));
        EditorGUILayout.CurveField(heightCurve, GUILayout.MinHeight(100f));
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.ObjectField("地形材質", terrain.terrainMaterial, typeof(Material), true);
        EditorGUILayout.ObjectField("湖面材質", terrain.lakeMaterial, typeof(Material), true);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        setting.mountainLayer = EditorGUILayout.IntPopup("山區", setting.mountainLayer, layerLabel, layerIndeices);
        setting.lakeLayer = EditorGUILayout.IntPopup("湖面", setting.lakeLayer, layerLabel, layerIndeices);
        EditorGUILayout.EndHorizontal();
        #endregion
        GUILine();

        #region Parameter
        setting.mapDimension = EditorGUILayout.IntSlider(string.Format("地塊數量 {1}({0}x{0})", setting.mapDimension, setting.mapDimension * setting.mapDimension), setting.mapDimension, 1, 10);
        setting.chunkMesh = EditorGUILayout.IntSlider("地塊網格數", setting.chunkMesh, 10, 250);
        setting.mapHeight = EditorGUILayout.IntSlider("地形高度", setting.mapHeight, 5, 50);
        setting.mapScale = EditorGUILayout.IntSlider("地形放大", setting.mapScale, 1, 20);
        setting.noiseScale = EditorGUILayout.IntSlider("Noise放大", setting.noiseScale, 25, 250);
        setting.octaves = EditorGUILayout.IntSlider("細緻度", setting.octaves, 2, 5);
        setting.lacunarity = EditorGUILayout.Slider("隙度", setting.lacunarity, 1, 5);
        setting.persistance = EditorGUILayout.Slider("持久度", setting.persistance, 0, 1);
        setting.seed = EditorGUILayout.IntField("隨機種子", setting.seed);
        setting.offset = EditorGUILayout.Vector2Field("位移", setting.offset);
        if (GUILayout.Button("生成地形", btnOption)) GenerateChunks();
        if (GUILayout.Button("隨機生成地形", btnOption)) GenerateRandomChunks();
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
        mapImage = MapImage.Generate(setting, terrain.noise);
    }
    void GenerateChunks()
    {
        terrain.ClearChunks();
        terrain.GenerateChunks();
        terrain.GenerateLake();
        UpdateMaterial();
        mapImage = MapImage.Generate(setting, terrain.noise);
    }
    void GenerateNav()
    {
        terrain.ClearNav();
        terrain.GenerateNavMesh();
    }
    void GenerateRandomAll()
    {
        setting.seed = UnityEngine.Random.Range(-1000, 1000);
        GenerateAll();
    }
    void GenerateRandomChunks()
    {
        setting.seed = UnityEngine.Random.Range(-1000, 1000);
        GenerateChunks();
    }
    void GenerateMapObject()
    {
        terrain.ClearMapObject();
        terrain.GenerateMapObject();
    }
    void ClearAll()
    {
        terrain.ClearChunks();
        terrain.ClearNav();
        terrain.ClearMapObject();
        mapImage = new Texture2D(0, 0);
    }
    void ClearNav()
    {
        terrain.ClearNav();
    }
    void ClearMapObject()
    {
        terrain.ClearMapObject();
    }
    void ClearChunks()
    {
        terrain.ClearChunks();
        mapImage = new Texture2D(0, 0);
    }
    void UpdateMaterial()
    {
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