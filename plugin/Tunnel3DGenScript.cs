using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
public partial class Tunnel3D : Node3D
{
    private async void QueueTask(Action action)
    {
        if (taskQueue.Count == 0) { doingTasks = false; }
        taskQueue.Enqueue(() => { methodRunFlag = true; action(); });
        if (!doingTasks && taskQueue.Count > 0)
        {
            doingTasks = true;
            await RunQueue();
            doingTasks = false;
            EmitSignal(SignalName.TunnelLoaded);
        }
    }

    private async Task RunQueue()
    {
        while (taskQueue.Count > 0)
        {
            try
            {
                taskQueue.Peek().Invoke();
            }
            catch
            {
                doingTasks = false;
                methodRunFlag = false;
                taskQueue.Clear();
                throw;
            }
            await Task.Run(WaitForCompletion);
            taskQueue.Dequeue();

        }
    }

    // WorkerThreadPool.WaitForGroupTaskCompletion() was already considered, however there were issues.
    // Either solution would block a thread anyway, so it isn't a big deal.
    private void WaitForCompletion()
    {
        while (!PoolFinished || methodRunFlag) { }
    }

    private void GenTunnelMesh()
    {
        if (!IsNodeReady())
        {
            throw new Exception("Tunnel3D not initialised");
        }
        if (_meshData == null)
        {
            throw new NullReferenceException("Tunnel3D Mesh Data Resource is null");
        }
        if (_voxelData == null)
        {
            throw new NullReferenceException("Tunnel3D Voxel Data Resource is null");
        }
        if (_voxelData.VoxelWeights is null)
        {
            throw new NullReferenceException("Tunnel3D MeshData.VoxelWeights Array is null");
        }
        if (_voxelData.VoxelWeights.Length != _voxelData.ChunkCount.X * _voxelData.ChunkCount.Y * _voxelData.ChunkCount.Z * _voxelData.VoxelCount.X * _voxelData.VoxelCount.Y * _voxelData.VoxelCount.Z)
        {
            throw new Exception("Tunnel3D VoxelWeights.Length mismatch with voxel and chunk count. Re-generate Generation Data.");
        }

        _meshData.TunnelMeshes = new Godot.Collections.Array<ArrayMesh>();
        _meshData.TunnelMeshes.Resize(_voxelData.ChunkCount.X * _voxelData.ChunkCount.Y * _voxelData.ChunkCount.Z);
        _meshData.TunnelChunks = _voxelData.ChunkCount;
        _meshData.TunnelVolume = _voxelData.VolumeSize;
        taskID = WorkerThreadPool.AddGroupTask(Callable.From<int>(GenerateChunkMesh), _meshData.TunnelMeshes.Count);
        methodRunFlag = false;
    }

    private void GenTunnelData()
    {
        if (!IsNodeReady()) { throw new Exception("Tunnel3D not initialised"); }
        if (_tunnelData is null) { throw new NullReferenceException("Tunnel3D Generation Data Resource is null"); }
        if (_generator is null) { throw new NullReferenceException("Tunnel3D Connection Generator Resource is null"); }
        if (!(_generator is null))
        {
            int count = _generator.GeneratedNodeCount;

            try
            {
                count += _generator.PresetNodes.Length;
            }
            catch { }
            if (count <= 0)
            {
                _tunnelData.TunnelNodes = new Vector3[0];
                _tunnelData.AdjacencyMatrix = new byte[0];
                _tunnelData.WeightMatrix = new float[0];
                GD.PushWarning("Tunnel3D Data Generator generates zero nodes. Generating empty Generation Data Resource.");
                methodRunFlag = false;
                return;
            }
            else
            {
                Task.Run(() => {GenerateGenerationData(); methodRunFlag = false;});
                return;
            }
        }
        else if (_tunnelData.TunnelNodes is null || _tunnelData.AdjacencyMatrix is null || _tunnelData.WeightMatrix is null)
        {
            
            throw new Exception("Tunnel3D Generation Data Resource data is incomplete or null.");
        }
        
    }

