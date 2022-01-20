using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FluidField
{
    private int width;
    private int height;
    private Dictionary<int, int> particleField;
    private bool reverseStep;   // step in both directions to avoid biasing one direction

    public FluidField(int width, int height)
    {
        this.width = width;
        this.height = height;
        particleField = new Dictionary<int, int>();

        for (int i = 0; i < width * height; ++i)
            particleField.Add(i, ParticleType.EMPTY);

        reverseStep = false;
    }

    private int GetAddress(int x, int y)
    {
        return y * width + x;
    }

    private Vector2Int GetCoordinate(int i)
    {
        return new Vector2Int(i % width, i / width);
    }

    private int GetParticleType(int x, int y)
    {
        return IsOutOfBounds(x, y) ? ParticleType.SOLID : particleField[GetAddress(x, y)];
    }

    public void Init()
    {
        Vector2Int centre = new Vector2Int(width / 2, height / 2);

        for (int x = 0; x < width; ++x)
        {
            for (int y = 0; y < height; ++y)
            {
                Vector2Int v = new Vector2Int(x, y);

                if ((v - centre).magnitude < 20)
                    SetParticleType(x, y, ParticleType.FLUID);
                else
                    SetParticleType(x, y, ParticleType.EMPTY);
            }
        }
    }

    public void Init(Texture2D texture)
    {
        if (texture.width != width || texture.height != height)
        {
            Debug.LogError("Texture-FluidField size mismatch in Init()!");
            return;
        }

        for (int x = 0; x < width; ++x)
        {
            for (int y = 0; y < height; ++y)
            {
                Color color = texture.GetPixel(x, y);

                if (color.DistanceTo(Color.black) < 0.1f)
                    SetParticleType(x, y, ParticleType.SOLID);
                else if (color.DistanceTo(Color.blue) < 0.1f)
                    SetParticleType(x, y, ParticleType.FLUID);
                else
                    SetParticleType(x, y, ParticleType.EMPTY);
            }
        }
    }

    private bool IsEmpty(int x, int y)
    {
        return GetParticleType(x, y) == ParticleType.EMPTY;
    }

    private bool IsOutOfBounds(int x, int y)
    {
        return x < 0 || x > width || y < 0 || y > height;
    }

    private void MoveParticle(int x1, int y1, int x2, int y2)
    {
        if (IsEmpty(x1, y1) || !IsEmpty(x2, y2))
            return;

        SetParticleType(x2, y2, GetParticleType(x1, y1));
        SetParticleType(x1, y1, ParticleType.EMPTY);
    }

    private void SetParticleType(int x, int y, int type)
    {
        particleField[GetAddress(x, y)] = type;
    }

    public void Step()
    {
        List<Vector2Int> movePriorities = new List<Vector2Int>
        {
            new Vector2Int(0,-1),
            new Vector2Int(1,-1),
            new Vector2Int(-1,-1),
            new Vector2Int(1,0),
            new Vector2Int(-1,0),
        };

        for (int y = 0; y < height; ++y)
        {
            for (int x = reverseStep ? width - 1 : 0; reverseStep ? x >= 0 : x < width; x += reverseStep ? -1 : 1)
            {
                int p = GetParticleType(x, y);
                int c = Constants.FlowMultiplier;

                if (p == ParticleType.EMPTY || p == ParticleType.SOLID)
                    continue;

                if (IsEmpty(x, y - 1))
                {
                    MoveParticle(x, y, x, y - 1);
                }
                else
                {
                    int sign = Utils.RandomFloat() < 0.5f ? 1 : -1;

                    if (IsEmpty(x + sign, y - 1))
                    {
                        MoveParticle(x, y, x + sign, y - 1);
                    }
                    else if (IsEmpty(x - sign, y - 1))
                    {
                        MoveParticle(x, y, x - sign, y - 1);
                    }
                    else if (IsEmpty(x + sign, y))
                    {
                        int i = 1;

                        while (i < Constants.FlowMultiplier && IsEmpty(x + sign * (i + 1), y))
                            ++i;

                        MoveParticle(x, y, x + sign * i, y);
                    }
                    else if (IsEmpty(x - sign, y))
                    {
                        int i = 1;

                        while (i < Constants.FlowMultiplier && IsEmpty(x - sign * (i + 1), y))
                            ++i;

                        MoveParticle(x, y, x - sign * i, y);
                    }
                }
            }
        }

        reverseStep = !reverseStep;
    }

    public void WriteTexture(Texture2D texture)
    {
        if (texture.width != width || texture.height != height)
        {
            Debug.LogError("Texture-FluidField size mismatch in WriteTexture()!");
            return;
        }

        Vector2Int v;
        Color c;

        foreach (var kv in particleField)
        {
            v = GetCoordinate(kv.Key);

            if (kv.Value == ParticleType.SOLID)
                c = Color.black;
            else if (kv.Value == ParticleType.FLUID)
                c = Color.blue;
            else
                c = Color.white;

            texture.SetPixel(v.x, v.y, c);
        }

        texture.Apply();
    }
}

public static class ParticleType
{
    public static readonly int EMPTY = 0;
    public static readonly int SOLID = 1;
    public static readonly int FLUID = 2;
}