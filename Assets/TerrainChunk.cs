using UnityEngine;

public class TerrainChunk
{
    const float colliderGenerationDistTreshold = 5;
    public event System.Action<TerrainChunk, bool> onVisilityChanged;

    public Vector2 coord;

    GameObject meshObject;
    Vector2 center;
    Bounds bounds;
    HeightMap heightMap;

    MeshRenderer meshRenderer;
    MeshFilter meshFilter;
    MeshCollider meshCollider;

    LODInfo[] detailLevels;
    LODMesh[] lodMeshes;
    int colliderLODIndex;

    bool heightMapReceived;
    int previousLODIndex = -1;
    bool hasSetCollider;
    float maxViewDist;

    MapSetting setting;
    Transform viewer;

    public TerrainChunk(Vector2 coord, MapSetting setting, LODInfo[] detailLevels, int colliderLODIndex, Transform parent, Transform viewer, Material material)
    {
        this.coord = coord;
        this.detailLevels = detailLevels;
        this.colliderLODIndex = colliderLODIndex;
        this.setting = setting;
        this.viewer = viewer;

        center = coord * setting.meshWorldSize / setting.meshScale;
        Vector2 position = coord * setting.meshWorldSize;
        bounds = new Bounds(position, Vector2.one * setting.meshWorldSize);

        meshObject = new GameObject("Terrain Chunk");
        meshRenderer = meshObject.AddComponent<MeshRenderer>();
        meshFilter = meshObject.AddComponent<MeshFilter>();
        meshCollider = meshObject.AddComponent<MeshCollider>();
        meshRenderer.material = material;

        meshObject.transform.position = new Vector3(position.x, 0, position.y);
        meshObject.transform.parent = parent;
        SetVisble(false);

        lodMeshes = new LODMesh[detailLevels.Length];
        for (int i = 0; i < detailLevels.Length; i++)
        {
            lodMeshes[i] = new LODMesh(detailLevels[i].lod);
            lodMeshes[i].updateCallback += UpdateTerrainChunk;
            if (i == colliderLODIndex)
            {
                lodMeshes[i].updateCallback += UpdateCollisionMesh;
            }
        }
        maxViewDist = detailLevels[detailLevels.Length - 1].visibleDstThreshold;
    }

    public void Load()
    {
        ThreadedDataRequester.RequestData(() => HeightMapGenerator.GenerateHeightMap(setting, center), OnHeightMapReceived);

    }

    void OnHeightMapReceived(object heightMapObject)
    {
        this.heightMap = (HeightMap)heightMapObject;
        heightMapReceived = true;

        UpdateTerrainChunk();
    }

    Vector2 viewerPosition
    {
        get { return new Vector2(viewer.position.x, viewer.position.z); }
    }

    public void UpdateTerrainChunk()
    {
        if (!heightMapReceived) return;
        float viewerDstFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));

        bool wasVisble = IsVisible();
        bool visible = viewerDstFromNearestEdge <= maxViewDist;

        if (visible)
        {
            int lodIndex = 0;
            for (int i = 0; i < detailLevels.Length - 1; i++)
            {
                if (viewerDstFromNearestEdge > detailLevels[i].visibleDstThreshold)
                {
                    lodIndex = i + 1;
                }
                else
                {
                    break;
                }
            }

            if (lodIndex != previousLODIndex)
            {
                LODMesh lodMesh = lodMeshes[lodIndex];
                if (lodMesh.hasMesh)
                {
                    previousLODIndex = lodIndex;
                    meshFilter.mesh = lodMesh.mesh;
                }
                else if (!lodMesh.hasRequestedMesh)
                {
                    lodMesh.RequestMesh(heightMap, setting);
                }
            }

        }
        if (wasVisble != visible)
        {
            SetVisble(visible);
            if (onVisilityChanged != null)
            {
                onVisilityChanged(this, visible);
            }
        }
    }

    public void SetVisble(bool visible)
    {
        meshObject.SetActive(visible);
        
    }

    public bool IsVisible()
    {
        return meshObject.activeSelf;
    }

    public void UpdateCollisionMesh()
    {
        if (hasSetCollider) return;
        float sqrDstFromViewerToEdge = bounds.SqrDistance(viewerPosition);
        if (sqrDstFromViewerToEdge < detailLevels[colliderLODIndex].sqrVisbleDistThreshold)
        {
            if (!lodMeshes[colliderLODIndex].hasRequestedMesh)
            {
                lodMeshes[colliderLODIndex].RequestMesh(heightMap, setting);
            }
        }
        if (sqrDstFromViewerToEdge < colliderGenerationDistTreshold * colliderGenerationDistTreshold)
        {
            if (lodMeshes[colliderLODIndex].hasMesh)
            {
                meshCollider.sharedMesh = lodMeshes[colliderLODIndex].mesh;
                hasSetCollider = true;
                Debug.Log(coord * setting.meshWorldSize);
            }
        }
    }

    class LODMesh
    {
        public Mesh mesh;
        public bool hasRequestedMesh;
        public bool hasMesh;
        int lod;
        public event System.Action updateCallback;

        public LODMesh(int lod)
        {
            this.lod = lod;
        }

        void OnMeshDataReceived(object meshData)
        {
            mesh = ((MeshData)meshData).CreateMesh();
            hasMesh = true;

            updateCallback();
        }

        public void RequestMesh(HeightMap heightMap, MapSetting setting)
        {
            hasRequestedMesh = true;
            ThreadedDataRequester.RequestData(() => MeshGenerator.GenerateTerrainMesh(heightMap.values, setting, lod), OnMeshDataReceived);
        }
    }
}
