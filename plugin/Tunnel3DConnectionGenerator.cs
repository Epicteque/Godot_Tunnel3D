using Godot;
using System;
/// <summary>
/// <para>Responsible for storing data for generating <see cref="Tunnel3DGenerationData"/> based on input parameters</para>
/// </summary>
[Tool, GlobalClass]
public partial class Tunnel3DConnectionGenerator : Resource
{
    /// <summary>
    /// Defines the Lower Corner of the Bounding Box the tunnel nodes are to generate in.
    /// </summary>
    [ExportGroup("Node Generation"), ExportSubgroup("Bounds"), Export]
    public Vector3 BoundsLowerCorner { get; set; } = new Vector3(-8, -8, -8);

    /// <summary>
    /// Defines the Upper Corner of the Bounding Box the tunnel nodes are to generate in.
    /// </summary>
    [Export]
    public Vector3 BoundsUpperCorner { get; set; } = new Vector3(8, 8, 8);
    /// <summary>
    /// Node positions defined here will be included as nodes in the generated data
    /// </summary>
    [ExportSubgroup("Nodes"), Export]
    public Vector3[] PresetNodes { get; set; }
    /// <summary>
    /// Count of nodes randomly generated in the tunnel system. Total node count includes <see cref="PresetNodes"/> and GeneratedNodeCount.
    /// </summary>
    [Export(PropertyHint.Range, "0,16,1,or_greater")]
    public int GeneratedNodeCount
    {
        get { return _generatedNodeCount; }
        set { _generatedNodeCount = Math.Max(0, value); }
    }
    private int _generatedNodeCount = 0;
    /// <summary>
    /// Count of tunnel connections in the tunnel system. Note: Regardless of specified tunnel connection count, it will always generate a tunnel system with all nodes connected.
    /// </summary>
    [Export(PropertyHint.Range, "0,15,1,or_greater")]
    public int GeneratedConnectionCount
    {
        get { return _generatedConnectionCount; }
        set { _generatedConnectionCount = Math.Max(0, value); }
    }
    private int _generatedConnectionCount = 0;
    /// <summary>
    /// Test for tunnel collisions. Generated connections will exclude tunnel collisions.
    /// </summary>
    [Export]
    public bool TestIntersections { get; set; } = true;
    /// <summary>
    /// The radius threshold between 2 tunnels that must be met to be considered a tunnel intersection.
    /// </summary>
    [Export(PropertyHint.Range, "0,5,or_greater")]
    public float ThresholdRadius
    {
        get { return _thresholdRadius; }
        set { _thresholdRadius = Math.Max(0, value); }
    }
    private float _thresholdRadius = 3.0f;
    /// <summary>
    /// Seed used to generate cave node positions.
    /// </summary>
    [Export]
    public int Seed { get; set; }
    /// <summary>
    /// The distance randomly generated points will attempt to generate from eachother
    /// </summary>
    [Export(PropertyHint.Range, "0,5,or_greater")]
    public float NodeSeparationDistance
    {
        get { return _nodeSeparationDistance; }
        set { _nodeSeparationDistance = Math.Min(0.0f, value); }
    }
    private float _nodeSeparationDistance = 0.0f;
    /// <summary>
    /// A heuristic that scales the calculated weights by the elevation angle between the two nodes.
    /// <br></br>Higher values tend to reduce tunnel elevation to produce more a traversable tunnel system.
    /// </summary>
    [ExportGroup("Weight Options"), Export(PropertyHint.Range, "0,1,0.01,or_greater")]
    public float ElevationAspect
    {
        get { return _elevationAspect; }
        set { _elevationAspect = Math.Max(0, value); }
    }
    private float _elevationAspect = 0.0f;
}
