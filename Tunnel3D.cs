using Godot;
using System;
using System.Collections.Generic;


/// <summary>
/// Generates 3D tunnels based on input parameters.
/// </summary>
[Tool]
public partial class Tunnel3D : Node3D
{
    private Tunnel3DMeshData _meshData = new Tunnel3DMeshData();
    private Tunnel3DVoxelData _voxelData = new Tunnel3DVoxelData();
    private Tunnel3DGenerationData _tunnelData = new Tunnel3DGenerationData();
    private Tunnel3DConnectionGenerator _generator = new Tunnel3DConnectionGenerator();
    private Node3D tunnelContainer = new Node3D();
    private Queue<Action> taskQueue = new Queue<Action>();

    private bool _generateCollisionMeshes = false;
    private uint _collisionLayer = 1;
    private uint _collisionMask = 1;

    private readonly object lockObj = new object(); // no Lock class in .NET 8
    private bool doingTasks = false;
    private long taskID = 0;
    private bool methodRunFlag = false;

    /// <summary>
    /// Resource that stores data that generates <see cref="Tunnel3D.Tunnel_Generation_Data"/>
    /// </summary>
    [ExportGroup("Tunnel Data")]
    [Export]
    public Resource Connection_Generator // Can't be set to Tunnel3DGenerationData due to editor limitations.
    {
        get { return _generator; }
        set { _generator = value as Tunnel3DConnectionGenerator; } // Workaround due to editor not considering "Tunnel3DConnectionGenerator" it's own type.
    }
    /// <summary>
    /// Resource that stores data that generates <see cref="Tunnel3D.Tunnel_Voxel_Data"/>
    /// </summary>
    [Export]
    public Resource Tunnel_Generation_Data // Same here.
    {
        get { return _tunnelData; }
        set { _tunnelData = value as Tunnel3DGenerationData; }
    }
    /// <summary>
    /// Resource that stores data that generates <see cref="Tunnel3D.Tunnel_Mesh_Data"/>
    /// </summary>
    [Export]
    public Resource Tunnel_Voxel_Data // Same here.
    {
        get { return _voxelData; }
        set { _voxelData = value as Tunnel3DVoxelData; }
    }
    /// <summary>
    /// Resource that stores data that displays the tunnel meshes.
    /// </summary>
    [Export]
    public Resource Tunnel_Mesh_Data // you get the idea
    {
        get { return _meshData; }
        set { _meshData = value as Tunnel3DMeshData; }
    }
    /// <summary>
    /// Generate Trimesh collisions for each chunk. <see cref="Tunnel3D.GenerateMeshChildren"/> will need to be called after setting.
    /// </summary>
    [ExportGroup("Tunnel Options")]
    [ExportSubgroup("Physics")]
    [Export]
    public bool Generate_Collision_Meshes
    {
        get { return _generateCollisionMeshes; }
        set { _generateCollisionMeshes = value; }
    }
    /// <summary>
    /// Collision Layer for generated collisions. <see cref="Tunnel3D.GenerateMeshChildren"/> will need to be called after setting.
    /// </summary>
    [Export(PropertyHint.Layers3DPhysics)]
    public uint Collision_Layer
    {
        get { return _collisionLayer; }
        set { _collisionLayer = value; }
    }
    /// <summary>
    /// Collision Mask for generated collisions. <see cref="Tunnel3D.GenerateMeshChildren"/> will need to be called after setting.
    /// </summary>
    [Export(PropertyHint.Layers3DPhysics)]
    public uint Collision_Mask
    {
        get { return _collisionMask; }
        set { _collisionMask = value; }
    }
    /// <summary>
    /// Callable that encapsulates <see cref="Tunnel3D.GenerateTunnelData"/>
    /// </summary>
    [ExportGroup("Actions")]
    [ExportToolButton("Generate Tunnel Data")]
    public Callable GenerateTunnelDataCallable => Callable.From(() => QueueTask(GenTunnelData)); //GenTunnelData
    /// <summary>
    /// Callable that encapsulates <see cref="Tunnel3D.GenerateVoxelData"/>
    /// </summary>
    [ExportToolButton("Generate Voxel Data")]
    public Callable GenerateVoxelDataCallable => Callable.From(() => QueueTask(GenVoxelData)); //GenVoxelData
    /// <summary>
    /// Callable that encapsulates <see cref="Tunnel3D.GenerateTunnelMesh"/>
    /// </summary>
    [ExportToolButton("Generate Tunnel Mesh")]
    public Callable GenerateTunnelMeshCallable => Callable.From(() => QueueTask(GenTunnelMesh)); //GenTunnelMesh
    /// <summary>
    /// Callable that encapsulates <see cref="Tunnel3D.GenerateMeshChildren"/>
    /// </summary>
    [ExportToolButton("Generate Mesh Children")]
    public Callable GenerateMeshChildrenCallable => Callable.From(() => QueueTask(GenMeshChildren)); //GenMeshChildren
    /// <summary>
    /// Callable that encapsulates <see cref="Tunnel3D.DestroyMeshChildren"/>
    /// </summary>
    [ExportToolButton("Destroy Mesh Children")]
    public Callable DestroyMeshChildrenCallable => Callable.From(() => QueueTask(DestroyChildren)); //DestroyChildren
    /// <summary>
    /// Updates <see cref="Tunnel3D.Tunnel_Generation_Data"/> tunnel connections from data in <see cref="Tunnel3D.Connection_Generator"/>
    /// </summary>
    public void GenerateTunnelData()
    {
        QueueTask(GenTunnelData);
    }
    /// <summary>
    /// Updates <see cref="Tunnel3D.Tunnel_Voxel_Data"/> voxel weights from data in <see cref="Tunnel3D.Tunnel_Generation_Data"/>
    /// </summary>
    public void GenerateVoxelData()
    {
        QueueTask(GenVoxelData);
    }
    /// <summary>
    /// Updates <see cref="Tunnel3D.Tunnel_Mesh_Data"/> tunnel meshes from data in <see cref="Tunnel3D.Tunnel_Voxel_Data"/>
    /// </summary>
    public void GenerateTunnelMesh()
    {
        QueueTask(GenTunnelMesh);
    }
    /// <summary>
    /// Generates children and displays the tunnel mesh and collisions (if input parameters specify)
    /// </summary>
    public void GenerateMeshChildren()
    {
        QueueTask(GenMeshChildren);
    }
    /// <summary>
    /// Destroys children and removes the tunnel mesh and collisions
    /// </summary>
    public void DestroyMeshChildren()
    {
        QueueTask(DestroyChildren);
    }
    /// <summary>
    /// A read-only flag indicating if called methods have finished executing
    /// </summary>
    [Export]
    public bool Work_Finished
    {
        get { return !doingTasks; }
        set { } // does nothing but required for export to allow it. Can't do exception either because it always runs on setup.
    }

