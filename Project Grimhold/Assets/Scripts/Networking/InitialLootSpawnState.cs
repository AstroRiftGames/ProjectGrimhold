using System.Collections.Generic;
using Fusion;

/// <summary>
/// Tracks only loot containers successfully spawned by one runner-scoped initial scene generation.
/// </summary>
public sealed class InitialLootSpawnState
{
    private readonly HashSet<int> _occupiedPointIndices = new();
    private readonly List<NetworkObject> _spawnedObjects = new();

    public int Count => _spawnedObjects.Count;

    public bool ContainsPoint(int spawnPointIndex)
    {
        return _occupiedPointIndices.Contains(spawnPointIndex);
    }

    public bool TryRecordSuccessfulSpawn(int spawnPointIndex, NetworkObject spawnedObject)
    {
        if (spawnPointIndex < 0 || spawnedObject == null || _occupiedPointIndices.Contains(spawnPointIndex))
        {
            return false;
        }

        _occupiedPointIndices.Add(spawnPointIndex);
        _spawnedObjects.Add(spawnedObject);
        return true;
    }

    public void Clear()
    {
        _occupiedPointIndices.Clear();
        _spawnedObjects.Clear();
    }
}
