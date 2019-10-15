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
    List<Color> colorList = new List<Color>()
    {
        Color.red,
        Color.green,
        Color.blue,
        Color.black,
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
        float radius = radiusList[Random.Range(0, radiusList.Count)];
        List<Vector2> spawnPoints = new List<Vector2> { new Vector2(Random.Range(0, region.x), Random.Range(0, region.y)) };
        List<int>[,] grid = new List<int>[Mathf.CeilToInt(region.x / cellSize), Mathf.CeilToInt(region.y / cellSize)];
        while (spawnPoints.Count > 0)
        {
            int spawnIndex = Random.Range(0, spawnPoints.Count);
            Vector2 spawnCenter = spawnPoints[spawnIndex];
            bool stop = false;
            for (int n = 0; n < numSamplesBeforeRejection; n++)
            {
                float angle = Random.value * Mathf.PI * 2;
                Vector2 dir = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
                Vector2 candidate = spawnCenter + dir * Random.Range(radius, 2 * radius);
                if (IsValid(grid, circles, candidate, region, cellSize, radius))
                {
                    Circle circle = new Circle() { radius = radius, center = candidate };
                    circles.Add(circle);
                    spawnPoints.Add(candidate);
                    MarkCircle(ref grid, circle, cellSize, circles.Count);
                    stop = true;
                    radius = radiusList[Random.Range(0, radiusList.Count)];
                    break;
                }
            }
            if (!stop)
            {
                spawnPoints.RemoveAt(spawnIndex);
            }
        }
    }
    bool IsValid(List<int>[,] grid, List<Circle> circles, Vector2 candidate, Vector2 region, float cellSize, float radius)
    {
        if (candidate.x >= 0 && candidate.x < region.x && candidate.y >= 0 && candidate.y < region.y)
        {
            int cellX = (int)(candidate.x / cellSize);
            int cellY = (int)(candidate.y / cellSize);
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
                            float sqrDst = Vector2.Distance(candidate, circles[pointIndex].center);
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
        }
    }
}
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