    private void GenMeshChildren()
    {
        if (!IsNodeReady()) { throw new Exception("Tunnel3D not initialised"); }
        if (tunnelContainer.GetParent() is null) { AddChild(tunnelContainer); }
        if (_meshData is null) { throw new NullReferenceException("Tunnel3D Mesh Data Resource is null"); }
        if (_meshData.TunnelMeshes is null) { throw new NullReferenceException("Tunnel3D MeshData.TunnelMeshes is null"); }

        int chunkCount;
        try
        {
            chunkCount = _meshData.TunnelChunks.X * _meshData.TunnelChunks.Y * _meshData.TunnelChunks.Z;
            if (chunkCount != _meshData.TunnelMeshes.Count)
            {
                GD.PushWarning("Tunnel3D ChunkCount and tunnel ArrayMesh count mismatch. Data is dirty, re-generate MeshData. Continuing with Generating Mesh Children.");
            }
        }
        catch
        {
            chunkCount = _meshData.TunnelMeshes.Count;
        }

        foreach (var child in tunnelContainer.GetChildren())
        {
            tunnelContainer.RemoveChild(child);
            child.Free();
        }
        MeshInstance3D[] chunks = new MeshInstance3D[chunkCount];

        for (int i = 0; i < chunkCount; i++)
        {
            try // try-catch necessary if _meshData.TunnelMeshes[i] is null
            {
                if (_meshData.TunnelMeshes[i].GetSurfaceCount() == 0) { continue; } // Don't create MeshInstance3Ds of empty meshes
            }
            catch { continue; }
            chunks[i] = new MeshInstance3D { Mesh = _meshData.TunnelMeshes[i], MaterialOverride = _meshData.TunnelMaterial, Position = GetRealCoordinateChunkOffsetFromChunkIndex(i) };

            tunnelContainer.AddChild(chunks[i]);

        }

        if (!_generateCollisionMeshes) { methodRunFlag = false; return; }

        Callable collisionGen = Callable.From<int>((i) =>
        {
            try // Return if empty/null
            {
                if (_meshData.TunnelMeshes[i].GetSurfaceCount() == 0) { return; }
            }
            catch { return; }
            chunks[i].CreateTrimeshCollision(); // Really expensive. Adds children, so must be run on main thread, meaning this is blocking.
            ((StaticBody3D)chunks[i].GetChild(0)).CollisionLayer = _collisionLayer;
            ((StaticBody3D)chunks[i].GetChild(0)).CollisionMask = _collisionMask;
        });

        for (int i = 0; i < chunkCount; i++)
        {
            collisionGen.CallDeferred(i); // CallDeferred to mitigate blocking, however blocking will still occur if meshes are sufficiently complex
        }
        methodRunFlag = false;
    }

    private void DestroyChildren()
    {
        if (!IsNodeReady())
        {
            throw new Exception("Tunnel3D not initialised");
        }

        foreach (var child in tunnelContainer.GetChildren())
        {
            tunnelContainer.RemoveChild(child);
            child.Free();
        }
        methodRunFlag = false;
    }

