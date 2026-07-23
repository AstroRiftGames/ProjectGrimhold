using Fusion;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public sealed class ExtractionZone : NetworkBehaviour
{
    private void Awake()
    {
        Collider2D col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!Runner || !Runner.IsServer)
            return;

        PlayerExtractionController extraction =
            other.GetComponentInParent<PlayerExtractionController>();

        if (extraction == null)
            return;

        extraction.SetInsideExtractionZone(true);
        
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!Runner || !Runner.IsServer)
            return;

        PlayerExtractionController extraction =
            other.GetComponentInParent<PlayerExtractionController>();

        if (extraction == null)
            return;

        extraction.SetInsideExtractionZone(false);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        Collider2D col = GetComponent<Collider2D>();

        if (col != null)
            col.isTrigger = true;
    }
#endif
}