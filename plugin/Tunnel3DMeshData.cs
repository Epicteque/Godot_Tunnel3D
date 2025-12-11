using Godot;
using System;
/// <summary>
/// <para>Responsible for storing all mesh data used in <see cref="Tunnel3D"/> nodes.</para>
/// </summary>
[Tool, GlobalClass]
public partial class Tunnel3DMeshData : Resource
{
    /// <summary>
    /// Stores the Chunk <see cref="Godot.ArrayMesh"/>s in the tunnel system.
    /// </summary>
    [Export]
    public Godot.Collections.Array<ArrayMesh> TunnelMeshes { get; set; }

    /// <summary>
    /// Stores the material of the tunnel meshes.
    /// <br></br>Note: Generated meshes are not UV mapped, hence triplanar mapping will need to be used.
    /// </summary>
    [Export]
    public BaseMaterial3D TunnelMaterial { get; set; }
    
    /// <summary>
    /// Stores how the mesh chunks are arranged
    /// </summary>
    [Export]
    public Vector3I TunnelChunks
    {
        get { return _tunnelChunks; }
        set { _tunnelChunks = new Vector3I { X = Math.Max(value.X, 1), Y = Math.Max(value.Y, 1), Z = Math.Max(value.Z, 1) }; }
    }
    private Vector3I _tunnelChunks = new Vector3I(4, 4, 4);

    /// <summary>
    /// Stores what volume the mesh chunks are distributed in.
    /// </summary>
    [Export]
    public Vector3 TunnelVolume
    {
        get { return _tunnelVolume; }
        set { _tunnelVolume = new Vector3I { X = Math.Max(value.X, 0f), Y = Math.Max(value.Y, 0f), Z = Math.Max(value.Z, 0f) }; }
    }
    private Vector3I _tunnelVolume = new Vector3I(16, 16, 16);

}
