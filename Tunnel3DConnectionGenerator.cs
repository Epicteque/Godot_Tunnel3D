using Godot;
/// <summary>
/// <para>Tunnel3DGenerator is responsible for storing data for generating the tunnel connections in <see cref="Tunnel3DGenerationData"/> based on input parameters</para>
/// </summary>
public partial class Tunnel3DConnectionGenerator : Resource
{
    [ExportGroup("Node Generation")]
    /// <summary>
    /// Defines the Lower Corner of the Bounding Box the tunnel nodes are to generate in.
    /// </summary>
    [ExportSubgroup("Bounds")]
    [Export]
    public Vector3 BoundsLowerCorner { get; set; } = new Vector3(-8, -8, -8);

    /// <summary>
    /// Defines the Upper Corner of the Bounding Box the tunnel nodes are to generate in.
    /// </summary>
    [Export]
    public Vector3 BoundsUpperCorner { get; set; } = new Vector3(8, 8, 8);

    [ExportSubgroup("Nodes")]
    /// <summary>
    /// Node positions defined here will be included as nodes in the generated data
    /// </summary>
    [Export]
    public Vector3[] PresetNodes { get; set; }

    /// <summary>
    /// Count of nodes randomly generated in the tunnel system. Total node count includes <see cref="PresetNodes"/> and GeneratedNodeCount.
    /// </summary>
    [Export(PropertyHint.Range, "0,16,1,or_greater")]
    public int GeneratedNodeCount { get; set; }

    /// <summary>
    /// Count of tunnel connections in the tunnel system. Note: Regardless of specified tunnel connection count, it will always generate a tunnel system with all nodes connected.
    /// </summary>
    [Export(PropertyHint.Range, "0,15,1,or_greater")]
    public int GeneratedConnectionCount { get; set; }

    /// <summary>
    /// Test for tunnel collisions. Generated connections will exclude tunnel collisions.
    /// </summary>
    [Export]
    public bool TestIntersections { get; set; } = true;

    /// <summary>
    /// Radius threshold that must be exceeded to be considered a tunnel intersection.
    /// </summary>
    [Export]
    public float ThresholdRadius { get; set; } = 3.0f;

    /// <summary>
    /// Seed used to generate cave node positions.
    /// </summary>
    [Export]
    public int Seed { get; set; }

    /// <summary>
    /// The distance randomly generated points will attempt to generate from eachother
    /// </summary>
    [Export]
    public float NodeSeperationDistance { get; set; }

    /// <summary>
    /// Scales the calculated weights by the elevation angle between the two nodes. Higher values tend to reduce tunnel elevation in elevation to produce more accessible tunnel system.
    /// </summary>
    [ExportGroup("Weight Options")]
    [Export(PropertyHint.Range, "0,1,0.01,or_greater")]
    public float ElevationAspect { get; set; }
}
