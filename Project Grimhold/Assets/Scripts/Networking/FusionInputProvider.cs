using Fusion;
using UnityEngine;

public sealed class FusionInputProvider : NetworkRunnerCallbacksAdapter
{
    [SerializeField]
    private PlayerInputReader _inputReader;

    private NetworkRunner _runner;

    private void Start()
    {
        _runner = FindAnyObjectByType<NetworkRunner>();

        _runner.AddCallbacks(this);
    }

    public override void OnInput(
        NetworkRunner runner,
        NetworkInput input)
    {
        if (_inputReader == null)
        {
            input.Set(default(PlayerNetworkInput));
            return;
        }

        PlayerNetworkInput networkInput =
            _inputReader.ConsumeNetworkInput();

        input.Set(networkInput);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_inputReader == null)
        {
            _inputReader =
                FindFirstObjectByType<PlayerInputReader>();
        }
    }
#endif
}