    private void GenVoxelData()
    {
        if (!IsNodeReady())
        {
            throw new Exception("Tunnel3D not initialised");
        }
        if (_voxelData is null)
        {
            throw new Exception("Tunnel3D Voxel Data Resource is null");
        }
        if (_tunnelData is null)
        {
            throw new Exception("Tunnel3D Generation Data Resource is null");
        }
        if (_tunnelData.AdjacencyMatrix is null || _tunnelData.AdjacencyMatrix.Length == 0)
        {
            throw new Exception("Tunnel3D GenerationData.AdjacencyMatrix is null or empty");
        }

        if (_tunnelData.TunnelEaseFunction is null)
        {
            throw new Exception("Tunnel3D GenerationData.TunnelEaseFunction is null.");
        }

        if (_tunnelData.TunnelNodes is null)
        {
            GD.PushWarning("Tunnel3D GenerationData.TunnelNodes is null. Generating empty voxel grid.");
        }
        else if (_tunnelData.TunnelNodes.Length == 0)
        {
            GD.PushWarning("Tunnel3D GenerationData.TunnelNodes is null. Generating empty voxel grid.");
        }
        else if (_tunnelData.TunnelNodes.Length * _tunnelData.TunnelNodes.Length != (_tunnelData.AdjacencyMatrix?.Length ?? 0))
        {
            throw new Exception("Tunnel3D Adjacency Matrix mismatch with Tunnel Node count.");
        }

        byte[] weights = new byte[_voxelData.ChunkCount.X * _voxelData.VoxelCount.X * _voxelData.ChunkCount.Y * _voxelData.VoxelCount.Y * _voxelData.ChunkCount.Z * _voxelData.VoxelCount.Z];
        List<(Line Tunnel, Vector3I Corner1AABB, Vector3I Corner2AABB)> tunnels = new List<(Line Tunnel, Vector3I Corner1AABB, Vector3I Corner2AABB)>();

        for (int x = 0; x < _tunnelData.TunnelNodes.Length; x++)
        {
            for (int y = x + 1; y < _tunnelData.TunnelNodes.Length; y++)
            {
                if (_tunnelData.AdjacencyMatrix[x + y * _tunnelData.TunnelNodes.Length] == 0) { continue; }

                Vector3I voxelDimensions = _voxelData.VoxelCount * _voxelData.ChunkCount;
                Vector3 corner1Real = _tunnelData.TunnelNodes[x];
                Vector3 corner2Real = _tunnelData.TunnelNodes[y];

                for (int i = 0; i < 3; i++) // offsets AABB to include tunnel radius and clamp to volume bounds
                {
                    corner1Real[i] = Math.Clamp(MathF.Min(_tunnelData.TunnelNodes[x][i], _tunnelData.TunnelNodes[y][i]) - _tunnelData.TunnelRadius, _voxelData.VolumeSize[i] / -2.0f, _voxelData.VolumeSize[i] / 2.0f);
                    corner2Real[i] = Math.Clamp(MathF.Max(_tunnelData.TunnelNodes[x][i], _tunnelData.TunnelNodes[y][i]) + _tunnelData.TunnelRadius, _voxelData.VolumeSize[i] / -2.0f, _voxelData.VolumeSize[i] / 2.0f);
                }

                Line tunnel = new Line(_tunnelData.TunnelNodes[x], _tunnelData.TunnelNodes[y]);
                Vector3 corner1VoxelReal = ((corner1Real + _voxelData.VolumeSize * 0.5f) / (_voxelData.VolumeSize)) * voxelDimensions;
                Vector3 corner2VoxelReal = ((corner2Real + _voxelData.VolumeSize * 0.5f) / (_voxelData.VolumeSize)) * voxelDimensions;

                Vector3I corner1Voxel = new Vector3I // clamping to prevent any out of bounds voxels due to floating point errors
                {
                    X = Math.Clamp((int)MathF.Floor(corner1VoxelReal.X), 0, voxelDimensions.X),
                    Y = Math.Clamp((int)MathF.Floor(corner1VoxelReal.Y), 0, voxelDimensions.Y),
                    Z = Math.Clamp((int)MathF.Floor(corner1VoxelReal.Z), 0, voxelDimensions.Z)
                };

                Vector3I corner2Voxel = new Vector3I
                {
                    X = Math.Clamp((int)MathF.Ceiling(corner2VoxelReal.X), 0, voxelDimensions.X),
                    Y = Math.Clamp((int)MathF.Ceiling(corner2VoxelReal.Y), 0, voxelDimensions.Y),
                    Z = Math.Clamp((int)MathF.Ceiling(corner2VoxelReal.Z), 0, voxelDimensions.Z)
                };


                tunnels.Add((tunnel, corner1Voxel, corner2Voxel));
            }
        }

        Callable call = Callable.From<int>((index) => { GenerateVoxelWeightsInAABB(tunnels[index], weights); });
        taskID = WorkerThreadPool.AddGroupTask(call, tunnels.Count);

        _voxelData.VoxelWeights = weights;
        methodRunFlag = false;
    }


    private void GenerateVoxelWeightsInAABB((Line Tunnel, Vector3I Corner1, Vector3I Corner2) AABB, byte[] weights)
    {
        bool processNoise = !(_tunnelData.Noise is null) && _tunnelData.NoiseIntensity != 0;
        Vector3I voxelDimensions = _voxelData.VoxelCount * _voxelData.ChunkCount;
        Vector3 position;
        int index;
        float normalisedTunnelDist;
        float weightValue;

        for (int x = AABB.Corner1.X; x < AABB.Corner2.X; x++)
        {
            for (int y = AABB.Corner1.Y; y < AABB.Corner2.Y; y++)
            {
                for (int z = AABB.Corner1.Z; z < AABB.Corner2.Z; z++)
                {
                    position = GetRealCoordinateFromVoxel(new Vector3I(x, y, z));

                    index = x + y * voxelDimensions.X + z * voxelDimensions.X * voxelDimensions.Y;

                    normalisedTunnelDist = Line.DistanceLineToPoint(AABB.Tunnel, position) / _tunnelData.TunnelRadius;
                    weightValue = _tunnelData.TunnelEaseFunction.Sample(Math.Clamp(normalisedTunnelDist, 0.0f, 1.0f));

                    if (normalisedTunnelDist < 1.0f && processNoise)
                    {
                        weightValue += (((_tunnelData.Noise.GetNoise3Dv(position * 100.0f) + 1.0f) / 2.0f) * _tunnelData.NoiseIntensity); // 100x multiplier to noise because noise has max frequency of 1.0, and at that frequency, the best results are found.
                    }

                    lock (lockObj) // gotta love race conditions
                    {
                        weights[index] = Math.Max(weights[index], (byte)(Math.Clamp(weightValue, 0.0f, 1.0f) * 255));
                    }
                }
            }
        }
    }

