using Godot;
/// <summary>
/// <para>Responsible for storing all data for generating <see cref="Tunnel3DVoxelData">.</para>
/// </summary>
public partial class Tunnel3DGenerationData : Resource
{
    /// <summary>
    /// Optional Noise added during the generation of the tunnels to vary cave surface.
    /// </summary>
    [ExportGroup("Noise")]
    [Export]
    public FastNoiseLite Noise { get; set; }
    [Export(PropertyHint.Range, "0,1")]
    public float NoiseIntensity { get; set; }
    [ExportGroup("Tunnel Radius")]
    [Export(PropertyHint.Range, "0,10,0.05,or_greater,exp")]
    public float TunnelRadius { get; set; } = 1.0f;
    /// <summary>
    /// Ease function scales tunnel voxel value as it gets away from the centre of the tunnel.
    /// </summary>
    [Export]
    public Curve TunnelEaseFunction { get; set; } = CurveSetup();
    /// <summary>
    /// Stores positions of nodes
    /// </summary>
    [ExportGroup("Tunnel Graph")]
    [Export]
    public Vector3[] TunnelNodes { get; set; }

    // 2D arrays (or jagged arrays) would be more appropriate, however GDScript does not have support for 2D arrays, only embedded arrays.
    // GDScript has an alternative: PackedByteArrays. It is contiguous, being the equivalent to byte[]

    /// <summary>
    /// A flattened adjacency matrix of the tunnel network
    /// </summary>
    [Export]
    public byte[] AdjacencyMatrix { get; set; } // as memory efficient as bool[] but accessible in Godot Editor 
    /// <summary>
    /// A flattened weight matrix of the tunnel network. Does not consider tunnel intersections as part of weight.
    /// </summary>
    [Export]
    public float[] WeightMatrix { get; set; }

    static private Curve CurveSetup()
    {
        Curve curve = new Curve { MinDomain = 0, MaxDomain = 1, MaxValue = 1, MinValue = 0 };
        curve.AddPoint(new Vector2(0.6f, 1));
        curve.AddPoint(new Vector2(1, 0));
        return curve;
    }
}
