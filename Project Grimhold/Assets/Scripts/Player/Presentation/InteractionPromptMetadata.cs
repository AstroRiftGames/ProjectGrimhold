using UnityEngine;

/// <summary>
/// Stores local-only contextual prompt text for an interactable network object.
/// It has no gameplay authority and is never synchronized or registered.
/// </summary>
[DisallowMultipleComponent]
public sealed class InteractionPromptMetadata : MonoBehaviour
{
    [SerializeField]
    private string _promptText = "Interactuar";

    public string PromptText => string.IsNullOrWhiteSpace(_promptText)
        ? "Interactuar"
        : _promptText;
}