    private void GenerateGenerationData()
    {
        Random random = new Random(_generator.Seed);

        int nodeCount = _generator.GeneratedNodeCount;
        int presetNodesLength = 0;

        bool nodeEmpty = _generator.PresetNodes is null;
        if (!nodeEmpty)
        {
            nodeCount += _generator.PresetNodes.Length;
            presetNodesLength = _generator.PresetNodes.Length;
        }

        byte[] adjacencyMatrix = new byte[nodeCount * nodeCount];
        float[] weightMatrix = new float[nodeCount * nodeCount];

        Vector3[] nodes = new Vector3[nodeCount];

        if (!nodeEmpty)
        {
            Array.Copy(_generator.PresetNodes, nodes, _generator.PresetNodes.Length);
        }
        for (int i = presetNodesLength; i < nodeCount; i++)
        {
            int iterations = 0;
            bool isValid = false;

            Vector3 randomCoordinate;
            do
            {
                float separationDistance = _generator.NodeSeparationDistance * Math.Clamp((10 - iterations) / 10.0f, 0.0f, 1.0f); // gradually reduces separation distance to prevent infinite loop whilst producing acceptable result
                randomCoordinate = new Vector3
                {
                    X = Mathf.Lerp(_generator.BoundsLowerCorner.X, _generator.BoundsUpperCorner.X, random.NextSingle()),
                    Y = Mathf.Lerp(_generator.BoundsLowerCorner.Y, _generator.BoundsUpperCorner.Y, random.NextSingle()),
                    Z = Mathf.Lerp(_generator.BoundsLowerCorner.Z, _generator.BoundsUpperCorner.Z, random.NextSingle())
                };

                isValid = true;
                for (int node1 = 0; node1 < i; node1++)
                {
                    for (int node2 = node1 + 1; node2 < i; node2++)
                    {
                        isValid &= Line.DistanceLineToPoint(new Line(nodes[node1], nodes[node2]), randomCoordinate) > separationDistance;

                        if (!isValid) { break; }
                    }
                    if (!isValid) { break; }
                }
                iterations++;
            } while (!isValid && iterations < 10);
            nodes[i] = randomCoordinate;
        }
        if (nodeCount < 2) { return; }

        PriorityQueue<(int Node1, int Node2, float Weight, bool Reshuffled), double> queue = new PriorityQueue<(int Node1, int Node2, float Weight, bool Reshuffled), double>();
        Line[,] tunnels = new Line[nodeCount, nodeCount];

        int index = 0;
        double maxWeight = 0.0;

        for (int i = 0; i < nodeCount - 1; i++)
        {
            for (int j = i + 1; j < nodeCount; j++)
            {
                Vector3 difference = (nodes[i] - nodes[j]);
                float weight = difference.Length();

                weight += weight * (Math.Clamp(Math.Abs(difference.Y) / ((difference with { Y = 0 }).Length()), 0, 1000) * _generator.ElevationAspect);

                maxWeight = Math.Max(maxWeight, weight);

                weightMatrix[i + j * nodeCount] = weight;
                weightMatrix[j + i * nodeCount] = weight;

                queue.Enqueue((i, j, weightMatrix[j + i * nodeCount], false), weight);

                tunnels[i, j] = new Line(nodes[i], nodes[j]);
                tunnels[j, i] = tunnels[i, j];

                index++;
            }
        }
        maxWeight = (int)maxWeight + 1.0f;

        (int, bool) intersectionCount((int X, int Y, float Weight, bool Reshuffled) connection)
        {
            int count = 0;
            bool isValid = true;
            for (int x = 0; x < nodeCount; x++)
            {
                if (x == connection.X || x == connection.Y) { continue; }
                for (int y = x + 1; y < nodeCount; y++)
                {
                    if (y == connection.Y || y == connection.X) { continue; }
                    if (_generator.ThresholdRadius > Line.DistanceLineToLine(tunnels[connection.X, connection.Y], tunnels[x, y]))
                    {
                        count++;
                        isValid &= adjacencyMatrix[x + y * nodeCount] == 0;
                    }
                }
            }
            return (count, isValid);
        }

        //Modified Prim's

        List<(int Node1, int Node2, float Weight, bool Reshuffled)> buffer = new List<(int Node1, int Node2, float Weight, bool Reshuffled)>();
        HashSet<int> visitedNodes = new HashSet<int>();

        bool bufferStuck = false;
        int connectionCount = 0;
        while (connectionCount < Math.Max(nodeCount - 1, _generator.GeneratedConnectionCount))
        {
            if (connectionCount > nodeCount - 1 || (buffer.Count > 0 && queue.Count == 0 && !bufferStuck))
            {
                bufferStuck = true;
                while (buffer.Count > 0)
                {
                    (int Node1, int Node2, float Weight, bool Reshuffled) bufferItem = buffer[0];
                    buffer.RemoveAt(0);
                    queue.Enqueue(bufferItem, bufferItem.Weight);
                }
            }

            if (queue.Count == 0) { break; }

            (int Node1, int Node2, float Weight, bool Reshuffled) current = queue.Dequeue();

            if (!(visitedNodes.Contains(current.Node1) ^ visitedNodes.Contains(current.Node2)) && connectionCount != 0 && connectionCount < nodeCount - 1)
            {
                buffer.Add(current);
                continue;
            }

            if (!_generator.TestIntersections)
            {
                bufferStuck = false;
                visitedNodes.Add(current.Node1);
                visitedNodes.Add(current.Node2);
                adjacencyMatrix[current.Node1 + current.Node2 * nodeCount] = 1;
                adjacencyMatrix[current.Node2 + current.Node1 * nodeCount] = 1;
                connectionCount++;

                for (int i = 0; i < buffer.Count; i++)
                {
                    (int, int, float, bool) bufferItem = buffer[i];
                    if (visitedNodes.Contains(bufferItem.Item1) ^ visitedNodes.Contains(bufferItem.Item2))
                    {
                        queue.Enqueue(bufferItem, bufferItem.Item3);
                        buffer.RemoveAt(i);
                        i--;
                    }
                }
                continue;
            }
            (int Count, bool IsValid) intersections = intersectionCount(current);

            if (!intersections.IsValid)
            {
                bufferStuck = false;
                continue;
            }

            if (intersections.Count != 0 && !current.Reshuffled)
            {
                bufferStuck = false;
                queue.Enqueue(current with { Reshuffled = true }, current.Weight + maxWeight * intersections.Count);
                continue;
            }
            visitedNodes.Add(current.Node1);
            visitedNodes.Add(current.Node2);
            adjacencyMatrix[current.Node1 + current.Node2 * nodeCount] = 1;
            adjacencyMatrix[current.Node2 + current.Node1 * nodeCount] = 1;

            for (int i = 0; i < buffer.Count; i++)
            {
                (int, int, float, bool) bufferItem = buffer[i];
                if (visitedNodes.Contains(bufferItem.Item1) ^ visitedNodes.Contains(bufferItem.Item2))
                {
                    queue.Enqueue(bufferItem, bufferItem.Item3);
                    buffer.RemoveAt(i);
                    i--;
                }
            }
            connectionCount++;
            bufferStuck = false;
        }
        _tunnelData.AdjacencyMatrix = adjacencyMatrix;
        _tunnelData.WeightMatrix = weightMatrix;
        _tunnelData.TunnelNodes = nodes;
    }

