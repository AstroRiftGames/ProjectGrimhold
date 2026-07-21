using System;
using UnityEngine;

/// <summary>
/// Stores the single local input reader associated with one runner lifetime.
/// It is a local-only bridge and contains no replicated state or gameplay services.
/// </summary>
[DisallowMultipleComponent]
public sealed class LocalInputContext : MonoBehaviour
{
    public PlayerInputReader Reader { get; private set; }

    public event Action<PlayerInputReader> ReaderChanged;

    /// <summary>
    /// Registers the active local reader. Re-registering the same instance is idempotent.
    /// </summary>
    public bool TryRegister(PlayerInputReader reader)
    {
        if (reader == null)
        {
            return false;
        }

        if (Reader == reader)
        {
            return true;
        }

        if (Reader != null)
        {
            Debug.LogError($"{nameof(LocalInputContext)} already has a different active reader.", this);
            return false;
        }

        Reader = reader;
        ReaderChanged?.Invoke(Reader);
        return true;
    }

    /// <summary>
    /// Removes the reader only when the caller owns the current registration.
    /// </summary>
    public void TryUnregister(PlayerInputReader reader)
    {
        if (Reader != reader)
        {
            return;
        }

        Clear();
    }

    /// <summary>
    /// Clears the runner-scoped local reference during shutdown.
    /// </summary>
    public void Clear()
    {
        if (Reader == null)
        {
            return;
        }

        Reader = null;
        ReaderChanged?.Invoke(null);
    }

    private void OnDestroy()
    {
        Clear();
    }
}
