using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class CAField
{
    public int Width { get; private set; }
    public int Height { get; private set; }
    private List<CANode> nodes;
    private HashSet<int> changedNodes;
    private List<int> sourceNodes, sinkNodes;
    private bool reverseStep;   // step in both directions to avoid biasing one direction

    public CAField(int width, int height)
    {
        Width = width;
        Height = height;
        nodes = new List<CANode>();

        for (int i = 0; i < width * height; ++i)
            nodes.Add(new CANode(i, GetCoordinate(i), CAType.Water));

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

    public void AddMaterial(int x, int y, CAType type, float fillAmount)
    {
        CANode node = GetNodeAt(x, y);
        node.Type = type;
        node.Fluid = fillAmount * node.Capacity;
    }

    public void AddSourceNode(int x, int y)
    {
        sourceNodes.Add(GetAddress(x, y));
    }

    private int GetAddress(int x, int y)
    {
        return y * Width + x;
    }

    public Vector2Int GetCoordinate(int i)
    {
        return new Vector2Int(i % Width, i / Width);
    }

    private CANode GetNodeAt(int x, int y)
    {
        return IsOutOfBounds(x, y) ? null : nodes[GetAddress(x, y)];
    }

    public void Init(Texture2D texture)
    {
        if (texture.width != Width || texture.height != Height)
        {
            Debug.LogError("Texture-FluidField size mismatch in Init()!");
            return;
        }

        for (int x = 0; x < Width; ++x)
        {
            for (int y = 0; y < Height; ++y)
            {
                Color color = texture.GetPixel(x, y);
                int i = GetAddress(x, y);

                if (color.ColorDistance(Color.black) < 0.1f)
                {
                    nodes[i] = new CANode(i, GetCoordinate(i), CAType.Rock);
                }
                else if (color.ColorDistance(Color.blue) < 0.1f)
                {
                    nodes[i] = new CANode(i, GetCoordinate(i), CAType.Water);
                    nodes[i].Fluid = nodes[i].Capacity;
                }
                else if (color.ColorDistance(Color.green) < 0.1f)
                {
                    sourceNodes.Add(GetAddress(x, y));
                }
                else if (color.ColorDistance(Color.red) < 0.1f)
                {
                    sinkNodes.Add(GetAddress(x, y));
                }
                else
                {
                    nodes[i] = new CANode(i, GetCoordinate(i), CAType.Water);
                }
            }
        }
    }

    private bool IsOutOfBounds(int x, int y)
    {
        return x < 0 || x >= Width || y < 0 || y >= Height;
    }

    private bool IsFluid(CANode node)
    {
        if (node == null)
            return false;

        return node.Type == CAType.Water || node.Type == CAType.Empty;
    }

    private void MoveFluid(CANode source, CANode dest, float volume)
    {
        if (volume < 0)
            return;

        if (dest.Type == CAType.Empty)
            dest.Type = CAType.Water;

        volume = Mathf.Min(new float[] { source.Fluid, volume });

        source.Fluid -= volume;
        dest.Fluid += volume;

        changedNodes.Add(source.Address);
        changedNodes.Add(dest.Address);
    }

    public void RemoveMaterial(int x, int y)
    {
        CANode node = GetNodeAt(x, y);

        if (!IsFluid(node))
        {
            node.Type = CAType.Empty;
            changedNodes.Add(node.Address);
        }
    }

    public void RemoveMaterial(Vector2Int v)
    {
        RemoveMaterial(v.x, v.y);
    }

    public void Step()
    {
        // Update CA nodes

        for (int y = 0; y < Height; ++y)
            for (int x = reverseStep ? Width - 1 : 0; reverseStep ? x >= 0 : x < Width; x += reverseStep ? -1 : 1)
                StepNode(x, y);

        // Update sources and sinks

        foreach (int i in sourceNodes)
            nodes[i].Fluid = nodes[i].Capacity;

        foreach (int i in sinkNodes)
            nodes[i].Fluid = 0;

        reverseStep = !reverseStep;
    }

    private void StepNode(int x, int y)
    {
        CANode thisNode = GetNodeAt(x, y);

        if (thisNode.Type == CAType.Empty || thisNode.Type == CAType.Rock)
        {
            return;
        }
        else if (thisNode.Fluid == 0)
        {
            thisNode.Type = CAType.Empty;
            return;
        }

        CANode upNode = GetNodeAt(x, y + 1);
        CANode downNode = GetNodeAt(x, y - 1);
        CANode leftNode = GetNodeAt(x - 1, y);
        CANode rightNode = GetNodeAt(x + 1, y);
        float flow;
        Vector2 momentum = thisNode.Momentum;
        thisNode.Momentum *= 0.9f;

        if (IsFluid(downNode))
        {
            if (downNode.FreeVolume > 0)
            {
                flow = downNode.FreeVolume + CANode.MaxPressure;
                MoveFluid(thisNode, downNode, flow);
                thisNode.Momentum.y -= flow;
            }
            else if (thisNode.Fluid + CANode.MaxPressure > downNode.Fluid)
            {
                flow = (thisNode.Fluid - downNode.Fluid + CANode.MaxPressure) / 2;
                MoveFluid(thisNode, downNode, flow);
                thisNode.Momentum.y -= flow;
            }
        }

        for (int sign = -1; sign <= 1; sign += 2)    // do left side, then right side
        {
            CANode sideNode = sign > 0 ? rightNode : leftNode;
            List<float> fluids = new List<float>();

            if (!IsFluid(sideNode))
                continue;

            for (int j = 0; j <= 5; ++j)
            {
                CANode rowNode = GetNodeAt(x + j * sign, y);

                if (!IsFluid(rowNode))
                    break;

                fluids.Add(rowNode.Fluid);
            }

            if (fluids.Count == 0)
                continue;

            float avgFluid = fluids.Average();

            if (IsFluid(sideNode) && thisNode.Fluid > avgFluid)
            {
                flow = (thisNode.Fluid - avgFluid) / 2;
                MoveFluid(thisNode, sideNode, flow);
                thisNode.Momentum.x += sign * flow;
            }
        }

        if (IsFluid(upNode) && thisNode.FreeVolume < 0)
        {
            if (thisNode.Fluid - CANode.MaxPressure > upNode.Fluid)
            {
                flow = (thisNode.Fluid - upNode.Fluid - CANode.MaxPressure) / 2;
                MoveFluid(thisNode, upNode, flow);
                thisNode.Momentum.y += flow;
            }
        }

        /*if (momentum.x > 0 && rightNode != null)
            MoveFluid(thisNode, rightNode, momentum.x);
        if (momentum.x < 0 && leftNode != null)
            MoveFluid(thisNode, rightNode, momentum.x);*/

        if (momentum.y > 0 && upNode != null)
            MoveFluid(thisNode, upNode, momentum.y);
        else if (momentum.y < 0 && downNode != null)
            MoveFluid(thisNode, downNode, momentum.y);
    }

    public void WriteTexture(Texture2D texture)
    {
        if (texture.width != Width || texture.height != Height)
        {
            Debug.LogError("Texture-FluidField size mismatch in WriteTexture()!");
            return;
        }

        Vector2Int v;
        Color c;

        foreach (int i in changedNodes)
        {
            v = GetCoordinate(i);

            if (nodes[i].Type == CAType.Rock)
                c = Color.grey;
            else if (nodes[i].Type == CAType.Water && nodes[i].Fluid > 0)
                c = new Color(nodes[i].Fluid - nodes[i].Capacity, 0, 1, Mathf.Lerp(0f, 1f, nodes[i].Fluid / nodes[i].Capacity));
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

            if (_type == CAType.Empty)
                Capacity = maxCapacity;
            else if (_type == CAType.Water)
                Capacity = maxCapacity;
            else if (_type == CAType.Rock)
                Capacity = 0;
        }
    }

    public float Fluid;
    public float Capacity { get; private set; }
    public Vector2 Momentum;
    public float FreeVolume { get => Capacity - Fluid; }
    private static readonly float maxCapacity = 1;
    public static readonly float MaxPressure = 0.01f;

    public CANode(int address, Vector2Int coord, CAType type)
    {
        Address = address;
        Coordinate = coord;
        Type = type;
        Fluid = 0;
        Momentum = Vector2.zero;
    }
}

public class CAType
{
    public int PhysicsType { get; private set; }
    public static CAType Empty { get; private set; }
    public static CAType Water { get; private set; }
    public static CAType Rock { get; private set; }

    public CAType(int physicsType)
    {
        PhysicsType = physicsType;
    }

    public static void InitTypes()
    {
        Empty = new CAType(CAPhysicsType.NONE);
        Water = new CAType(CAPhysicsType.FLOW);
        Rock = new CAType(CAPhysicsType.NONE);
    }
}

public static class CAPhysicsType
{
    public static readonly int NONE = 0;
    public static readonly int FALL = 1;
    public static readonly int PILE = 2;
    public static readonly int FLOW = 3;
}