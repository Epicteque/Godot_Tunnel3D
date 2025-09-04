using Godot;
using System;
/// <summary>
/// <para>Responsible for storing all voxel and chunk data used in generating <see cref="Tunnel3DMeshData"/> instances to generate tunnel meshes.</para>
/// </summary>
[Tool, GlobalClass]
public partial class Tunnel3DVoxelData : Resource
{
    /// <summary>
    /// Stores the Volume Dimensions the collection of chunks occupy.
    /// </summary>
    [Export]
    public Vector3 VolumeSize
    {
        get { return _volumeSize; }
        set { _volumeSize = new Vector3 { X = Math.Max(value.X, 0.0f), Y = Math.Max(value.Y, 0.0f), Z = Math.Max(value.Z, 0.0f) }; }
    }
    private Vector3 _volumeSize = new Vector3(16, 16, 16);
    /// <summary>
    /// Stores the Voxel Count per Chunk.
    /// </summary>
    [Export(PropertyHint.Link)]
    public Vector3I VoxelCount
    {
        get { return _voxelCount; }
        set { _voxelCount = new Vector3I { X = Math.Max(value.X, 1), Y = Math.Max(value.Y, 1), Z = Math.Max(value.Z, 1) }; }
    }
    private Vector3I _voxelCount = new Vector3I(8, 8, 8);
    /// <summary>
    /// Stores the Chunk Count per tunnel system.
    /// </summary>
    [Export]
    public Vector3I ChunkCount
    {
        get { return _chunkCount; }
        set { _chunkCount = new Vector3I { X = Math.Max(value.X, 1), Y = Math.Max(value.Y, 1), Z = Math.Max(value.Z, 1) }; }
    }
    private Vector3I _chunkCount = new Vector3I(4, 4, 4);

    /// <summary>
    /// Stores the Voxel Weights per tunnel system.
    /// </summary>
    [Export]
    public byte[] VoxelWeights { get; set; }

    /// <summary>
    /// The value compared against the voxel weights to determine tunnel level displayed
    /// </summary>
    [ExportSubgroup("Mesh Generator"), Export(PropertyHint.Range, "0,1")]
    public float TunnelLevel
    {
        get { return _tunnelLevel; }
        set { _tunnelLevel = Math.Clamp(value, 0.0f, 1.0f); }
    }
    private float _tunnelLevel = 0.5f;

    /// <summary>
    /// Inverts tunnel weights being read to create the tunnel. Example: 255 -> 0, 0 -> 255, 55 -> 200
    /// </summary>
    [Export]
    public bool InvertTunnel { get; set; } = false;

}
