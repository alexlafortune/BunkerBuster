using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct NoiseProfile
{
    [Range(0, 1)]
    public float frequency;

    [Range(0, 10)]
    public int octaves;

    [Range(0, 1)]
    public float persistence;

    [Range(0, 10)]
    public float lacunarity;

    public NoiseProfile(float frequency, int octaves, float persistence, float lacunarity)
    {
        this.frequency = frequency;
        this.octaves = octaves;
        this.persistence = persistence;
        this.lacunarity = lacunarity;
    }
}

public static class Noise
{
    public static float GetNoise1D(float x, NoiseProfile np, float seed)
    {
        float total = 0;
        float frequency = 1;
        float amplitude = 1;
        float maxValue = 0;
        float n;

        for (int i = 0; i < np.octaves; ++i)
        {
            n = Mathf.PerlinNoise(x * np.frequency * frequency + seed, 1 * np.frequency * frequency + seed);
            total += amplitude * n;
            maxValue += amplitude;
            frequency *= np.lacunarity;
            amplitude *= np.persistence;
        }

        return Mathf.Clamp(total / maxValue, 0, 1);
    }
}