using Godot;
using System.Threading.Tasks;
// Demo code of Tunnel3D
public partial class DemoWorld : Node3D
{
    private Task WaitToLoad(string path)
    {
        while (ResourceLoader.LoadThreadedGetStatus(path) != ResourceLoader.ThreadLoadStatus.Loaded)
        {
        }
        return Task.CompletedTask;
    }

    private async Task SetupTunnel()
    {
        const string lightFixturePath = "uid://bwwlsnif0xjj0";

        ResourceLoader.LoadThreadedRequest(lightFixturePath);
        Tunnel3D tunnel = (Tunnel3D)FindChild("Tunnel3D");

        Node3D lightContainer = new Node3D();
        AddChild(lightContainer);

        // Example Usage of Tunnel3D generation pipeline

        tunnel.GenerateTunnelData();
        tunnel.GenerateVoxelData();
        tunnel.GenerateTunnelMesh();
        tunnel.GenerateMeshChildren();

        await ToSignal(tunnel, "TunnelLoaded"); // Uses the Tunnel3D signal to wait until the Tunnel3D mesh has loaded.


        await WaitToLoad(lightFixturePath);

        /* Example usage of properties exposed from Tunnel3D
        
           Gets the positions of all Tunnel Junctions (Nodes) from the generated tunnel
           and places a light asset procedurally a set distance above the node position.

           Current position implementation is not perfectly flush with the tunnel. Can be mitigated with PhysicsDirectSpaceState3D.IntersectRay or Raycast3D, but will suffice for demonstration purposes.
        */
        PackedScene lightFixture = (PackedScene)ResourceLoader.LoadThreadedGet(lightFixturePath);
        foreach (Vector3 position in tunnel.Tunnel_Generation_Data.TunnelNodes)
        {
            Node3D lightInstance = lightFixture.Instantiate<Node3D>();
            lightInstance.Position = position + Vector3.Up * tunnel.Tunnel_Generation_Data.TunnelRadius * 0.8f;
            AddChild(lightInstance);
        }
    }

    public override async void _Ready()
    {
        DemoCharacterController characterBody3D = (DemoCharacterController)FindChild("CharacterBody3D");

        await SetupTunnel();

        characterBody3D.Enabled = true;
    }

}
