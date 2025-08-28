#if TOOLS
using Godot;
/// <summary>
/// Class that loads and unloads <see cref="Tunnel3D"/> plugin and associated resources
/// </summary>
[Tool]
public partial class Tunnel3DPlugin : EditorPlugin
{
    public override void _EnterTree()
    {
        // Initialization of the plugin goes here.
        string localDir = ((Script)GetScript()).ResourcePath.GetBaseDir();

        AddCustomType("Tunnel3DMeshData", "Resource", (Script)ResourceLoader.Load($"{localDir}/Tunnel3DMeshData.cs"), (Texture2D)ResourceLoader.Load($"{localDir}/Tunnel3DDataIcon.png"));
        AddCustomType("Tunnel3DVoxelData", "Resource", (Script)ResourceLoader.Load($"{localDir}/Tunnel3DVoxelData.cs"), (Texture2D)ResourceLoader.Load($"{localDir}/Tunnel3DDataIcon.png"));
        AddCustomType("Tunnel3DGenerationData", "Resource", (Script)ResourceLoader.Load($"{localDir}/Tunnel3DGenerationData.cs"), (Texture2D)ResourceLoader.Load($"{localDir}/Tunnel3DDataIcon.png"));
        AddCustomType("Tunnel3DConnectionGenerator", "Resource", (Script)ResourceLoader.Load($"{localDir}/Tunnel3DConnectionGenerator.cs"), (Texture2D)ResourceLoader.Load($"{localDir}/Tunnel3DDataIcon.png"));

        AddCustomType("Tunnel3D", "Node3D", (Script)ResourceLoader.Load($"{localDir}/Tunnel3D.cs"), (Texture2D)ResourceLoader.Load($"{localDir}/Tunnel3DIcon.png"));
        AddCustomType("Tunnel3D", "Node3D", (Script)ResourceLoader.Load($"{localDir}/Tunnel3DGenScript.cs"), (Texture2D)ResourceLoader.Load($"{localDir}/Tunnel3DIcon.png"));

       
    }

    public override void _ExitTree()
    {
        // Clean-up of the plugin goes here.
        RemoveCustomType("Tunnel3D");
        RemoveCustomType("Tunnel3DMeshData");
        RemoveCustomType("Tunnel3DVoxelData");
        RemoveCustomType("Tunnel3DGenerationData");
        RemoveCustomType("Tunnel3DConnectionGenerator");
    }
}
#endif
