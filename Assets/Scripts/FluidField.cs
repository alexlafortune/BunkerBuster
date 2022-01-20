using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FluidField
{
    private int width;
    private int height;
    private List<int> nodes;
    private HashSet<int> changedNodes;
    private List<int> sourceNodes;
    private List<int> sinkNodes;
    private bool reverseStep;   // step in both directions to avoid biasing one direction

    public FluidField(int width, int height)
    {
        this.width = width;
        this.height = height;
        nodes = new List<int>();

        for (int i = 0; i < width * height; ++i)
            nodes.Add(ParticleType.EMPTY);

        changedNodes = new HashSet<int>();
        sourceNodes = new List<int>();
        sinkNodes = new List<int>();
        reverseStep = false;
    }

    public FluidField(Texture2D texture) : this(texture.width, texture.height)
    {
        Init(texture);
    }

    public void AddSourceNode(int x, int y)
    {
        sourceNodes.Add(GetAddress(x, y));
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
        return IsOutOfBounds(x, y) ? ParticleType.SOLID : nodes[GetAddress(x, y)];
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
                else if (color.DistanceTo(Color.green) < 0.1f)
                    sourceNodes.Add(GetAddress(x, y));
                else if (color.DistanceTo(Color.red) < 0.1f)
                    sinkNodes.Add(GetAddress(x, y));
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

    private void SetParticleType(int i, int type)
    {
        nodes[i] = type;
        changedNodes.Add(i);
    }

    private void SetParticleType(int x, int y, int type)
    {
        SetParticleType(GetAddress(x, y), type);
    }

    public void Step()
    {
        for (int y = 0; y < height; ++y)
        {
            for (int x = reverseStep ? width - 1 : 0; reverseStep ? x >= 0 : x < width; x += reverseStep ? -1 : 1)
            {
                int p = GetParticleType(x, y);

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

                        while (i < Constants.FlowMultiplier)    // search up to FlowMultiplier spaces in either direction
                        {
                            if (!IsEmpty(x + sign * (i + 1), y) || IsEmpty(x + sign * (i + 1), y - 1))
                                break;  // move the particle along the surface until we hit another particle or the particle has an open space below

                            ++i;
                        }

                        MoveParticle(x, y, x + sign * i, y);
                    }
                    else if (IsEmpty(x - sign, y))
                    {
                        int i = 1;

                        while (i < Constants.FlowMultiplier)
                        {
                            if (!IsEmpty(x - sign * (i + 1), y) || IsEmpty(x - sign * (i + 1), y - 1))
                                break;

                            ++i;
                        }

                        MoveParticle(x, y, x - sign * i, y);
                    }
                }
            }
        }

        foreach (int i in sourceNodes)
            SetParticleType(i, ParticleType.FLUID);

        foreach (int i in sinkNodes)
            SetParticleType(i, ParticleType.EMPTY);

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

        foreach (int i in changedNodes)
        {
            v = GetCoordinate(i);

            if (nodes[i] == ParticleType.SOLID)
                c = Color.black;
            else if (nodes[i] == ParticleType.FLUID)
                c = Color.blue;
            else
                c = Color.white;

            texture.SetPixel(v.x, v.y, c);
        }

        texture.Apply();
        changedNodes.Clear();
    }
}

public static class ParticleType
{
    public static readonly int EMPTY = 0;
    public static readonly int SOLID = 1;
    public static readonly int FLUID = 2;
}