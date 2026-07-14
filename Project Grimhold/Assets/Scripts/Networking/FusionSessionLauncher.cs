using Fusion;
using System.Threading.Tasks;
using UnityEngine;

public sealed class FusionSessionLauncher : MonoBehaviour
{
    [Header("Session")]
    [SerializeField]
    private string _sessionName;

    [SerializeField, Min(1)]
    private int _maxPlayers = 4;

    [Header("Dependencies")]
    [SerializeField]
    private FusionInputProvider _inputProvider;

    [SerializeField]
    private NetworkPlayerSpawner _playerSpawner;

    private NetworkRunner _runner;
    private GameObject _runnerObject;
    private bool _isStarting;

    public NetworkRunner Runner => _runner;

    public async Task StartSessionAsync(string sessionName, GameMode mode)
    {
        if (_isStarting || _runner != null)
        {
            return;
        }

        if (_inputProvider == null)
        {
            Debug.LogError(
                $"{nameof(FusionSessionLauncher)} requires " +
                $"{nameof(FusionInputProvider)}.",
                this);

            return;
        }

        if (_playerSpawner == null)
        {
            Debug.LogError(
                $"{nameof(FusionSessionLauncher)} requires " +
                $"{nameof(NetworkPlayerSpawner)}.",
                this);

            return;
        }

        _isStarting = true;

        _runnerObject = new GameObject("NetworkRunner");
        _runnerObject.transform.SetParent(transform);

        _runner = _runnerObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;
        _runner.AddCallbacks(_inputProvider);
        _runner.AddCallbacks(_playerSpawner);

        StartGameResult result = await _runner.StartGame(
        new StartGameArgs
        {
            GameMode = mode,
            SessionName = sessionName,
            PlayerCount = _maxPlayers
        });

        _isStarting = false;

        if (!result.Ok)
        {
            Debug.LogError(
                $"Fusion failed to start. " +
                $"Reason: {result.ShutdownReason}",
                this);

            DestroyRunner();
            return;
        }

        string peerType = _runner.IsServer
            ? "Host"
            : "Client";

        Debug.Log(
            $"Fusion session started. " +
            $"Session: {_runner.SessionInfo.Name}. " +
            $"Peer: {peerType}.",
            this);
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

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_inputProvider == null)
        {
            _inputProvider =
                GetComponent<FusionInputProvider>();
        }

        if (_playerSpawner == null)
        {
            _playerSpawner =
                GetComponent<NetworkPlayerSpawner>();
        }
    }
#endif
}