    private Vector3 GetRealCoordinateChunkOffsetFromChunkIndex(int i)
    {
        int chunkX = i % (_meshData.TunnelChunks.X);
        int chunkY = i / _meshData.TunnelChunks.X % (_meshData.TunnelChunks.Y);
        int chunkZ = i / _meshData.TunnelChunks.X / _meshData.TunnelChunks.Y % (_meshData.TunnelChunks.Z);
        return (new Vector3(chunkX, chunkY, chunkZ) / _meshData.TunnelChunks - 0.5f * Vector3.One) * _meshData.TunnelVolume;
    }

    private Vector3 GetRealCoordinateFromChunk(Vector3 vec)
    {
        return vec * (_voxelData.VolumeSize / _voxelData.ChunkCount / _voxelData.VoxelCount);
    }

    private Vector3 GetRealCoordinateFromIndex(int i)
    {
        int voxelX = i % (_voxelData.ChunkCount.X * _voxelData.VoxelCount.X);
        int voxelY = i / (_voxelData.ChunkCount.X * _voxelData.VoxelCount.X) % (_voxelData.ChunkCount.Y * _voxelData.VoxelCount.Y);
        int voxelZ = i / (_voxelData.ChunkCount.X * _voxelData.VoxelCount.X) / (_voxelData.ChunkCount.Y * _voxelData.VoxelCount.Y) % (_voxelData.ChunkCount.Z * _voxelData.VoxelCount.Z);
        return new Vector3(voxelX, voxelY, voxelZ) / (_voxelData.VoxelCount * _voxelData.ChunkCount - 0.5f * Vector3.One) * _voxelData.VolumeSize;
    }

