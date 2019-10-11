using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class MapImage
{
    public static Texture2D Generate(MapSetting setting, float[,] noise, int scale = 1)
    {
        int textureSize = (noise.GetLength(0) - 2) * setting.mapScale;

        Texture2D texture = new Texture2D(textureSize, textureSize)
        {
            wrapMode = TextureWrapMode.Clamp
        };
        Color[] colorMap = new Color[textureSize * textureSize];

        for (int x = 0; x < textureSize; x++)
        {
            for (int y = 0; y < textureSize; y++)
            {
                float height = setting.heightCurve.Evaluate(noise[x / setting.mapScale, y / setting.mapScale]);
                for (int i = setting.layers.Count - 1; i >= 0; i--)
                {
                    if (height >= setting.layers[i].height)
                    {
                        Color c = setting.layers[i].color;
                        colorMap[y * textureSize + x] = new Color(c.r, c.g, c.b, 1);
                        break;
                    }
                }
            }
        }
        texture.SetPixels(0, 0, textureSize, textureSize, colorMap);
        texture.Apply();
        return texture;
    }

}
