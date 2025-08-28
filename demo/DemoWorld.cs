using Godot;
using System;
using System.Threading.Tasks;

public partial class DemoWorld : Node3D
{
    private async Task WaitToLoad(string path)
    {
        while (ResourceLoader.LoadThreadedGetStatus(path) != ResourceLoader.ThreadLoadStatus.Loaded)
        {
        }
    }

    private async Task SetupTunnel()
    {
        const string lightFixturePath = "uid://bwwlsnif0xjj0";

        ResourceLoader.LoadThreadedRequest(lightFixturePath);
        Tunnel3D tunnel = (Tunnel3D)FindChild("Tunnel3D");

        Node3D lightContainer = new Node3D();
        AddChild(lightContainer);

        tunnel.GenerateTunnelData();
        tunnel.GenerateVoxelData();
        tunnel.GenerateTunnelMesh();
        tunnel.GenerateMeshChildren();

        await ToSignal(tunnel, "TunnelLoaded");
        await WaitToLoad(lightFixturePath);

        PackedScene lightFixture = (PackedScene)ResourceLoader.LoadThreadedGet(lightFixturePath);
        foreach (Vector3 position in tunnel.Tunnel_Generation_Data.TunnelNodes)
        {
            Node3D lightInstance = lightFixture.Instantiate<Node3D>();
            lightInstance.Position = position + Vector3.Up * tunnel.Tunnel_Generation_Data.TunnelRadius*0.8f;
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
