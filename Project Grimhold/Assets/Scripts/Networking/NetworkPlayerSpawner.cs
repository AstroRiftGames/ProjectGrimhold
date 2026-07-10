using Fusion;
using System.Collections.Generic;
using UnityEngine;

public sealed class NetworkPlayerSpawner : NetworkRunnerCallbacksAdapter
{
    [Header("Player")]
    [SerializeField]
    private NetworkPrefabRef _playerPrefab;

    [Header("Spawn Points")]
    [SerializeField]
    private Transform[] _spawnPoints;

    private readonly Dictionary<PlayerRef, NetworkObject>
        _spawnedPlayers = new();

    public override void OnPlayerJoined(
        NetworkRunner runner,
        PlayerRef player)
    {
        if (!runner.IsServer)
        {
            return;
        }

        if (_spawnedPlayers.ContainsKey(player))
        {
            return;
        }

        GetSpawnTransform(
            player,
            out Vector3 position,
            out Quaternion rotation);

        NetworkObject playerObject = runner.Spawn(
            _playerPrefab,
            position,
            rotation,
            player);

        runner.SetPlayerObject(player, playerObject);
        _spawnedPlayers.Add(player, playerObject);

        Debug.Log(
            $"Spawned network player {player}.",
            playerObject);
    }

    public override void OnPlayerLeft(
        NetworkRunner runner,
        PlayerRef player)
    {
        if (!runner.IsServer)
        {
            return;
        }

        if (!_spawnedPlayers.Remove(
                player,
                out NetworkObject playerObject))
        {
            return;
        }

        if (playerObject != null)
        {
            runner.Despawn(playerObject);
        }

        Debug.Log($"Despawned network player {player}.");
    }

    private void GetSpawnTransform(
        PlayerRef player,
        out Vector3 position,
        out Quaternion rotation)
    {
        if (_spawnPoints == null || _spawnPoints.Length == 0)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;
            return;
        }

        int spawnIndex =
            player.RawEncoded % _spawnPoints.Length;

        Transform spawnPoint = _spawnPoints[spawnIndex];

        position = spawnPoint.position;
        rotation = spawnPoint.rotation;
    }
}