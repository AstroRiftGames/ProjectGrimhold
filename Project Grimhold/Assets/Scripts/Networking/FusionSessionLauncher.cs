using Fusion;
using System;
using System.Threading.Tasks;
using UnityEngine;

public sealed class FusionSessionLauncher : MonoBehaviour
{
    [Header("Session")]
    [SerializeField]
    private string _sessionName;

    [SerializeField, Min(1)]
    private int _maxPlayers = 4;

    private NetworkRunner _runner;
    private GameObject _runnerObject;
    private bool _isStarting;

    public NetworkRunner Runner => _runner;

    public async Task<bool> StartSessionAsync(string sessionName, GameMode mode)
    {
        if (string.IsNullOrEmpty(sessionName) || string.IsNullOrWhiteSpace(sessionName))
            throw new Exception("Invalid session code. The code cannot be empty or null.");

        if (_isStarting || _runner != null)
            return false;

        _isStarting = true;

        _runnerObject = new GameObject("NetworkRunner");
        _runner = _runnerObject.AddComponent<NetworkRunner>();
        _runnerObject.AddComponent<EntityRegistry>();
        DontDestroyOnLoad(_runnerObject);
        _runner.ProvideInput = true;

        try
        {
            StartGameResult result = await _runner.StartGame(
                new StartGameArgs
                {
                    GameMode = mode,
                    SessionName = sessionName,
                    PlayerCount = _maxPlayers
                });

            if (!result.Ok)
            {
                Debug.LogError(
                    $"Fusion failed to start. Reason: {result.ShutdownReason}",
                    this);

                DestroyRunner();
                return false;
            }

            Debug.Log(
                $"Fusion session started. " +
                $"Session: {_runner.SessionInfo.Name}. " +
                $"Peer: {(_runner.IsServer ? "Host" : "Client")}.",
                this);

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogException(ex, this);

            DestroyRunner();

            throw;
        }
        finally
        {
            _isStarting = false;
        }
    }

    private void DestroyRunner()
    {
        if (_runnerObject != null)
        {
            Destroy(_runnerObject);
        }

        _runner = null;
        _runnerObject = null;
    }
}