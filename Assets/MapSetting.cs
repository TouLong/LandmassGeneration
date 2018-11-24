using UnityEngine;
using System.Linq;

[CreateAssetMenu()]
public class MapSetting : UpdatebleData
{
    public Noise.NormalizeMode normalizeMode;
    public bool useFlatShading;
    public bool useFalloff;

    [Range(10, 100)]
    public int noiseScale = 10;
    [Range(1, 5)]
    public int octaves;
    [Range(0, 1)]
    public float persistance;
    [Range(1, 3)]
    public float lacunarity;

    public int seed;
    public Vector2 offset;

    [Range(1, 10)]
    public int uniformScale = 1;
    [Range(5, 50)]
    public int meshHeightMultiplier = 5;
    public AnimationCurve meshHeightCurve;

    const int textureSize = 512;
    const TextureFormat textureFormat = TextureFormat.RGB565;

    public Layer[] layers;

    public float minHeight
    {
        get
        {
            return uniformScale * meshHeightMultiplier * meshHeightCurve.Evaluate(0);
        }
    }
    public float maxHeight
    {
        get
        {
            return uniformScale * meshHeightMultiplier * meshHeightCurve.Evaluate(1);
        }
    }

    public void ApplyToMaterial(Material material)
    {

    }

    public void UpdateMaterial(Material material)
    {
        material.SetInt("layerCount", layers.Length);
        material.SetColorArray("baseColors", layers.Select(x => x.tint).ToArray());
        material.SetFloatArray("baseStartHeights", layers.Select(x => x.startHeight).ToArray());
        material.SetFloatArray("baseBlends", layers.Select(x => x.blendStrength).ToArray());
        material.SetFloatArray("baseColorStrength", layers.Select(x => x.tintStrength).ToArray());
        material.SetFloatArray("baseTextureScales", layers.Select(x => x.textureScale).ToArray());
        material.SetFloat("minHeight", minHeight);
        material.SetFloat("maxHeight", maxHeight);
        Texture2DArray texturesArray = GenerateTextureArray(layers.Select(x => x.texture).ToArray());
        material.SetTexture("baseTextures", texturesArray);
    }

    Texture2DArray GenerateTextureArray(Texture2D[] textures)
    {
        Texture2DArray textureArray = new Texture2DArray(textureSize, textureSize, textures.Length, textureFormat, true);
        for (int i = 0; i < textures.Length; i++)
        {
            textureArray.SetPixels(textures[i].GetPixels(), i);
        }
        textureArray.Apply();
        return textureArray;
    }

    [System.Serializable]
    public class Layer
    {
        public Texture2D texture;
        public Color tint;
        [Range(0, 1)]
        public float tintStrength;
        [Range(0, 1)]
        public float startHeight;
        [Range(0, 1)]
        public float blendStrength;
        public float textureScale;
    }
}