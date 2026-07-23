using System.Collections.Generic;
using Fusion;

/// <summary>
/// Tracks breakables successfully spawned at scene-configured points for one
/// runner-scoped scene-load generation.
/// </summary>
public sealed class InitialBreakableSpawnState
{
    private readonly HashSet<int> _occupiedPointIndices = new();
    private readonly List<NetworkObject> _spawnedObjects = new();

    public int Count => _spawnedObjects.Count;

    /// <summary>Reports whether this generation already consumed the point.</summary>
    public bool ContainsPoint(int spawnPointIndex)
    {
        return _occupiedPointIndices.Contains(spawnPointIndex);
    }

    /// <summary>Records one successfully initialized network object for a point.</summary>
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

    /// <summary>Resets all runner-local state for a new scene generation or shutdown.</summary>
    public void Clear()
    {
        _occupiedPointIndices.Clear();
        _spawnedObjects.Clear();
    }
}