    private Vector3 GetRealCoordinateFromVoxel(Vector3I voxel)
    {
        float realX = ((float)voxel.X / (_voxelData.VoxelCount.X * _voxelData.ChunkCount.X) - 0.5f) * _voxelData.VolumeSize.X;
        float realY = ((float)voxel.Y / (_voxelData.VoxelCount.Y * _voxelData.ChunkCount.Y) - 0.5f) * _voxelData.VolumeSize.Y;
        float realZ = ((float)voxel.Z / (_voxelData.VoxelCount.Z * _voxelData.ChunkCount.Z) - 0.5f) * _voxelData.VolumeSize.Z;

        return new Vector3(realX, realY, realZ);
    }

    private float ReadVoxelValues(int x, int y, int z)
    {
        Vector3I voxelSize = _voxelData.VoxelCount;
        Vector3I chunkSize = _voxelData.ChunkCount;
        if (x <= 0 || y <= 0 || z <= 0 || x >= voxelSize.X * chunkSize.X || y >= voxelSize.Y * chunkSize.Y || z >= voxelSize.Z * chunkSize.Z)
        {
            return 0.0f;
        }

        int byteIndex = x + y * (voxelSize.X * chunkSize.X) + z * (voxelSize.X * chunkSize.X * voxelSize.Y * chunkSize.Y);
        return (_voxelData.VoxelWeights[byteIndex]) / 255.0f;
    }

