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
            nodes.Add(new CANode(i, GetCoordinate(i), fluidType));

        changedNodes = new HashSet<int>();
        sourceNodes = new List<int>();
        sinkNodes = new List<int>();
        reverseStep = false;

        for (int i = 0; i < nodes.Count; ++i)   // ensure full refresh on first iteration
            changedNodes.Add(i);
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

    public Vector2Int GetCoordinate(int i)
    {
        return new Vector2Int(i % width, i / width);
    }

    private CANode GetNodeAt(int x, int y)
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
                    nodes[i] = new CANode(i, GetCoordinate(i), solidType);
                }
                else if (color.DistanceTo(Color.blue) < 0.1f)
                {
                    nodes[i] = new CANode(i, GetCoordinate(i), fluidType);
                    nodes[i].Fluid = CANode.MaxCapacity;
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
                    nodes[i] = new CANode(i, GetCoordinate(i), fluidType);
                }
            }
        }
    }

    private bool IsOutOfBounds(int x, int y)
    {
        return x < 0 || x >= width || y < 0 || y >= height;
    }

    private void MoveFluid(CANode source, CANode dest, float volume)
    {
        if (volume < 0)
            return;

        volume = Mathf.Min(new float[] { source.Fluid, volume });

        source.Fluid -= volume;
        dest.Fluid += volume;

        changedNodes.Add(source.Address);
        changedNodes.Add(dest.Address);
    }

    public void Step()
    {
        // Update CA nodes

        for (int y = 0; y < height; ++y)
            for (int x = reverseStep ? width - 1 : 0; reverseStep ? x >= 0 : x < width; x += reverseStep ? -1 : 1)
                StepNode(x, y);

        // Update sources and sinks

        foreach (int i in sourceNodes)
            nodes[i].Fluid = CANode.MaxCapacity;

        foreach (int i in sinkNodes)
            nodes[i].Fluid = 0;

        reverseStep = !reverseStep;
    }

    private void StepNode(int x, int y)
    {
        CANode thisNode = GetNodeAt(x, y);
        CAType p = thisNode.Type;

        if (p.PhysicsType == CAPhysicsType.NONE || thisNode.Fluid == 0)
            return;

        CANode upNode = GetNodeAt(x, y + 1);
        CANode downNode = GetNodeAt(x, y - 1);
        CANode leftNode = GetNodeAt(x - 1, y);
        CANode rightNode = GetNodeAt(x + 1, y);
        float flow;

        thisNode.LastChange = Vector2.zero;

        if (downNode != null && downNode.Type == fluidType)
        {
            if (downNode.FreeVolume > 0)
            {
                flow = downNode.FreeVolume + CANode.MaxPressure;
                MoveFluid(thisNode, downNode, flow);
                thisNode.LastChange.y -= flow;
                downNode.LastChange.y += flow;
            }
            else if (thisNode.Fluid + CANode.MaxPressure > downNode.Fluid)
            {
                flow = (thisNode.Fluid - downNode.Fluid + CANode.MaxPressure) / 2;
                MoveFluid(thisNode, downNode, flow);
                thisNode.LastChange.y -= flow;
                downNode.LastChange.y += flow;
            }
        }

        CANode sideNode = reverseStep ? rightNode : leftNode;

        for (int i = 0; i < 2; ++i)
        {
            if (sideNode != null && sideNode.Type == fluidType && thisNode.Fluid > sideNode.Fluid)
            {
                flow = (thisNode.Fluid - sideNode.Fluid) / 2;
                MoveFluid(thisNode, sideNode, flow);
                thisNode.LastChange.x -= flow;
                sideNode.LastChange.x += flow;
            }

            sideNode = sideNode == leftNode ? rightNode : leftNode; // switch sides
        }

        if (upNode != null && upNode.Type == fluidType && thisNode.FreeVolume < 0)
        {
            if (thisNode.Fluid - CANode.MaxPressure > upNode.Fluid)
            {
                flow = (thisNode.Fluid - upNode.Fluid - CANode.MaxPressure) / 2;
                MoveFluid(thisNode, upNode, flow);
                thisNode.LastChange.y -= flow;
                upNode.LastChange.y += flow;
            }
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

        foreach (int i in changedNodes)
        {
            v = GetCoordinate(i);

            if (nodes[i].Type == solidType)
                c = Color.grey;
            else if (nodes[i].Type == fluidType && nodes[i].Fluid > 0)
                c = new Color(nodes[i].Fluid - CANode.MaxCapacity, 0, Mathf.Lerp(1f, 0.1f, nodes[i].Fluid / CANode.MaxCapacity));
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
    public int Address { get; private set; }
    public Vector2Int Coordinate { get; private set; }
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
                capacity = MaxCapacity;
        }
    }

    public float Fluid;
    public Vector2 LastChange;
    private float capacity;
    public float FreeVolume { get => capacity - Fluid; }
    public static readonly float MaxCapacity = 1;
    public static readonly float MaxPressure = 0.01f;

    public CANode(int address, Vector2Int coord, CAType type)
    {
        Address = address;
        Coordinate = coord;
        Type = type;
        Fluid = 0;
        LastChange = Vector2.zero;
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