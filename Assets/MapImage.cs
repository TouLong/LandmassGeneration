using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class MapImage
{
    public static Texture2D Generate(MapSetting setting, List<Height> chunkHegihts, int scale)
    {
        int dim = setting.mapDimension;
        int textureSize = dim * setting.chunkSideLength * scale;
        float maxHeight = chunkHegihts.Max(x => x.maxValue);

        Color[] layerColor = setting.layers.Select(x => x.color).ToArray();
        Texture2D texture = new Texture2D(textureSize, textureSize);
        texture.wrapMode = TextureWrapMode.Clamp;
        int chunkSize = setting.chunkSideLength * scale;
        Color[] colorMap = new Color[chunkSize * chunkSize];

        for (int h = 0; h < dim; h++)
        {
            for (int w = 0; w < dim; w++)
            {
                for (int y = 0; y < chunkSize; y++)
                {
                    for (int x = 0; x < chunkSize; x++)
                    {
                        for (int i = setting.layers.Count - 1; i >= 0; i--)
                        {
                            if (chunkHegihts[h * dim + w].values[x / scale, y / scale] >= setting.layers[i].height * maxHeight)
                            {
                                Color c = setting.layers[i].color;
                                colorMap[y * chunkSize + x] = new Color(c.r, c.g, c.b, 1);
                                break;
                            }
                        }
                    }
                }
                texture.SetPixels(w * chunkSize, h * chunkSize, chunkSize, chunkSize, colorMap);
            }
        }
        texture.Apply();
        return texture;
    }

}