    private bool PoolFinished
    {
        get { if (taskID == 0) { return true; } else { return WorkerThreadPool.IsGroupTaskCompleted(taskID); } }
    }

    [Signal]
    public delegate void TunnelLoadedEventHandler();

    public override void _Ready()
    {
        AddChild(tunnelContainer);
        tunnelContainer.Name = "Tunnel3DContainer";
    }

    // Source: https://gist.github.com/dwilliamson/c041e3454a713e58baf6e4f8e5fffecd

    readonly private static Vector3[] cornerPositions =
    {
        new Vector3 (0,0,0),
        new Vector3 (1,0,0),
        new Vector3 (0,1,0),
        new Vector3 (1,1,0),

        new Vector3 (0,0,1),
        new Vector3 (1,0,1),
        new Vector3 (0,1,1),
        new Vector3 (1,1,1),
    };
    readonly private static int[][] edgeVertexLUT =
    [
        [0, 1],
        [1, 3],
        [3, 2],
        [2, 0],
        [4, 5],
        [5, 7],
        [7, 6],
        [6, 4],
        [0, 4],
        [1, 5],
        [3, 7],
        [2, 6]
    ];

    readonly private static int[][] triangulationTable = [
        [-1],
        [0, 3, 8, -1],
        [0, 9, 1, -1],
        [3, 8, 1, 1, 8, 9, -1],
        [2, 11, 3, -1],
        [8, 0, 11, 11, 0, 2, -1],
        [3, 2, 11, 1, 0, 9, -1],
        [11, 1, 2, 11, 9, 1, 11, 8, 9, -1],
        [1, 10, 2, -1],
        [0, 3, 8, 2, 1, 10, -1],
        [10, 2, 9, 9, 2, 0, -1],
        [8, 2, 3, 8, 10, 2, 8, 9, 10, -1],
        [11, 3, 10, 10, 3, 1, -1],
        [10, 0, 1, 10, 8, 0, 10, 11, 8, -1],
        [9, 3, 0, 9, 11, 3, 9, 10, 11, -1],
        [8, 9, 11, 11, 9, 10, -1],
        [4, 8, 7, -1],
        [7, 4, 3, 3, 4, 0, -1],
        [4, 8, 7, 0, 9, 1, -1],
        [1, 4, 9, 1, 7, 4, 1, 3, 7, -1],
        [8, 7, 4, 11, 3, 2, -1],
        [4, 11, 7, 4, 2, 11, 4, 0, 2, -1],
        [0, 9, 1, 8, 7, 4, 11, 3, 2, -1],
        [7, 4, 11, 11, 4, 2, 2, 4, 9, 2, 9, 1, -1],
        [4, 8, 7, 2, 1, 10, -1],
        [7, 4, 3, 3, 4, 0, 10, 2, 1, -1],
        [10, 2, 9, 9, 2, 0, 7, 4, 8, -1],
        [10, 2, 3, 10, 3, 4, 3, 7, 4, 9, 10, 4, -1],
        [1, 10, 3, 3, 10, 11, 4, 8, 7, -1],
        [10, 11, 1, 11, 7, 4, 1, 11, 4, 1, 4, 0, -1],
        [7, 4, 8, 9, 3, 0, 9, 11, 3, 9, 10, 11, -1],
        [7, 4, 11, 4, 9, 11, 9, 10, 11, -1],
        [9, 4, 5, -1],
        [9, 4, 5, 8, 0, 3, -1],
        [4, 5, 0, 0, 5, 1, -1],
        [5, 8, 4, 5, 3, 8, 5, 1, 3, -1],
        [9, 4, 5, 11, 3, 2, -1],
        [2, 11, 0, 0, 11, 8, 5, 9, 4, -1],
        [4, 5, 0, 0, 5, 1, 11, 3, 2, -1],
        [5, 1, 4, 1, 2, 11, 4, 1, 11, 4, 11, 8, -1],
        [1, 10, 2, 5, 9, 4, -1],
        [9, 4, 5, 0, 3, 8, 2, 1, 10, -1],
        [2, 5, 10, 2, 4, 5, 2, 0, 4, -1],
        [10, 2, 5, 5, 2, 4, 4, 2, 3, 4, 3, 8, -1],
        [11, 3, 10, 10, 3, 1, 4, 5, 9, -1],
        [4, 5, 9, 10, 0, 1, 10, 8, 0, 10, 11, 8, -1],
        [11, 3, 0, 11, 0, 5, 0, 4, 5, 10, 11, 5, -1],
        [4, 5, 8, 5, 10, 8, 10, 11, 8, -1],
        [8, 7, 9, 9, 7, 5, -1],
        [3, 9, 0, 3, 5, 9, 3, 7, 5, -1],
        [7, 0, 8, 7, 1, 0, 7, 5, 1, -1],
        [7, 5, 3, 3, 5, 1, -1],
        [5, 9, 7, 7, 9, 8, 2, 11, 3, -1],
        [2, 11, 7, 2, 7, 9, 7, 5, 9, 0, 2, 9, -1],
        [2, 11, 3, 7, 0, 8, 7, 1, 0, 7, 5, 1, -1],
        [2, 11, 1, 11, 7, 1, 7, 5, 1, -1],
        [8, 7, 9, 9, 7, 5, 2, 1, 10, -1],
        [10, 2, 1, 3, 9, 0, 3, 5, 9, 3, 7, 5, -1],
        [7, 5, 8, 5, 10, 2, 8, 5, 2, 8, 2, 0, -1],
        [10, 2, 5, 2, 3, 5, 3, 7, 5, -1],
        [8, 7, 5, 8, 5, 9, 11, 3, 10, 3, 1, 10, -1],
        [5, 11, 7, 10, 11, 5, 1, 9, 0, -1],
        [11, 5, 10, 7, 5, 11, 8, 3, 0, -1],
        [5, 11, 7, 10, 11, 5, -1],
        [6, 7, 11, -1],
        [7, 11, 6, 3, 8, 0, -1],
        [6, 7, 11, 0, 9, 1, -1],
        [9, 1, 8, 8, 1, 3, 6, 7, 11, -1],
        [3, 2, 7, 7, 2, 6, -1],
        [0, 7, 8, 0, 6, 7, 0, 2, 6, -1],
        [6, 7, 2, 2, 7, 3, 9, 1, 0, -1],
        [6, 7, 8, 6, 8, 1, 8, 9, 1, 2, 6, 1, -1],
        [11, 6, 7, 10, 2, 1, -1],
        [3, 8, 0, 11, 6, 7, 10, 2, 1, -1],
        [0, 9, 2, 2, 9, 10, 7, 11, 6, -1],
        [6, 7, 11, 8, 2, 3, 8, 10, 2, 8, 9, 10, -1],
        [7, 10, 6, 7, 1, 10, 7, 3, 1, -1],
        [8, 0, 7, 7, 0, 6, 6, 0, 1, 6, 1, 10, -1],
        [7, 3, 6, 3, 0, 9, 6, 3, 9, 6, 9, 10, -1],
        [6, 7, 10, 7, 8, 10, 8, 9, 10, -1],
        [11, 6, 8, 8, 6, 4, -1],
        [6, 3, 11, 6, 0, 3, 6, 4, 0, -1],
        [11, 6, 8, 8, 6, 4, 1, 0, 9, -1],
        [1, 3, 9, 3, 11, 6, 9, 3, 6, 9, 6, 4, -1],
        [2, 8, 3, 2, 4, 8, 2, 6, 4, -1],
        [4, 0, 6, 6, 0, 2, -1],
        [9, 1, 0, 2, 8, 3, 2, 4, 8, 2, 6, 4, -1],
        [9, 1, 4, 1, 2, 4, 2, 6, 4, -1],
        [4, 8, 6, 6, 8, 11, 1, 10, 2, -1],
        [1, 10, 2, 6, 3, 11, 6, 0, 3, 6, 4, 0, -1],
        [11, 6, 4, 11, 4, 8, 10, 2, 9, 2, 0, 9, -1],
        [10, 4, 9, 6, 4, 10, 11, 2, 3, -1],
        [4, 8, 3, 4, 3, 10, 3, 1, 10, 6, 4, 10, -1],
        [1, 10, 0, 10, 6, 0, 6, 4, 0, -1],
        [4, 10, 6, 9, 10, 4, 0, 8, 3, -1],
        [4, 10, 6, 9, 10, 4, -1],
        [6, 7, 11, 4, 5, 9, -1],
        [4, 5, 9, 7, 11, 6, 3, 8, 0, -1],
        [1, 0, 5, 5, 0, 4, 11, 6, 7, -1],
        [11, 6, 7, 5, 8, 4, 5, 3, 8, 5, 1, 3, -1],
        [3, 2, 7, 7, 2, 6, 9, 4, 5, -1],
        [5, 9, 4, 0, 7, 8, 0, 6, 7, 0, 2, 6, -1],
        [3, 2, 6, 3, 6, 7, 1, 0, 5, 0, 4, 5, -1],
        [6, 1, 2, 5, 1, 6, 4, 7, 8, -1],
        [10, 2, 1, 6, 7, 11, 4, 5, 9, -1],
        [0, 3, 8, 4, 5, 9, 11, 6, 7, 10, 2, 1, -1],
        [7, 11, 6, 2, 5, 10, 2, 4, 5, 2, 0, 4, -1],
        [8, 4, 7, 5, 10, 6, 3, 11, 2, -1],
        [9, 4, 5, 7, 10, 6, 7, 1, 10, 7, 3, 1, -1],
        [10, 6, 5, 7, 8, 4, 1, 9, 0, -1],
        [4, 3, 0, 7, 3, 4, 6, 5, 10, -1],
        [10, 6, 5, 8, 4, 7, -1],
        [9, 6, 5, 9, 11, 6, 9, 8, 11, -1],
        [11, 6, 3, 3, 6, 0, 0, 6, 5, 0, 5, 9, -1],
        [11, 6, 5, 11, 5, 0, 5, 1, 0, 8, 11, 0, -1],
        [11, 6, 3, 6, 5, 3, 5, 1, 3, -1],
        [9, 8, 5, 8, 3, 2, 5, 8, 2, 5, 2, 6, -1],
        [5, 9, 6, 9, 0, 6, 0, 2, 6, -1],
        [1, 6, 5, 2, 6, 1, 3, 0, 8, -1],
        [1, 6, 5, 2, 6, 1, -1],
        [2, 1, 10, 9, 6, 5, 9, 11, 6, 9, 8, 11, -1],
        [9, 0, 1, 3, 11, 2, 5, 10, 6, -1],
        [11, 0, 8, 2, 0, 11, 10, 6, 5, -1],
        [3, 11, 2, 5, 10, 6, -1],
        [1, 8, 3, 9, 8, 1, 5, 10, 6, -1],
        [6, 5, 10, 0, 1, 9, -1],
        [8, 3, 0, 5, 10, 6, -1],
        [6, 5, 10, -1],
        [10, 5, 6, -1],
        [0, 3, 8, 6, 10, 5, -1],
        [10, 5, 6, 9, 1, 0, -1],
        [3, 8, 1, 1, 8, 9, 6, 10, 5, -1],
        [2, 11, 3, 6, 10, 5, -1],
        [8, 0, 11, 11, 0, 2, 5, 6, 10, -1],
        [1, 0, 9, 2, 11, 3, 6, 10, 5, -1],
        [5, 6, 10, 11, 1, 2, 11, 9, 1, 11, 8, 9, -1],
        [5, 6, 1, 1, 6, 2, -1],
        [5, 6, 1, 1, 6, 2, 8, 0, 3, -1],
        [6, 9, 5, 6, 0, 9, 6, 2, 0, -1],
        [6, 2, 5, 2, 3, 8, 5, 2, 8, 5, 8, 9, -1],
        [3, 6, 11, 3, 5, 6, 3, 1, 5, -1],
        [8, 0, 1, 8, 1, 6, 1, 5, 6, 11, 8, 6, -1],
        [11, 3, 6, 6, 3, 5, 5, 3, 0, 5, 0, 9, -1],
        [5, 6, 9, 6, 11, 9, 11, 8, 9, -1],
        [5, 6, 10, 7, 4, 8, -1],
        [0, 3, 4, 4, 3, 7, 10, 5, 6, -1],
        [5, 6, 10, 4, 8, 7, 0, 9, 1, -1],
        [6, 10, 5, 1, 4, 9, 1, 7, 4, 1, 3, 7, -1],
        [7, 4, 8, 6, 10, 5, 2, 11, 3, -1],
        [10, 5, 6, 4, 11, 7, 4, 2, 11, 4, 0, 2, -1],
        [4, 8, 7, 6, 10, 5, 3, 2, 11, 1, 0, 9, -1],
        [1, 2, 10, 11, 7, 6, 9, 5, 4, -1],
        [2, 1, 6, 6, 1, 5, 8, 7, 4, -1],
        [0, 3, 7, 0, 7, 4, 2, 1, 6, 1, 5, 6, -1],
        [8, 7, 4, 6, 9, 5, 6, 0, 9, 6, 2, 0, -1],
        [7, 2, 3, 6, 2, 7, 5, 4, 9, -1],
        [4, 8, 7, 3, 6, 11, 3, 5, 6, 3, 1, 5, -1],
        [5, 0, 1, 4, 0, 5, 7, 6, 11, -1],
        [9, 5, 4, 6, 11, 7, 0, 8, 3, -1],
        [11, 7, 6, 9, 5, 4, -1],
        [6, 10, 4, 4, 10, 9, -1],
        [6, 10, 4, 4, 10, 9, 3, 8, 0, -1],
        [0, 10, 1, 0, 6, 10, 0, 4, 6, -1],
        [6, 10, 1, 6, 1, 8, 1, 3, 8, 4, 6, 8, -1],
        [9, 4, 10, 10, 4, 6, 3, 2, 11, -1],
        [2, 11, 8, 2, 8, 0, 6, 10, 4, 10, 9, 4, -1],
        [11, 3, 2, 0, 10, 1, 0, 6, 10, 0, 4, 6, -1],
        [6, 8, 4, 11, 8, 6, 2, 10, 1, -1],
        [4, 1, 9, 4, 2, 1, 4, 6, 2, -1],
        [3, 8, 0, 4, 1, 9, 4, 2, 1, 4, 6, 2, -1],
        [6, 2, 4, 4, 2, 0, -1],
        [3, 8, 2, 8, 4, 2, 4, 6, 2, -1],
        [4, 6, 9, 6, 11, 3, 9, 6, 3, 9, 3, 1, -1],
        [8, 6, 11, 4, 6, 8, 9, 0, 1, -1],
        [11, 3, 6, 3, 0, 6, 0, 4, 6, -1],
        [8, 6, 11, 4, 6, 8, -1],
        [10, 7, 6, 10, 8, 7, 10, 9, 8, -1],
        [3, 7, 0, 7, 6, 10, 0, 7, 10, 0, 10, 9, -1],
        [6, 10, 7, 7, 10, 8, 8, 10, 1, 8, 1, 0, -1],
        [6, 10, 7, 10, 1, 7, 1, 3, 7, -1],
        [3, 2, 11, 10, 7, 6, 10, 8, 7, 10, 9, 8, -1],
        [2, 9, 0, 10, 9, 2, 6, 11, 7, -1],
        [0, 8, 3, 7, 6, 11, 1, 2, 10, -1],
        [7, 6, 11, 1, 2, 10, -1],
        [2, 1, 9, 2, 9, 7, 9, 8, 7, 6, 2, 7, -1],
        [2, 7, 6, 3, 7, 2, 0, 1, 9, -1],
        [8, 7, 0, 7, 6, 0, 6, 2, 0, -1],
        [7, 2, 3, 6, 2, 7, -1],
        [8, 1, 9, 3, 1, 8, 11, 7, 6, -1],
        [11, 7, 6, 1, 9, 0, -1],
        [6, 11, 7, 0, 8, 3, -1],
        [11, 7, 6, -1],
        [7, 11, 5, 5, 11, 10, -1],
        [10, 5, 11, 11, 5, 7, 0, 3, 8, -1],
        [7, 11, 5, 5, 11, 10, 0, 9, 1, -1],
        [7, 11, 10, 7, 10, 5, 3, 8, 1, 8, 9, 1, -1],
        [5, 2, 10, 5, 3, 2, 5, 7, 3, -1],
        [5, 7, 10, 7, 8, 0, 10, 7, 0, 10, 0, 2, -1],
        [0, 9, 1, 5, 2, 10, 5, 3, 2, 5, 7, 3, -1],
        [9, 7, 8, 5, 7, 9, 10, 1, 2, -1],
        [1, 11, 2, 1, 7, 11, 1, 5, 7, -1],
        [8, 0, 3, 1, 11, 2, 1, 7, 11, 1, 5, 7, -1],
        [7, 11, 2, 7, 2, 9, 2, 0, 9, 5, 7, 9, -1],
        [7, 9, 5, 8, 9, 7, 3, 11, 2, -1],
        [3, 1, 7, 7, 1, 5, -1],
        [8, 0, 7, 0, 1, 7, 1, 5, 7, -1],
        [0, 9, 3, 9, 5, 3, 5, 7, 3, -1],
        [9, 7, 8, 5, 7, 9, -1],
        [8, 5, 4, 8, 10, 5, 8, 11, 10, -1],
        [0, 3, 11, 0, 11, 5, 11, 10, 5, 4, 0, 5, -1],
        [1, 0, 9, 8, 5, 4, 8, 10, 5, 8, 11, 10, -1],
        [10, 3, 11, 1, 3, 10, 9, 5, 4, -1],
        [3, 2, 8, 8, 2, 4, 4, 2, 10, 4, 10, 5, -1],
        [10, 5, 2, 5, 4, 2, 4, 0, 2, -1],
        [5, 4, 9, 8, 3, 0, 10, 1, 2, -1],
        [2, 10, 1, 4, 9, 5, -1],
        [8, 11, 4, 11, 2, 1, 4, 11, 1, 4, 1, 5, -1],
        [0, 5, 4, 1, 5, 0, 2, 3, 11, -1],
        [0, 11, 2, 8, 11, 0, 4, 9, 5, -1],
        [5, 4, 9, 2, 3, 11, -1],
        [4, 8, 5, 8, 3, 5, 3, 1, 5, -1],
        [0, 5, 4, 1, 5, 0, -1],
        [5, 4, 9, 3, 0, 8, -1],
        [5, 4, 9, -1],
        [11, 4, 7, 11, 9, 4, 11, 10, 9, -1],
        [0, 3, 8, 11, 4, 7, 11, 9, 4, 11, 10, 9, -1],
        [11, 10, 7, 10, 1, 0, 7, 10, 0, 7, 0, 4, -1],
        [3, 10, 1, 11, 10, 3, 7, 8, 4, -1],
        [3, 2, 10, 3, 10, 4, 10, 9, 4, 7, 3, 4, -1],
        [9, 2, 10, 0, 2, 9, 8, 4, 7, -1],
        [3, 4, 7, 0, 4, 3, 1, 2, 10, -1],
        [7, 8, 4, 10, 1, 2, -1],
        [7, 11, 4, 4, 11, 9, 9, 11, 2, 9, 2, 1, -1],
        [1, 9, 0, 4, 7, 8, 2, 3, 11, -1],
        [7, 11, 4, 11, 2, 4, 2, 0, 4, -1],
        [4, 7, 8, 2, 3, 11, -1],
        [9, 4, 1, 4, 7, 1, 7, 3, 1, -1],
        [7, 8, 4, 1, 9, 0, -1],
        [3, 4, 7, 0, 4, 3, -1],
        [7, 8, 4, -1],
        [11, 10, 8, 8, 10, 9, -1],
        [0, 3, 9, 3, 11, 9, 11, 10, 9, -1],
        [1, 0, 10, 0, 8, 10, 8, 11, 10, -1],
        [10, 3, 11, 1, 3, 10, -1],
        [3, 2, 8, 2, 10, 8, 10, 9, 8, -1],
        [9, 2, 10, 0, 2, 9, -1],
        [8, 3, 0, 10, 1, 2, -1],
        [2, 10, 1, -1],
        [2, 1, 11, 1, 9, 11, 9, 8, 11, -1],
        [11, 2, 3, 9, 0, 1, -1],
        [11, 0, 8, 2, 0, 11, -1],
        [3, 11, 2, -1],
        [1, 8, 3, 9, 8, 1, -1],
        [1, 9, 0, -1],
        [8, 3, 0, -1],
        [-1],
    ];
}
