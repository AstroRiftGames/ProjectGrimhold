using Fusion;
using UnityEngine;

/// <summary>
/// Binds the provisional gameplay HUD exclusively to this peer's Input Authority player.
/// All dependencies are serialized within the network player prefab.
/// </summary>
[DisallowMultipleComponent]
public sealed class LocalPlayerHudBinder : NetworkBehaviour
{
    [SerializeField]
    private GameObject _hudRoot;

    [SerializeField]
    private InteractionHudPresenter _interactionPresenter;

    [SerializeField]
    private LootHudPresenter _lootPresenter;

    [SerializeField]
    private LocalInteractionCandidateSource _candidateSource;

    [SerializeField]
    private PlayerInteractionNetworkController _interactionController;

    [SerializeField]
    private PlayerLootReceiver _lootReceiver;

    private bool _isBound;

    public override void Spawned()
    {
        if (!HasInputAuthority)
        {
            SetHudActive(false);
            return;
        }

        BindLocalHud();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        UnbindLocalHud();
    }

    private void OnDisable()
    {
        UnbindLocalHud();
    }

    private void OnDestroy()
    {
        UnbindLocalHud();
    }

    private void BindLocalHud()
    {
        if (_isBound)
        {
            return;
        }

        if (_hudRoot == null || _interactionPresenter == null || _lootPresenter == null ||
            _candidateSource == null || _interactionController == null || _lootReceiver == null)
        {
            Debug.LogError($"{nameof(LocalPlayerHudBinder)} has missing HUD dependencies.", this);
            SetHudActive(false);
            return;
        }

        SetHudActive(true);
        _interactionPresenter.Bind(_candidateSource, _interactionController);
        _lootPresenter.Bind(_lootReceiver);
        _isBound = true;
    }

    private void UnbindLocalHud()
    {
        if (_interactionPresenter != null)
        {
            _interactionPresenter.Unbind();
        }

        if (_lootPresenter != null)
        {
            _lootPresenter.Unbind();
        }

        _isBound = false;
        SetHudActive(false);
    }

    private void SetHudActive(bool active)
    {
        if (_hudRoot != null && _hudRoot.activeSelf != active)
        {
            _hudRoot.SetActive(active);
        }
    }
}
