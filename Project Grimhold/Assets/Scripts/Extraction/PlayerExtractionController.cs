using Fusion;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerExtractionController :
    NetworkBehaviour,
    IExtractionParticipant
{
    [SerializeField]
    private ExtractionConfig _config;

    [SerializeField]
    private PlayerMovementNetworkController _movement;

    [Networked]
    public ExtractionState State { get; private set; }

    [Networked]
    private NetworkBool InsideExtractionZoneNetworked { get; set; }
    public bool IsInsideExtractionZone => InsideExtractionZoneNetworked;

    [Networked]
    private TickTimer ExtractionTimer { get; set; }

    public bool IsExtracting =>
        State == ExtractionState.Extracting;

    public bool IsExtracted =>
        State == ExtractionState.Extracted;

    private void Awake()
    {
        if (_movement == null)
            _movement = GetComponent<PlayerMovementNetworkController>();
    }

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            State = ExtractionState.None;
            InsideExtractionZoneNetworked = false;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority)
            return;

        if (State != ExtractionState.Extracting)
            return;

        if (!IsInsideExtractionZone)
        {
            CancelExtraction();
            return;
        }

        if (ExtractionTimer.Expired(Runner))
        {
            CompleteExtraction();
        }
    }

    public void SetInsideExtractionZone(bool value)
    {
        if (!HasStateAuthority)
            return;

        InsideExtractionZoneNetworked = value;
    }

    public bool BeginExtraction()
    {
        if (!HasStateAuthority)
            return false;

        if (_config == null)
            return false;

        if (State != ExtractionState.None)
            return false;

        if (!IsInsideExtractionZone)
            return false;

        State = ExtractionState.Extracting;

        ExtractionTimer = TickTimer.CreateFromSeconds(
            Runner,
            _config.ExtractionDuration);

        return true;
    }

    public void CancelExtraction()
    {
        if (!HasStateAuthority)
            return;

        State = ExtractionState.None;
        ExtractionTimer = TickTimer.None;
    }

    public bool CompleteExtraction()
    {
        if (!HasStateAuthority)
            return false;

        State = ExtractionState.Extracted;

        ExtractionTimer = TickTimer.None;

        if (_movement != null)
        {
            _movement.TrySetControlEnabled(false);
        }

        // TODO:
        // - Animate extraction
        // - Disable combat
        Debug.Log("Player has completed extraction!");

        return true;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_movement == null)
        {
            _movement = GetComponent<PlayerMovementNetworkController>();
        }

        if (_config == null)
        {
            Debug.LogWarning($"{nameof(PlayerExtractionController)}: No {nameof(ExtractionConfig)} assigned.", this);
        }
    }
#endif
}