    private void GenerateChunkMesh(int index) // Marching cubes
    {
        SurfaceTool workST = new SurfaceTool();

        List<int> verticesMarkedForDeletion = new List<int>();

        int deletionIndex = 0;
        bool isMeshEmptyFlag = true;

        workST.Begin(Mesh.PrimitiveType.Triangles);

        int xChunkOffset = (index % _voxelData.ChunkCount.X) * _voxelData.VoxelCount.X;
        int yChunkOffset = ((index / _voxelData.ChunkCount.X) % _voxelData.ChunkCount.Y) * _voxelData.VoxelCount.Y;
        int zChunkOffset = ((index / _voxelData.ChunkCount.X / _voxelData.ChunkCount.Y) % _voxelData.ChunkCount.Z) * _voxelData.VoxelCount.Z;

        float[] localWeights;
        int meshComponentIndex;
        int[] triangleEdges;
        int[] edges;
        float t;
        Vector3 corner1;
        Vector3 corner2;
        Vector3 interpolatedVertex;
        bool invert = _voxelData.InvertTunnel;
        float tunnelLevel = _voxelData.TunnelLevel;

        for (int z = -1; z < _voxelData.VoxelCount.Z + 1; z++)
        {
            for (int y = -1; y < _voxelData.VoxelCount.Y + 1; y++)
            {
                for (int x = -1; x < _voxelData.VoxelCount.X + 1; x++)
                {
                    localWeights = new float[8] {
                        ReadVoxelValues(x+xChunkOffset,  y+yChunkOffset,  z+zChunkOffset) * (invert ? -1 : 1) + (invert ? 1 : 0),
                        ReadVoxelValues(x+xChunkOffset+1,y+yChunkOffset,  z+zChunkOffset) * (invert ? -1 : 1) + (invert ? 1 : 0),
                        ReadVoxelValues(x+xChunkOffset,  y+yChunkOffset+1,z+zChunkOffset) * (invert ? -1 : 1) + (invert ? 1 : 0),
                        ReadVoxelValues(x+xChunkOffset+1,y+yChunkOffset+1,z+zChunkOffset) * (invert ? -1 : 1) + (invert ? 1 : 0),
                        ReadVoxelValues(x+xChunkOffset,  y+yChunkOffset,  z+zChunkOffset+1) * (invert ? -1 : 1) + (invert ? 1 : 0),
                        ReadVoxelValues(x+xChunkOffset+1,y+yChunkOffset,  z+zChunkOffset+1) * (invert ? -1 : 1) + (invert ? 1 : 0),
                        ReadVoxelValues(x+xChunkOffset,  y+yChunkOffset+1,z+zChunkOffset+1) * (invert ? -1 : 1) + (invert ? 1 : 0),
                        ReadVoxelValues(x+xChunkOffset+1,y+yChunkOffset+1,z+zChunkOffset+1) * (invert ? -1 : 1) + (invert ? 1 : 0),
                    };

                    meshComponentIndex = 0;
                    for (int i = 0; i < localWeights.Length; i++)
                    {
                        meshComponentIndex += Convert.ToInt32(localWeights[i] > tunnelLevel) << i;
                    }

                    triangleEdges = triangulationTable[meshComponentIndex];


                    for (int i = 0; i < triangleEdges.Length; i++)
                    {
                        if (triangleEdges[i] == -1) { break; }

                        edges = edgeVertexLUT[triangleEdges[i]];
                        t = Mathf.InverseLerp(localWeights[edges[0]], localWeights[edges[1]], tunnelLevel);

                        corner1 = GetRealCoordinateFromChunk(cornerPositions[edges[0]] + new Vector3 { X = x, Y = y, Z = z });
                        corner2 = GetRealCoordinateFromChunk(cornerPositions[edges[1]] + new Vector3 { X = x, Y = y, Z = z });

                        interpolatedVertex = new Vector3 // interpolate between corner positions to get edge position
                        {
                            X = Mathf.Lerp(corner1.X, corner2.X, t),
                            Y = Mathf.Lerp(corner1.Y, corner2.Y, t),
                            Z = Mathf.Lerp(corner1.Z, corner2.Z, t)
                        };


                        workST.AddVertex(interpolatedVertex);
                        if (z == _voxelData.VoxelCount.Z || y == _voxelData.VoxelCount.Y || x == _voxelData.VoxelCount.X || x == -1 || y == -1 || z == -1)
                        {
                            verticesMarkedForDeletion.Add(deletionIndex);
                        }
                        else
                        {
                            isMeshEmptyFlag = false;
                        }
                        deletionIndex++;
                    }
                }
            }
        }

        if (isMeshEmptyFlag) { _meshData.TunnelMeshes[index] = new ArrayMesh(); return; }

        // Builds normals with outer mesh margin included

        workST.GenerateNormals();

        // Strips outer meshes from original mesh

        Godot.Collections.Array inputArray = workST.CommitToArrays();

        Vector3[] inputVertices = (Vector3[])inputArray[0];
        Vector3[] inputNormals = (Vector3[])inputArray[1];

        Vector3[] vertices = new Vector3[inputVertices.Length - verticesMarkedForDeletion.Count];
        Vector3[] normals = new Vector3[inputNormals.Length - verticesMarkedForDeletion.Count];

        int offset = 0;

        for (int i = 0; i < inputVertices.Length; i++)
        {
            if (verticesMarkedForDeletion.Contains(i))
            {
                offset++;
            }
            else
            {
                vertices[i - offset] = inputVertices[i];
                normals[i - offset] = inputNormals[i];
            }
        }

        Godot.Collections.Array surfaceArrays = [];
        surfaceArrays.Resize((int)Mesh.ArrayType.Max);

        surfaceArrays[(int)Mesh.ArrayType.Vertex] = vertices;
        surfaceArrays[(int)Mesh.ArrayType.Normal] = normals;

        ArrayMesh outMesh = new ArrayMesh();

        outMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surfaceArrays);

        _meshData.TunnelMeshes[index] = outMesh;
    }

}
