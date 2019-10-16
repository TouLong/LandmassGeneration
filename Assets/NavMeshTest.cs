using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
[ExecuteInEditMode]
public class NavMeshTest : MonoBehaviour
{
    public Transform charater;
    public Transform target;
    private NavMeshHit hit;
    NavMeshPath path;
    private bool blocked = false;
    void Start()
    {
    }

    void Update()
    {
        Triangle();
    }

    void Triangle()
    {
        NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
        Vector3[] vs = triangulation.vertices;
        int[] indices = triangulation.indices;
        int[] areas = triangulation.areas;
        for (int i = 0; i < indices.Length;)
        {
            int area = areas[i / 3];
            Random.InitState(area);
            Color color = new Color(Random.value, Random.value, Random.value);
            int index1 = indices[i++];
            int index2 = indices[i++];
            int index3 = indices[i++];
            Debug.DrawLine(vs[index1], vs[index2], color);
            Debug.DrawLine(vs[index2], vs[index3], color);
            Debug.DrawLine(vs[index3], vs[index1], color);
        }
    }
    void Path()
    {
        path = new NavMeshPath();
        NavMesh.CalculatePath(charater.position, target.position, NavMesh.AllAreas, path);
        for (int i = 0; i < path.corners.Length - 1; i++)
            Debug.DrawLine(path.corners[i], path.corners[i + 1], Color.red);
    }
    void Raycast()
    {
        blocked = NavMesh.Raycast(charater.position, target.position, out hit, NavMesh.AllAreas);
        Debug.DrawLine(charater.position, target.position, blocked ? Color.red : Color.green);
        if (blocked)
            Debug.DrawRay(hit.position, Vector3.up, Color.red);
    }
}
