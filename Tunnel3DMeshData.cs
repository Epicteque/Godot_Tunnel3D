using Godot;
/// <summary>
/// <para>TunnelMeshData is the <see cref="Godot.Resource"/> responsible for storing all chunk data used in <see cref="Tunnel3DPlugin"/> instances.</para>
/// </summary>
public partial class Tunnel3DMeshData : Resource
{
    /// <summary>
    /// Stores the Chunk <see cref="Godot.ArrayMesh"/>s in the tunnel system.
    /// </summary>
    [Export]
    public Godot.Collections.Array<ArrayMesh> TunnelMeshes { get; set; }

    /// <summary>
    /// Stores the material of the tunnel meshes.
    /// </summary>
    [Export]
    public BaseMaterial3D TunnelMaterial { get; set; }



}
