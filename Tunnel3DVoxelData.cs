using Godot;
/// <summary>
/// <para>Responsible for storing all voxel and chunk data used in generating <see cref="Tunnel3DMeshData"/> instances to generate tunnel meshes.</para>
/// </summary>
public partial class Tunnel3DVoxelData : Resource
{
    /// <summary>
    /// Stores the Volume Dimensions the collection of chunks occupy.
    /// </summary>
    [Export]
    public Vector3 VolumeSize { get; set; } = new Vector3(16, 16, 16);

    /// <summary>
    /// Stores the Voxel Count per Chunk.
    /// </summary>
    [Export(PropertyHint.Link)]
    public Vector3I VoxelCount { get; set; } = new Vector3I(8, 8, 8);

    /// <summary>
    /// Stores the Chunk Count per tunnel system.
    /// </summary>
    [Export]
    public Vector3I ChunkCount { get; set; } = new Vector3I(4, 4, 4);

    /// <summary>
    /// Stores the Voxel Weights per tunnel system.
    /// </summary>
    [Export]
    public byte[] VoxelWeights { get; set; }

    [ExportSubgroup("Mesh Generator")]
    [Export(PropertyHint.Range, "0,1")]
    public float TunnelLevel { get; set; } = 0.5f;

    /// <summary>
    /// Inverts tunnel weights being read to create the tunnel. Example: 255 -> 0, 0 -> 255, 55 -> 200
    /// </summary>
    [Export]
    public bool InvertTunnel { get; set; } = false;

}
