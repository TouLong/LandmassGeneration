using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour {

    const float viewerMoveTreshold = 25f;
    const float sqrViewerMoveTreshold = viewerMoveTreshold * viewerMoveTreshold;

    public int colliderLODIndex;
    public LODInfo[] detailOfLevels;

    public MapSetting setting;

    public Transform viewer;
    public Material mapMaterial;

    Vector2 viewerPosittion;
    Vector2 viewerPosittionOld;

    float meshWolrdSize;
    int chunksVisibleInViewDst;

    Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    List<TerrainChunk> terrainChunkList = new List<TerrainChunk>();

    void Start()
    {
        setting.UpdateMaterial(mapMaterial);
        float maxViewDist = detailOfLevels[detailOfLevels.Length - 1].visibleDstThreshold;
        meshWolrdSize = setting.meshWorldSize;
        chunksVisibleInViewDst = Mathf.RoundToInt(maxViewDist / meshWolrdSize);
        UpdateVisibleChunks();
    }

    void Update()
    {
        viewerPosittion = new Vector2(viewer.position.x, viewer.position.z);

        if (viewerPosittion != viewerPosittionOld)
        {
            foreach(TerrainChunk chunk in terrainChunkList)
            {
                chunk.UpdateCollisionMesh();
            }
        }

        if ((viewerPosittionOld - viewerPosittion).sqrMagnitude > sqrViewerMoveTreshold)
        {
            viewerPosittionOld = viewerPosittion;
            UpdateVisibleChunks();
        }
    }

    void UpdateVisibleChunks()
    {
        HashSet<Vector2> alreadyUpdatedChunkCoords = new HashSet<Vector2>();
        for (int i = terrainChunkList.Count - 1; i >= 0; i--)
        {
            alreadyUpdatedChunkCoords.Add(terrainChunkList[i].coord);
            terrainChunkList[i].UpdateTerrainChunk();
        }

        int currentChunkCoordX = Mathf.RoundToInt(viewerPosittion.x / meshWolrdSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPosittion.y / meshWolrdSize);

        for(int yOffset = -chunksVisibleInViewDst; yOffset <= chunksVisibleInViewDst; yOffset++)
        {
            for (int xOffset = -chunksVisibleInViewDst; xOffset <= chunksVisibleInViewDst; xOffset++)
            {
                Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);
                if (!alreadyUpdatedChunkCoords.Contains(viewedChunkCoord))
                {
                    if (terrainChunkDictionary.ContainsKey(viewedChunkCoord))
                    {
                        terrainChunkDictionary[viewedChunkCoord].UpdateTerrainChunk();
                    }
                    else
                    {
                        TerrainChunk newChunk = new TerrainChunk(viewedChunkCoord, setting, detailOfLevels, colliderLODIndex, transform, viewer, mapMaterial);
                        terrainChunkDictionary.Add(viewedChunkCoord, newChunk);
                        newChunk.onVisilityChanged += OnTerrainChunkVisiblilityChanged;
                        newChunk.Load();
                    }
                }
            }

        }
    }

    void OnTerrainChunkVisiblilityChanged(TerrainChunk chunk,bool isVisible)
    {
        if (isVisible) terrainChunkList.Add(chunk);
        else terrainChunkList.Remove(chunk);
    }

}

[System.Serializable]
public struct LODInfo
{
    [Range(0, MapSetting.numSupportedLODs - 1)]
    public int lod;
    public float visibleDstThreshold;
    public float sqrVisbleDistThreshold
    {
        get
        {
            return visibleDstThreshold * visibleDstThreshold;
        }
    }
}