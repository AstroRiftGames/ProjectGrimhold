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
        public NetworkPrefabRef[] Prefabs;
        public int amount;
    }

    [Header("Player")]
    [SerializeField]
    private PlayerClassCatalog _playerClassCatalog;

    [Header("Spawn Groups")]
    [SerializeField]
    private SpawnGroup[] _spawnGroups;

    private readonly Dictionary<PlayerRef, NetworkObject> _spawnedPlayers = new();
    private readonly Dictionary<PlayerRef, NetworkObject> _spawnedEnemies = new();

    private readonly Dictionary<SpawnGroupType, Transform[]> _spawnPointLookup = new();

    private NetworkRunner _runner;

    private void Awake()
    {
        if (_playerClassCatalog == null)
        {
            Debug.LogError("PlayerClassCatalog reference is missing on NetworkSpawnManager!", this);
        }
        else if (!_playerClassCatalog.TryValidate(out string error))
        {
            Debug.LogError($"PlayerClassCatalog validation failed: {error}", this);
            _playerClassCatalog = null;
        }

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

        for(int n = 0; n < _spawnGroups.Length; n++)
        {
            SpawnGroup group = _spawnGroups[n];
            if (group.Group == SpawnGroupType.Players)
                continue;
            for (int i = 0; i < group.amount; i++)
            {
                SpawnEnemy(runner, group.Group);
            }
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

    private bool TryGetJoinData(
        NetworkRunner runner,
        PlayerRef player,
        out PlayerJoinData joinData)
    {
        byte[] token = runner.GetPlayerConnectionToken(player);

        if (PlayerJoinDataCodec.TryDecode(token, out joinData))
        {
            return true;
        }

        if (!runner.IsServer || player != runner.LocalPlayer)
        {
            joinData = default;
            return false;
        }

        LocalPlayerJoinContext context = runner.GetComponent<LocalPlayerJoinContext>();

        if (context == null || !PlayerJoinDataCodec.IsSupported(context.JoinData.ClassId))
        {
            joinData = default;
            return false;
        }

        joinData = context.JoinData;
        return true;
    }

    private void SpawnPlayer(NetworkRunner runner, PlayerRef player)
    {
        if (_spawnedPlayers.ContainsKey(player))
        {
            Debug.Log($"Player {player} already spawned.");
            return;
        }

        if (_playerClassCatalog == null)
        {
            Debug.LogError($"Cannot spawn player {player}: PlayerClassCatalog is null or invalid.");
            return;
        }

        if (!TryGetJoinData(runner, player, out PlayerJoinData joinData))
        {
            Debug.LogError($"Rejecting spawn for player {player}: Invalid or missing join data.");
            return;
        }

        if (!_playerClassCatalog.TryGetPrefab(joinData.ClassId, out NetworkPrefabRef prefab))
        {
            Debug.LogError($"Rejecting spawn for player {player}: Class {joinData.ClassId} not registered or has invalid prefab.");
            return;
        }

        GetSpawnTransform(
            SpawnGroupType.Players,
            player.RawEncoded,
            out Vector3 position,
            out Quaternion rotation);

        NetworkObject playerObject = runner.Spawn(
            prefab,
            position,
            rotation,
            player);

        runner.SetPlayerObject(player, playerObject);

        _spawnedPlayers.Add(player, playerObject);

        Debug.Log($"Spawned player {player} with class {joinData.ClassId}.");
    }

    private void SpawnEnemy(NetworkRunner runner, SpawnGroupType groupType)
    {
        if (_spawnGroups[2] == null || _spawnGroups[2].Prefabs == null)
        {
            Debug.LogError("Cannot spawn enemy: Enemy prefab reference is missing.");
            return;
        }
        GetSpawnTransform(
            groupType,
            UnityEngine.Random.Range(0, int.MaxValue),
            out Vector3 position,
            out Quaternion rotation);
        NetworkObject enemyObject = runner.Spawn(
            _spawnGroups[2].Prefabs[UnityEngine.Random.Range(0, _spawnGroups[2].Prefabs.Length)],
            position,
            rotation);
        Debug.Log($"Spawned enemy of group {groupType} at {position}.");
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