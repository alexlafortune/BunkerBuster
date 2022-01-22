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
    }

    public CAField(Texture2D texture) : this(texture.width, texture.height)
    {
        Init(texture);
    }

    public void AddSourceNode(int x, int y)
    {
        sourceNodes.Add(GetAddress(x, y));
    }

    private CANode FindNearestFreeVolumeNodeBelow(int x, int y)
    {
        int distance = 0;
        HashSet<Vector2Int> toBeChecked = new HashSet<Vector2Int>();
        HashSet<Vector2Int> checkedCoords = new HashSet<Vector2Int>();
        List<CANode> nearbyFreeVolumeNodes = new List<CANode>();
        Vector2Int[] neighbours = new Vector2Int[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        toBeChecked.Add(new Vector2Int(x, y));

        while (nearbyFreeVolumeNodes.Count == 0)
        {
            HashSet<Vector2Int> nextLoopToBeChecked = new HashSet<Vector2Int>();

            foreach (Vector2Int v in toBeChecked)
            {
                checkedCoords.Add(v);
                CANode node = GetNodeAt(v);

                if (node.FreeVolume > 0 && v.y < y)
                {
                    nearbyFreeVolumeNodes.Add(node);
                }
                else if (node.FreeVolume == 0)
                {
                    foreach (Vector2Int n in neighbours)
                    {
                        Vector2Int vn = v + n;

                        if (!IsOutOfBounds(vn) && GetNodeAt(vn).Type == fluidType
                            && !checkedCoords.Contains(vn))
                            nextLoopToBeChecked.Add(vn);
                    }
                }
            }

            toBeChecked = nextLoopToBeChecked;
            ++distance;

            if (toBeChecked.Count == 0 && nearbyFreeVolumeNodes.Count == 0)
                return null;
        }

        return (from n in nearbyFreeVolumeNodes orderby n.Fluid select n).First();
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

    private CANode GetNodeAt(Vector2Int v)
    {
        return GetNodeAt(v.x, v.y);
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

    private bool IsEmpty(int x, int y)
    {
        return GetNodeAt(x, y).Fluid == 0;
    }

    private bool IsOutOfBounds(int x, int y)
    {
        return x < 0 || x >= width || y < 0 || y >= height;
    }

    private bool IsOutOfBounds(Vector2Int v)
    {
        return IsOutOfBounds(v.x, v.y);
    }

    private void MoveFluid(CANode source, CANode dest, int volume = 0)
    {
        if (volume < 0)
            return;
        else if (volume == 0)
            volume = source.Fluid;  // if no volume specified, attempt to move all of it

        volume = Mathf.Min(new int[] { source.Fluid, dest.FreeVolume, volume });

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

        CANode downNode = GetNodeAt(x, y - 1);
        CANode leftNode = GetNodeAt(x - 1, y);
        CANode rightNode = GetNodeAt(x + 1, y);

        if (downNode != null)
        {
            if (downNode.FreeVolume > 0)
            {
                MoveFluid(thisNode, downNode);
            }
            else
            {
                CANode nearestNonfilledNodeBelow = FindNearestFreeVolumeNodeBelow(x, y);

                if (nearestNonfilledNodeBelow != null)
                    MoveFluid(thisNode, nearestNonfilledNodeBelow);
            }
        }

        if (leftNode != null && thisNode.Fluid > leftNode.Fluid)
        {
            MoveFluid(thisNode, leftNode, (thisNode.Fluid - leftNode.Fluid) / 2 + 1);
        }

        if (rightNode != null && thisNode.Fluid > rightNode.Fluid)
        {
            MoveFluid(thisNode, rightNode, (thisNode.Fluid - rightNode.Fluid) / 2 + 1);
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
                c = new Color(0, 0, Mathf.Lerp(1f, 0.2f, nodes[i].Fluid / CANode.MaxCapacity));
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

    public int Fluid;
    private int capacity;
    public int FreeVolume { get => capacity - Fluid; }
    public static readonly int MaxCapacity = 10;

    public CANode(int address, Vector2Int coord, CAType type)
    {
        Address = address;
        Coordinate = coord;
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