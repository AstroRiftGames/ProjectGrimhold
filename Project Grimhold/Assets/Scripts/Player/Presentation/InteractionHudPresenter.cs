using TMPro;
using UnityEngine;

/// <summary>
/// Presents the local predictive interaction prompt and confirmed interaction results.
/// It never queries or mutates gameplay directly.
/// </summary>
[DisallowMultipleComponent]
public sealed class InteractionHudPresenter : MonoBehaviour
{
    [SerializeField]
    private GameObject _promptRoot;

    [SerializeField]
    private TMP_Text _promptText;

    [SerializeField]
    private GameObject _feedbackRoot;

    [SerializeField]
    private TMP_Text _feedbackText;

    [SerializeField, Min(0f)]
    private float _feedbackDuration = 1.25f;

    [SerializeField, Min(0f)]
    private float _attemptPulseDuration = 0.15f;

    private LocalInteractionCandidateSource _candidateSource;
    private PlayerInteractionNetworkController _interactionController;
    private int _lastConsumedSequence;
    private float _feedbackRemaining;
    private float _attemptPulseRemaining;
    private bool _isBound;

    public void Bind(
        LocalInteractionCandidateSource candidateSource,
        PlayerInteractionNetworkController interactionController)
    {
        Unbind();

        _candidateSource = candidateSource;
        _interactionController = interactionController;
        if (_interactionController == null)
        {
            HideAll();
            return;
        }

        _lastConsumedSequence = _interactionController.CurrentInteractionSequence;
        _interactionController.InteractionResolved += OnInteractionResolved;
        _isBound = true;
        RefreshPrompt();
    }

    public void Unbind()
    {
        if (_isBound && _interactionController != null)
        {
            _interactionController.InteractionResolved -= OnInteractionResolved;
        }

        _candidateSource = null;
        _interactionController = null;
        _isBound = false;
        HideAll();
    }

    private void Update()
    {
        if (!_isBound)
        {
            return;
        }

        RefreshPrompt();

        if (_attemptPulseRemaining > 0f)
        {
            _attemptPulseRemaining -= Time.deltaTime;
        }

        if (_feedbackRemaining <= 0f)
        {
            return;
        }

        _feedbackRemaining -= Time.deltaTime;
        if (_feedbackRemaining <= 0f && _feedbackRoot != null)
        {
            _feedbackRoot.SetActive(false);
        }
    }

    private void OnDisable()
    {
        Unbind();
    }

    private void OnInteractionResolved(InteractionPresentationEvent interactionEvent)
    {
        if (interactionEvent.Sequence == _lastConsumedSequence)
        {
            return;
        }

        _lastConsumedSequence = interactionEvent.Sequence;
        _attemptPulseRemaining = _attemptPulseDuration;

        if (interactionEvent.Success && interactionEvent.IsConsumed)
        {
            HideFeedback();
            return;
        }

        ShowFeedback(interactionEvent.Success
            ? "Interacción aceptada"
            : GetFailureMessage(interactionEvent.FailureReason));
    }

    private void RefreshPrompt()
    {
        bool hasCandidate = _candidateSource != null && _candidateSource.HasCandidate;
        bool showPrompt = hasCandidate || _attemptPulseRemaining > 0f;

        if (_promptRoot != null)
        {
            _promptRoot.SetActive(showPrompt);
        }

        if (_promptText != null && showPrompt)
        {
            _promptText.text = "E — Interactuar";
        }
    }

    private void ShowFeedback(string message)
    {
        if (_feedbackText != null)
        {
            _feedbackText.text = message;
        }

        if (_feedbackRoot != null)
        {
            _feedbackRoot.SetActive(true);
        }

        _feedbackRemaining = _feedbackDuration;
    }

    private void HideFeedback()
    {
        _feedbackRemaining = 0f;
        if (_feedbackRoot != null)
        {
            _feedbackRoot.SetActive(false);
        }
    }

    private void HideAll()
    {
        _feedbackRemaining = 0f;
        _attemptPulseRemaining = 0f;

        if (_promptRoot != null)
        {
            _promptRoot.SetActive(false);
        }

        HideFeedback();
    }

    private static string GetFailureMessage(InteractionFailureReason reason)
    {
        return reason switch
        {
            InteractionFailureReason.InteractionDisabled => "No podés interactuar ahora",
            InteractionFailureReason.InteractorUnavailable => "No podés interactuar",
            InteractionFailureReason.OutOfRange => "Fuera de alcance",
            InteractionFailureReason.TargetUnavailable => "Objetivo no disponible",
            InteractionFailureReason.ReceiverNotFound => "No se pudo recibir el loot",
            InteractionFailureReason.LootRejected => "Loot rechazado",
            _ => "Nada para interactuar"
        };
    }
}
