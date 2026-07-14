using Fusion;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public sealed class NetworkSpawnManager : NetworkRunnerCallbacksAdapter
{
    public enum SpawnGroupType
    {
        Players,
        Enemies,
        Loot,
        NPCs,
        Bosses,
        Misc
    }

    [Serializable]
    public class SpawnGroup
    {
        public SpawnGroupType Group;
        public Transform[] SpawnPoints;
    }

    [Header("Player")]
    [SerializeField]
    private NetworkPrefabRef _playerPrefab;

    [Header("Spawn Groups")]
    [SerializeField]
    private SpawnGroup[] _spawnGroups;

    private readonly Dictionary<PlayerRef, NetworkObject> _spawnedPlayers = new();

    private readonly Dictionary<SpawnGroupType, Transform[]> _spawnPointLookup = new();

    private NetworkRunner _runner;

    private void Awake()
    {
        _spawnPointLookup.Clear();

        foreach (SpawnGroup group in _spawnGroups)
        {
            if (!_spawnPointLookup.ContainsKey(group.Group))
            {
                _spawnPointLookup.Add(group.Group, group.SpawnPoints);
            }
        }
    }

    private void Start()
    {
        _runner = FindAnyObjectByType<NetworkRunner>();

        _runner.AddCallbacks(this);
    }

    public override void OnSceneLoadDone(NetworkRunner runner)
    {
        if (!runner.IsServer)
            return;

        foreach (PlayerRef player in runner.ActivePlayers)
        {
            SpawnPlayer(runner, player);
        }
    }

    public override void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"PlayerJoined: {player}");

        if (!runner.IsServer)
        {
            Debug.Log("Not server, skipping spawn");
            return;
        }

        SpawnPlayer(runner, player);
    }

    private void SpawnPlayer(NetworkRunner runner, PlayerRef player)
    {
        if (_spawnedPlayers.ContainsKey(player))
        {
            Debug.Log($"Player {player} already spawned.");
            return;
        }

        GetSpawnTransform(
            SpawnGroupType.Players,
            player.RawEncoded,
            out Vector3 position,
            out Quaternion rotation);

        NetworkObject playerObject = runner.Spawn(
            _playerPrefab,
            position,
            rotation,
            player);

        runner.SetPlayerObject(player, playerObject);

        _spawnedPlayers.Add(player, playerObject);

        Debug.Log($"Spawned player {player}.");
    }

    public override void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (!runner.IsServer)
            return;

        if (!_spawnedPlayers.Remove(player, out NetworkObject playerObject))
            return;

        if (playerObject != null)
        {
            runner.Despawn(playerObject);
        }

        Debug.Log($"Despawned player {player}.");
    }

    public NetworkObject Spawn(
        NetworkPrefabRef prefab,
        SpawnGroupType group)
    {
        return Spawn(prefab, group, UnityEngine.Random.Range(0, int.MaxValue));
    }

    public NetworkObject Spawn(
        NetworkPrefabRef prefab,
        SpawnGroupType group,
        int spawnSeed)
    {
        if (_runner == null)
        {
            Debug.LogError("NetworkRunner not initialized.");
            return null;
        }

        if (!_runner.IsServer)
        {
            Debug.LogWarning("Only the server can spawn NetworkObjects.");
            return null;
        }

        GetSpawnTransform(
            group,
            spawnSeed,
            out Vector3 position,
            out Quaternion rotation);

        return _runner.Spawn(
            prefab,
            position,
            rotation);
    }

    public NetworkObject Spawn(
        NetworkPrefabRef prefab,
        Vector3 position,
        Quaternion rotation)
    {
        if (_runner == null)
        {
            Debug.LogError("NetworkRunner not initialized.");
            return null;
        }

        if (!_runner.IsServer)
        {
            Debug.LogWarning("Only the server can spawn NetworkObjects.");
            return null;
        }

        return _runner.Spawn(
            prefab,
            position,
            rotation);
    }

    private void GetSpawnTransform(
        SpawnGroupType group,
        int seed,
        out Vector3 position,
        out Quaternion rotation)
    {
        if (!_spawnPointLookup.TryGetValue(group, out Transform[] spawnPoints))
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;
            return;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;
            return;
        }

        int spawnIndex = Mathf.Abs(seed) % spawnPoints.Length;

        Transform spawnPoint = spawnPoints[spawnIndex];

        position = spawnPoint.position;
        rotation = spawnPoint.rotation;
    }
}