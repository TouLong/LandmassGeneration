using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndlessTerrain : MonoBehaviour {

    const float scale = 1;
    const float viewerMoveTreshold = 25f;
    const float sqrViewerMoveTreshold = viewerMoveTreshold * viewerMoveTreshold;

    public LODInfo[] detailOfLevels;
    public static float maxViewDst;

    public Transform viewer;
    public Material mapMaterial;

    public static Vector2 viewPosition;
    Vector2 viewPosittionOld;
    static MapGenerator mapGenerator;
    int chunkSize;
    int chunksVisibleInViewDst;

    Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    List<TerrainChunk> terrainChunkList = new List<TerrainChunk>();

    void Start()
    {
        mapGenerator = GetComponent<MapGenerator>();
        maxViewDst = detailOfLevels[detailOfLevels.Length - 1].visibleDstThreshold;
        chunkSize = MapGenerator.mapChunkSize - 1;
        chunksVisibleInViewDst = Mathf.RoundToInt(maxViewDst / chunkSize);
        UpdateVisibleChunks();
    }

    void Update()
    {
        viewPosition = new Vector2(viewer.position.x, viewer.position.z) / scale;
        if ((viewPosittionOld - viewPosition).sqrMagnitude > sqrViewerMoveTreshold)
        {
            viewPosittionOld = viewPosition;
            UpdateVisibleChunks();
        }
    }

    void UpdateVisibleChunks()
    {
        for(int i = 0; i < terrainChunkList.Count; i++)
        {
            terrainChunkList[i].SetVisble(false);
        }
        int currentChunkCoordX = Mathf.RoundToInt(viewPosition.x / chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewPosition.y / chunkSize);

        for(int yOffset = -chunksVisibleInViewDst; yOffset <= chunksVisibleInViewDst; yOffset++)
        {
            for (int xOffset = -chunksVisibleInViewDst; xOffset <= chunksVisibleInViewDst; xOffset++)
            {
                Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);
                if (terrainChunkDictionary.ContainsKey(viewedChunkCoord))
                {
                    terrainChunkDictionary[viewedChunkCoord].UpdateTerrainChunk();
                }
                else
                {
                    terrainChunkDictionary.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, chunkSize, detailOfLevels, transform, mapMaterial));
                    terrainChunkList.Add(terrainChunkDictionary[viewedChunkCoord]);
                }
            }

        }
    }

    public class TerrainChunk
    {
        GameObject meshObject;
        Vector2 position;
        Bounds bounds;
        MapData mapData;

        MeshRenderer meshRenderer;
        MeshFilter meshFilter;

        LODInfo[] detailLevels;
        LODMesh[] lodMeshes;

        bool mapDataReceived;
        int previousLODIndex = -1;

        public TerrainChunk(Vector2 coord, int size, LODInfo[] detailLevels, Transform parent, Material material)
        {
            position = coord * size;
            this.detailLevels = detailLevels;
            bounds = new Bounds(position, Vector2.one * size);
            Vector3 positionV3 = new Vector3(position.x, 0, position.y);

            meshObject = new GameObject("Terrain Chunk");
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshFilter = meshObject.AddComponent<MeshFilter>();
            meshRenderer.material = material;

            meshObject.transform.position = positionV3 * scale;
            meshObject.transform.parent = parent;
            meshObject.transform.localScale = Vector3.one * scale;
            SetVisble(false);

            lodMeshes = new LODMesh[detailLevels.Length];
            for(int i = 0; i < detailLevels.Length; i++)
            {
                lodMeshes[i] = new LODMesh(detailLevels[i].lod, UpdateTerrainChunk);
            }

            mapGenerator.RequestMapData(position, OnMapDataReceived);
        }

        void OnMapDataReceived(MapData mapData)
        {
            this.mapData = mapData;
            mapDataReceived = true;

            Texture2D texture = TextureGenerator.TextureFromColorMap(mapData.colorMap, MapGenerator.mapChunkSize, MapGenerator.mapChunkSize);
            meshRenderer.material.mainTexture = texture;

            UpdateTerrainChunk();
        }


        public void UpdateTerrainChunk()
        {
            if (!mapDataReceived) return;
            float viewerDstFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewPosition));
            bool visible = viewerDstFromNearestEdge <= maxViewDst;

            if (visible)
            {
                int lodIndex = 0;
                for(int i = 0; i < detailLevels.Length - 1; i++)
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
                        lodMesh.RequestMesh(mapData);

                    }
                }
            }
            SetVisble(visible);
        }
        public void SetVisble(bool visible)
        {
            meshObject.SetActive(visible);
        }
        public bool IsVisible()
        {
            return meshObject.activeSelf;
        }
    }

    class LODMesh
    {
        public Mesh mesh;
        public bool hasRequestedMesh;
        public bool hasMesh;
        int lod;
        System.Action updateCallback;

        public LODMesh(int lod, System.Action updateCallback)
        {
            this.lod = lod;
            this.updateCallback = updateCallback;
        }

        void OnMeshDataReceived(MeshData meshData)
        {
            mesh = meshData.CreateMesh();
            hasMesh = true;

            updateCallback();
        }

        public void  RequestMesh(MapData mapData)
        {
            hasRequestedMesh = true;
            mapGenerator.RequestMeshData(mapData, lod, OnMeshDataReceived);
        }
    }

    [System.Serializable]
    public struct LODInfo
    {
        public int lod;
        public float visibleDstThreshold;
    }
}
