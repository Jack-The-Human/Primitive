using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextureGenerator
{
    public static Texture2D TextureFromColormap(Color[] colorMap, int width, int height)
    {
        Texture2D texture = new Texture2D(width, height);
        // changes blurriness of the texture (trilinear, bilinear, point)
        texture.filterMode = FilterMode.Trilinear;
        //prevents texture looping
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels(colorMap);
        texture.Apply();
        return texture;
    }

    public static Texture2D TextureFromHeightmap(HeightMap heightMap)
    {
        int width = heightMap.values.GetLength (0);
        int height = heightMap.values.GetLength (1);

        Color[] colorMap = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                colorMap[y * width + x] = Color.Lerp(Color.black, Color.white, Mathf.InverseLerp(heightMap.minValue, heightMap.maxValue, heightMap.values[x,y]));
            }
        }
        return TextureFromColormap(colorMap, width, height);
    }
}
