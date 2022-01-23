using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TerrainGenerator
{
    public static Texture2D Generate(int width, int height, int groundLevel, float amplitude, NoiseProfile np)
    {
        Texture2D texture = new Texture2D(width, height);
        float seed = Utils.RandomFloat() * 1000f;

        for (int x = 0; x < width; ++x)
        {
            for (int y = 0; y < height; ++y)
            {
                float level = groundLevel + amplitude * Noise.GetNoise1D(x, np, seed);

                if (y < level)
                    texture.SetPixel(x, y, Color.black);
                else
                    texture.SetPixel(x, y, Color.white);
            }
        }

        return texture;
    }
}