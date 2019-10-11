using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.AI;
using System.Linq;

public class MapObjectGenerator
{
    class Spawn
    {
        public float radius;
        public float regionMin;
        public float regionMax;
        public List<GameObject> mapObjects;
        public string name;
        public Spawn(float radius, Vector2 heightMask, List<GameObject> mapObjects, string name)
        {
            this.radius = radius;
            regionMin = heightMask.x;
            regionMax = heightMask.y;
            this.mapObjects = mapObjects;
            this.name = name;
        }
    }
    static public void Generate(Vector2 regionSize, float regionPeak, Transform parent, List<MapSetting.ObjectDistribution> objectsDistribution)
    {
        List<Spawn> spawns = new List<Spawn>();
        foreach (MapSetting.ObjectDistribution objectDistribution in objectsDistribution)
        {
            foreach (MapSetting.ObjectDistribution.Distribution distribution in objectDistribution.distributions)
            {
                spawns.Add(new Spawn(distribution.radius, distribution.region, objectDistribution.objects, objectDistribution.groupName));
            }
        }
        spawns = spawns.OrderByDescending(a => a.radius).ToList();
        Dictionary<string, Transform> transDic = new Dictionary<string, Transform>();
        foreach (string name in objectsDistribution.Select(a => a.groupName))
        {
            Transform group = parent.Find(name);
            if (group == null)
            {
                group = new GameObject(name).transform;
                group.parent = parent;
            }
            if (!transDic.ContainsKey(name))
            {
                transDic.Add(name, group);
            }
        }
        int[,] indexMap = new int[,] { };
        List<Vector2> allPoints = new List<Vector2>();
        for (int i = 0; i < spawns.Count; i++)
        {
            Spawn spawn = spawns[i];
            float radius = spawn.radius;
            float cellSize = radius / Mathf.Sqrt(2);
            int mapDimX = Mathf.CeilToInt(regionSize.x / cellSize);
            int mapDimY = Mathf.CeilToInt(regionSize.y / cellSize);
            if (i != 0)
            {
                Spawn preOrder = spawns[i - 1];
                if (radius != preOrder.radius)
                {
                    float scale = preOrder.radius / radius;
                    indexMap = new int[mapDimX, mapDimY];
                    for (int p = 1; p < allPoints.Count + 1; p++)
                    {
                        int cellX = (int)(allPoints[p - 1].x / cellSize);
                        int cellY = (int)(allPoints[p - 1].y / cellSize);
                        indexMap[cellX, cellY] = p;
                    }
                }
            }
            else
                indexMap = new int[mapDimX, mapDimY];

            List<Vector2> spawnPoints = new List<Vector2>() { new Vector2(Random.Range(0, regionSize.x), Random.Range(0, regionSize.y)) };
            int repeat = 10;
            int repeatCount = repeat;
            while (spawnPoints.Count > 0)
            {
                int spawnIndex = Random.Range(0, spawnPoints.Count);
                Vector2 spawnCenter = spawnPoints[spawnIndex];
                bool candidateAccepted = false;

                for (int r = 0; r < repeat; r++)
                {
                    #region
                    float angle = Random.value * Mathf.PI * 2;
                    Vector2 dir = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
                    Vector2 candidate = spawnCenter + dir * Random.Range(radius, 2 * radius);
                    if (IsValid(candidate, regionSize, cellSize, radius, allPoints, indexMap))
                    {
                        Physics.Raycast(new Vector3(candidate.x, regionPeak + 1, candidate.y), Vector3.down, out RaycastHit hit);
                        if (hit.point.y >= spawn.regionMin && hit.point.y <= spawn.regionMax)
                        {
                            GameObject newGO = Object.Instantiate(spawn.mapObjects[Random.Range(0, spawn.mapObjects.Count)], transDic[spawn.name]);
                            newGO.transform.position = new Vector3(candidate.x, hit.point.y, candidate.y);
                            GameObjectUtility.SetStaticEditorFlags(newGO, StaticEditorFlags.NavigationStatic);
                            allPoints.Add(candidate);
                            spawnPoints.Add(candidate);
                            indexMap[(int)(candidate.x / cellSize), (int)(candidate.y / cellSize)] = allPoints.Count;
                            candidateAccepted = true;
                            break;
                        }
                    }
                    #endregion
                }
                if (!candidateAccepted)
                {
                    spawnPoints.RemoveAt(spawnIndex);
                    if (spawnPoints.Count == 0 && repeatCount > 0)
                    {
                        spawnPoints.Add(new Vector2(Random.Range(0, regionSize.x), Random.Range(0, regionSize.y)));
                        repeatCount--;
                    }
                    else
                    {
                        repeatCount = repeat;
                    }
                }
            }
        }
    }
    static bool IsValid(Vector2 candidate, Vector2 regionSize, float cellSize, float radius, List<Vector2> points, int[,] map)
    {
        if (candidate.x >= 0 && candidate.x < regionSize.x && candidate.y >= 0 && candidate.y < regionSize.y)
        {
            int cellX = (int)(candidate.x / cellSize);
            int cellY = (int)(candidate.y / cellSize);
            int searchStartX = Mathf.Max(0, cellX - 5);
            int searchEndX = Mathf.Min(cellX + 5, map.GetLength(0) - 1);
            int searchStartY = Mathf.Max(0, cellY - 5);
            int searchEndY = Mathf.Min(cellY + 5, map.GetLength(1) - 1);
            searchStartX = 0;
            searchEndX = map.GetLength(0) - 1;
            searchStartY = 0;
            searchEndY = map.GetLength(1) - 1;
            for (int x = searchStartX; x <= searchEndX; x++)
            {
                for (int y = searchStartY; y <= searchEndY; y++)
                {
                    int pointIndex = map[x, y] - 1;
                    if (pointIndex > -1)
                    {
                        float sqrDst = (candidate - points[pointIndex]).sqrMagnitude;
                        if (sqrDst < radius * radius)
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }
        return false;
    }
}
