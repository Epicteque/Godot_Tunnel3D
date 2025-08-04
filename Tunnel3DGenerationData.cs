using Godot;
/// <summary>
/// <para>TunnelGenerationData is the <see cref="Godot.Resource"/> responsible for storing all generation data used in <see cref="Tunnel3DPlugin"/> instances.</para>
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

    [ExportGroup("Tunnel Displacement")]

    [Export(PropertyHint.Range, "0,10,0.05,or_greater,exp")]
    public float TunnelRadius { get; set; } = 1.0f;

    /// <summary>
    /// Ease function scales tunnel voxel value as it gets away from the centre of the tunnel.
    /// </summary>
    [Export]
    public Curve TunnelEaseFunction { get; set; } = CurveSetup();

    [ExportGroup("Tunnel Graph")]

    /// <summary>
    /// Stores positions of nodes
    /// </summary>
    [Export]
    public Vector3[] TunnelNodes { get; set; }

    // 2D arrays would be more appropriate, however GDScript does not have support for 2D arrays, only embedded arrays.
    // GDScript's arrays are dynamic whereas GDScript's PackedByteArrays are static, being the equivalent to byte[]

    [Export]
    public byte[] AdjacencyMatrix { get; set; } // as memory efficient as bool[] but accessible in Godot Editor 

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
