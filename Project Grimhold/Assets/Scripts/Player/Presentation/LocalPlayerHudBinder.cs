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
    private RaidInventoryPresenter _inventoryPresenter;

    [SerializeField]
    private LocalInteractionCandidateSource _candidateSource;

    [SerializeField]
    private PlayerInteractionNetworkController _interactionController;

    [SerializeField]
    private PlayerLootReceiver _lootReceiver;

    [SerializeField]
    private PlayerLootTransferNetworkController _lootTransferController;

    private bool _isBound;
    private LocalInputContext _inputContext;

    public override void Spawned()
    {
        if (!HasInputAuthority)
        {
            SetHudActive(false);
            return;
        }

        BindLocalHud();
    }

    private void OnEnable()
    {
        if (Object != null && Object.IsValid && HasInputAuthority)
        {
            BindLocalHud();
        }
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
            _inventoryPresenter == null ||
            _candidateSource == null || _interactionController == null || _lootReceiver == null ||
            _lootTransferController == null)
        {
            Debug.LogError($"{nameof(LocalPlayerHudBinder)} has missing HUD dependencies.", this);
            SetHudActive(false);
            return;
        }

        SetHudActive(true);
        _interactionPresenter.Bind(_candidateSource, _interactionController);
        _lootPresenter.Bind(_lootReceiver);
        _isBound = true;

        _inputContext = Runner.GetComponent<LocalInputContext>();
        if (_inputContext == null)
        {
            Debug.LogError($"{nameof(LocalPlayerHudBinder)} could not resolve {nameof(LocalInputContext)}.", this);
            return;
        }

        _inputContext.ReaderChanged += OnInputReaderChanged;
        OnInputReaderChanged(_inputContext.Reader);
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

        if (_inputContext != null)
        {
            _inputContext.ReaderChanged -= OnInputReaderChanged;
            _inputContext = null;
        }

        if (_inventoryPresenter != null)
        {
            _inventoryPresenter.Unbind();
        }

        _isBound = false;
        SetHudActive(false);
    }

    private void OnInputReaderChanged(PlayerInputReader inputReader)
    {
        if (!_isBound || _inventoryPresenter == null)
        {
            return;
        }

        _inventoryPresenter.Unbind();
        if (inputReader != null)
        {
            _inventoryPresenter.Bind(
                _lootReceiver,
                inputReader,
                _interactionController,
                _lootTransferController,
                Runner,
                transform);
        }
    }

    private void SetHudActive(bool active)
    {
        if (_hudRoot != null && _hudRoot.activeSelf != active)
        {
            _hudRoot.SetActive(active);
        }
    }
}
