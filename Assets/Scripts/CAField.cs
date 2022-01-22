using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class CAField
{
    private int width;
    private int height;
    private List<CANode> nodes;
    private HashSet<int> changedNodes;
    private List<int> sourceNodes, sinkNodes;
    private bool reverseStep;   // step in both directions to avoid biasing one direction
    private CAType solidType, fluidType;

    public CAField(int width, int height)
    {
        solidType = new CAType(CAPhysicsType.NONE);
        fluidType = new CAType(CAPhysicsType.FLOW);

        this.width = width;
        this.height = height;
        nodes = new List<CANode>();

        for (int i = 0; i < width * height; ++i)
            nodes.Add(new CANode(fluidType));

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

    private CANode GetNode(int x, int y)
    {
        return IsOutOfBounds(x, y) ? null : nodes[GetAddress(x, y)];
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
                int i = GetAddress(x, y);

                if (color.DistanceTo(Color.black) < 0.1f)
                {
                    nodes[i] = new CANode(solidType);
                }
                else if (color.DistanceTo(Color.blue) < 0.1f)
                {
                    nodes[i] = new CANode(fluidType);
                    nodes[i].Fluid = 10;
                }
                else if (color.DistanceTo(Color.green) < 0.1f)
                {
                    sourceNodes.Add(GetAddress(x, y));
                }
                else if (color.DistanceTo(Color.red) < 0.1f)
                {
                    sinkNodes.Add(GetAddress(x, y));
                }
                else
                {
                    nodes[i] = new CANode(fluidType);
                }
            }
        }
    }

    private bool IsEmpty(int x, int y)
    {
        return GetNode(x, y).Fluid == 0;
    }

    private bool IsOutOfBounds(int x, int y)
    {
        return x < 0 || x >= width || y < 0 || y >= height;
    }

    private void MoveFluid(CANode source, CANode dest, int volume = 10) // default: move as much fluid as possible
    {
        if (volume <= 0)
            return;

        int[] volumes = new int[] { source.Fluid, dest.FreeVolume, volume };
        volume = Mathf.Min(volumes);

        source.Fluid -= volume;
        dest.Fluid += volume;
    }

    public void Step()
    {
        // Update CA nodes

        for (int y = 0; y < height; ++y)
            for (int x = reverseStep ? width - 1 : 0; reverseStep ? x >= 0 : x < width; x += reverseStep ? -1 : 1)
                StepNode(x, y);

        // Update sources and sinks

        foreach (int i in sourceNodes)
            nodes[i].Fluid = 10;

        foreach (int i in sinkNodes)
            nodes[i].Fluid = 0;

        reverseStep = !reverseStep;
    }

    private void StepNode(int x, int y)
    {
        CANode thisNode = GetNode(x, y);
        CAType p = thisNode.Type;

        if (p.PhysicsType == CAPhysicsType.NONE || thisNode.Fluid == 0)
            return;

        CANode downNode = GetNode(x, y - 1);
        CANode leftNode = GetNode(x - 1, y);
        CANode rightNode = GetNode(x + 1, y);

        if (downNode != null && downNode.FreeVolume > 0)
        {
            MoveFluid(thisNode, downNode);
            changedNodes.Add(GetAddress(x, y));
            changedNodes.Add(GetAddress(x, y - 1));
        }

        if (leftNode != null && thisNode.Fluid > leftNode.Fluid)
        {
            MoveFluid(thisNode, leftNode, Mathf.Max((thisNode.Fluid - leftNode.Fluid) / 2 + 1, 1));
            changedNodes.Add(GetAddress(x, y));
            changedNodes.Add(GetAddress(x - 1, y));
        }

        if (rightNode != null && thisNode.Fluid > rightNode.Fluid)
        {
            MoveFluid(thisNode, rightNode, Mathf.Max((thisNode.Fluid - rightNode.Fluid) / 2 + 1, 1));
            changedNodes.Add(GetAddress(x, y));
            changedNodes.Add(GetAddress(x + 1, y));
        }
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

        for (int i = 0; i < nodes.Count; ++i)
            changedNodes.Add(i);

        foreach (int i in changedNodes)
        {
            v = GetCoordinate(i);

            if (nodes[i].Type == solidType)
                c = Color.grey;
            else if (nodes[i].Type == fluidType && nodes[i].Fluid > 0)
                c = new Color(Mathf.Lerp(0f, 1f, nodes[i].FreeVolume / 10f), Mathf.Lerp(0f, 1f, nodes[i].FreeVolume / 10f), 1);
            else
                c = Color.white;

            texture.SetPixel(v.x, v.y, c);
        }

        texture.Apply();
        changedNodes.Clear();
    }
}

public class CANode
{
    private CAType _type;

    public CAType Type
    {
        get
        {
            return _type;
        }
        set
        {
            _type = value;

            if (_type.PhysicsType == CAPhysicsType.NONE)
                capacity = 0;
            else
                capacity = 10;
        }
    }

    public int Fluid;
    private int capacity;
    public int FreeVolume { get => capacity - Fluid; }

    public CANode(CAType type)
    {
        Type = type;
        Fluid = 0;
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