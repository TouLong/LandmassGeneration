using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;
using System;

public class TerrainEditor : EditorWindow
{
    static TerrainGenerator terrain;
    static MapSetting setting;
    Vector2 scrollPos;
    [MenuItem("Window/Terrain Editor")]
    public static void ShowWindow()
    {
        GetWindow<TerrainEditor>("Terrain Editor");
    }
    public static bool GetTerrain()
    {
        terrain = FindObjectOfType<TerrainGenerator>();
        setting = terrain.setting;
        return terrain != null;
    }
    public static bool GetSetting()
    {
        if (setting == null)
        {
            setting = EditorGUILayout.ObjectField(setting, typeof(MapSetting), true) as MapSetting;
            terrain.setting = setting;
        }
        return setting != null;
    }
    void OnGUI()
    {
        if (!GetTerrain())
        {
            GUILayout.Label("找不到Terrain Generator");
            return;
        }
        if (!GetSetting())
        {
            GUILayout.Label("Terrain Generator沒Setting檔");
            return;
        }
        minSize = new Vector2(400, 100);
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        GUILayoutOption[] btnOption = new GUILayoutOption[] { GUILayout.MaxWidth(200), GUILayout.Height(20) };


        #region Info And Image
        GUILayout.Label(string.Format("地圖尺寸: {0}x{0}x{1}, 地圖網格: {2}x{2}, Noise範圍: {3:0.00}~{4:0.00} ",
        setting.MapSideLength, terrain.mapPeakMax, setting.MapSideMesh, terrain.noisePeakMin, terrain.noisePeakMax));
        GUILayout.Label(terrain.texture);
        if (GUILayout.Button("生成", btnOption)) terrain.Generate();
        if (GUILayout.Button("隨機生成", btnOption)) terrain.RandomGenerate();
        if (GUILayout.Button("清除", btnOption)) terrain.Clear();
        #endregion
        GUILine();

        #region Objects And Distribution
        ListField("地圖物件", setting.objectsDistribution.Count,
                    //() => { setting.objectsDistribution.Add(CreateInstance(typeof(MapSetting.ObjectDistribution)) as MapSetting.ObjectDistribution); },
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
                    distributions[j].radius = EditorGUILayout.FloatField(string.Format("分佈範圍(高度): {0:0} ~ {1:0} | 分散度: ", distributions[j].region.x, distributions[j].region.y), distributions[j].radius);
                    EditorGUIUtility.labelWidth = 0;
                    EditorGUILayout.MinMaxSlider(ref distributions[j].region.x, ref distributions[j].region.y, 0, 100);
                }
            }
            EditorGUI.indentLevel = 0;
            EditorGUIUtility.labelWidth = 0;
            GUILayout.EndVertical();
        }

        EditorGUI.indentLevel = 0;
        if (GUILayout.Button("更新地圖物件", btnOption)) terrain.GenerateMapObject();
        #endregion
        GUILine();

        #region Layer
        setting.mapMaterial = EditorGUILayout.ObjectField(setting.mapMaterial, typeof(Material), true) as Material;
        setting.waterMaterial = EditorGUILayout.ObjectField(setting.waterMaterial, typeof(Material), true) as Material;
        AnimationCurve heightCurve = setting.heightCurve;
        if (setting.layers.Count < heightCurve.keys.Length - 1)
        {
            for (int i = 0; i < heightCurve.keys.Length - 1; i++)
            {
                if (!setting.layers.Exists(x => x.height == heightCurve.keys[i].value))
                    setting.layers.Add(new MapSetting.Layer(heightCurve.keys[i].value));
            }
            setting.mapMaterial = new Material(Shader.Find("Custom/Terrain"));
            terrain.UpdateMaterial();
            terrain.ComputeRatio();
        }
        else if (setting.layers.Count > heightCurve.keys.Length - 1)
        {
            for (int i = 0; i < setting.layers.Count; i++)
            {
                if (!heightCurve.keys.ToList().Exists(x => x.value == setting.layers[i].height))
                    setting.layers.Remove(setting.layers[i]);
            }
            terrain.UpdateMaterial();
            terrain.ComputeRatio();
        }

        for (int i = 0; i < setting.layers.Count; i++)
        {
            MapSetting.Layer layer = setting.layers[i];
            layer.height = heightCurve.keys[i].value;
            //float rangeMin = layer.height * terrain.mapPeakMax;
            //float rangeMax = i == layerHeights.Length - 1 ? terrain.mapPeakMax : setting.layers[i + 1].height * terrain.mapPeakMax;
            float rangeMin = layer.height;
            float rangeMax = i == terrain.distributionRatio.Length - 1 ? 1 : setting.layers[i + 1].height;
            GUILayout.Label(string.Format("區域{0}-高度:{1:0.00}~{2:0.00}, 佔比:{3:0}%",
                i + 1, rangeMin, rangeMax, terrain.distributionRatio[i] * 100), GUILayout.MaxWidth(210f));
            EditorGUILayout.BeginHorizontal();
            layer.blendStrength = EditorGUILayout.Slider(layer.blendStrength, 0, i == 0 ? 0 : 1, GUILayout.MinWidth(80f));
            layer.color = EditorGUILayout.ColorField(layer.color, GUILayout.MinWidth(20f));
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.LabelField("水面:");
        setting.waterLayer = EditorGUILayout.IntSlider(setting.waterLayer, -1, setting.layers.Count - 1);
        EditorGUILayout.CurveField(heightCurve, GUILayout.MinHeight(100f));
        if (GUILayout.Button("生成", btnOption)) terrain.Generate();
        if (GUILayout.Button("更新材質", btnOption)) terrain.UpdateMaterial();
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
        if (GUILayout.Button("生成", btnOption)) terrain.Generate();
        if (GUILayout.Button("隨機生成", btnOption)) terrain.RandomGenerate();
        #endregion
        GUILine();

        EditorGUILayout.EndScrollView();
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
