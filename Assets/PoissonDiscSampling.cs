using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

public struct Circle
{
    public Vector2 center;
    public float radius;
}
public class PoissonDiscSampling : MonoBehaviour
{
    public List<float> radiusList;
    public Vector2 region;
    public int numSamplesBeforeRejection = 30;
    static public List<Circle> circles = new List<Circle>();
    Dictionary<float, Color> colorDic = new Dictionary<float, Color>();
    List<int>[,] grid;
    List<Color> colorList = new List<Color>()
    {
        Color.red,
        Color.green,
        Color.blue,
        Color.yellow,
        Color.white
    };
    public void GeneratePoints()
    {
        int c = 0;
        foreach (float f in radiusList)
        {
            if (!colorDic.ContainsKey(f))
                colorDic.Add(f, colorList[c++]);
        }
        float cellSize = radiusList.Min() / Mathf.Sqrt(2);
        float spawnRadius = radiusList[Random.Range(0, radiusList.Count)];
        List<Circle> spawnCircles = new List<Circle> {
            new Circle() { center = new Vector2(Random.Range(0, region.x), Random.Range(0, region.y)), radius = spawnRadius } };
        grid = new List<int>[Mathf.CeilToInt(region.x / cellSize), Mathf.CeilToInt(region.y / cellSize)];
        while (spawnCircles.Count > 0)
        {
            int spawnIndex = Random.Range(0, spawnCircles.Count);
            Circle spawnCircle = spawnCircles[spawnIndex];
            bool stop = false;
            for (int n = 0; n < numSamplesBeforeRejection; n++)
            {
                float angle = Random.value * Mathf.PI * 2;
                Vector2 dir = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
                Vector2 spawnCenter = spawnCircle.center + dir * Random.Range(spawnCircle.radius / 2 + spawnRadius / 2, spawnRadius + spawnCircle.radius);
                if (IsValid(grid, circles, spawnCenter, spawnRadius, region, cellSize))
                {
                    Circle circle = new Circle() { radius = spawnRadius, center = spawnCenter };
                    circles.Add(circle);
                    spawnCircles.Add(circle);
                    MarkCircle(ref grid, circle, cellSize, circles.Count);
                    stop = true;
                    spawnRadius = radiusList[Random.Range(0, radiusList.Count)];
                    break;
                }
            }
            if (!stop)
            {
                spawnCircles.RemoveAt(spawnIndex);
            }
        }
    }
    bool IsValid(List<int>[,] grid, List<Circle> circles, Vector2 center, float radius, Vector2 region, float cellSize)
    {
        if (center.x >= 0 && center.x < region.x && center.y >= 0 && center.y < region.y)
        {
            int cellX = (int)(center.x / cellSize);
            int cellY = (int)(center.y / cellSize);
            int offset = Mathf.CeilToInt(radius / cellSize);
            int searchStartX = Mathf.Max(0, cellX - offset);
            int searchEndX = Mathf.Min(cellX + offset, grid.GetLength(0) - 1);
            int searchStartY = Mathf.Max(0, cellY - offset);
            int searchEndY = Mathf.Min(cellY + offset, grid.GetLength(1) - 1);
            for (int x = searchStartX; x <= searchEndX; x++)
            {
                for (int y = searchStartY; y <= searchEndY; y++)
                {
                    if (grid[x, y] == null) continue;
                    foreach (int i in grid[x, y])
                    {
                        int pointIndex = i - 1;
                        if (pointIndex != -1)
                        {
                            float sqrDst = Vector2.Distance(center, circles[pointIndex].center);
                            float dist = circles[pointIndex].radius / 2 + radius / 2;
                            if (sqrDst < dist)
                                return false;
                        }
                    }
                }
            }
            return true;
        }
        return false;
    }
    void MarkCircle(ref List<int>[,] grid, Circle circle, float cellSize, int mark)
    {
        int cellX = (int)(circle.center.x / cellSize);
        int cellY = (int)(circle.center.y / cellSize);
        int offset = (int)(circle.radius / cellSize);
        int markStartX = Mathf.Max(0, cellX - offset);
        int markEndX = Mathf.Min(cellX + offset, grid.GetLength(0) - 1);
        int markStartY = Mathf.Max(0, cellY - offset);
        int markEndY = Mathf.Min(cellY + offset, grid.GetLength(1) - 1);
        for (int x = markStartX; x <= markEndX; x++)
        {
            for (int y = markStartY; y <= markEndY; y++)
            {
                float dist = Vector2.Distance(new Vector2(cellX, cellY), new Vector2(x, y));
                if (dist <= circle.radius / 2 / cellSize)
                {
                    if (grid[x, y] == null)
                        grid[x, y] = new List<int>() { mark };
                    else
                        grid[x, y].Add(mark);
                }
            }
        }
    }
    void OnDrawGizmos()
    {
        if (circles.Count > 0)
        {
            Gizmos.DrawWireCube(region / 2, region);
            foreach (Circle circle in circles)
            {
                Gizmos.color = colorDic[circle.radius];
                Gizmos.DrawSphere(circle.center, circle.radius / 2);
            }
            Gizmos.color = Color.white;
            for (int x = 0; x < grid.GetLength(0); x++)
            {
                for (int y = 0; y < grid.GetLength(1); y++)
                {
                    if (grid[x, y] != null)
                        Gizmos.DrawCube(new Vector3(x * 5 - region.x * 3 / 2, y * 5, 0), Vector3.one * 5);
                }
            }
        }
    }
}
#if UNITY_EDITOR
[CustomEditor(typeof(PoissonDiscSampling))]
public class PoissonDiscSamplingEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        PoissonDiscSampling poissonDisc = target as PoissonDiscSampling;

        if (GUILayout.Button("GeneratePoints"))
        {
            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Restart();
            PoissonDiscSampling.circles.Clear();
            poissonDisc.GeneratePoints();
            Debug.Log(stopwatch.ElapsedMilliseconds.ToString());
        }
        if (GUILayout.Button("Clear"))
        {
            PoissonDiscSampling.circles.Clear();
        }
    }

}
#endif