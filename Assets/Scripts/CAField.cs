using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CAField
{
    private int width;
    private int height;
    private List<CAType> nodes;
    private HashSet<int> changedNodes;
    private List<int> sourceNodes, sinkNodes;
    private bool reverseStep;   // step in both directions to avoid biasing one direction
    private CAType emptyNode, solidNode, fluidNode;

    public CAField(int width, int height)
    {
        emptyNode = new CAType(CAPhysicsType.NONE);
        solidNode = new CAType(CAPhysicsType.NONE);
        fluidNode = new CAType(CAPhysicsType.FLOW);

        this.width = width;
        this.height = height;
        nodes = new List<CAType>();

        for (int i = 0; i < width * height; ++i)
            nodes.Add(emptyNode);

        changedNodes = new HashSet<int>();
        sourceNodes = new List<int>();
        sinkNodes = new List<int>();
        reverseStep = false;
    }

    public CAField(Texture2D texture) : this(texture.width, texture.height)
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

    private CAType GetNodeType(int x, int y)
    {
        return IsOutOfBounds(x, y) ? solidNode : nodes[GetAddress(x, y)];
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
                    SetNodeType(x, y, solidNode);
                else if (color.DistanceTo(Color.blue) < 0.1f)
                    SetNodeType(x, y, fluidNode);
                else if (color.DistanceTo(Color.green) < 0.1f)
                    sourceNodes.Add(GetAddress(x, y));
                else if (color.DistanceTo(Color.red) < 0.1f)
                    sinkNodes.Add(GetAddress(x, y));
                else
                    SetNodeType(x, y, emptyNode);
            }
        }
    }

    private bool IsEmpty(int x, int y)
    {
        return GetNodeType(x, y) == emptyNode;
    }

    private bool IsOutOfBounds(int x, int y)
    {
        return x < 0 || x > width || y < 0 || y > height;
    }

    private void MoveNode(int x1, int y1, int x2, int y2)
    {
        if (IsEmpty(x1, y1) || !IsEmpty(x2, y2))
            return;

        SetNodeType(x2, y2, GetNodeType(x1, y1));
        SetNodeType(x1, y1, emptyNode);
    }

    private void SetNodeType(int i, CAType type)
    {
        nodes[i] = type;
        changedNodes.Add(i);
    }

    private void SetNodeType(int x, int y, CAType type)
    {
        SetNodeType(GetAddress(x, y), type);
    }

    public void Step()
    {
        for (int y = 0; y < height; ++y)
        {
            for (int x = reverseStep ? width - 1 : 0; reverseStep ? x >= 0 : x < width; x += reverseStep ? -1 : 1)
            {
                CAType p = GetNodeType(x, y);

                if (p.PhysicsType == CAPhysicsType.NONE)
                    continue;

                if (IsEmpty(x, y - 1))
                {
                    MoveNode(x, y, x, y - 1);
                }
                else
                {
                    int sign = Utils.RandomFloat() < 0.5f ? 1 : -1;

                    if (IsEmpty(x + sign, y - 1))
                    {
                        MoveNode(x, y, x + sign, y - 1);
                    }
                    else if (IsEmpty(x - sign, y - 1))
                    {
                        MoveNode(x, y, x - sign, y - 1);
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

                        MoveNode(x, y, x + sign * i, y);
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

                        MoveNode(x, y, x - sign * i, y);
                    }
                }
            }
        }

        foreach (int i in sourceNodes)
            SetNodeType(i, fluidNode);

        foreach (int i in sinkNodes)
            SetNodeType(i, emptyNode);

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

            if (nodes[i] == solidNode)
                c = Color.black;
            else if (nodes[i] == fluidNode)
                c = Color.blue;
            else
                c = Color.white;

            texture.SetPixel(v.x, v.y, c);
        }

        texture.Apply();
        changedNodes.Clear();
    }
}

public class CAType
{
    public int PhysicsType { get; private set; }

    public CAType(int physicsType)
    {
        PhysicsType = physicsType;
    }
}

public static class CAPhysicsType
{
    public static readonly int NONE = 0;
    public static readonly int FALL = 1;
    public static readonly int PILE = 2;
    public static readonly int FLOW = 3;
}