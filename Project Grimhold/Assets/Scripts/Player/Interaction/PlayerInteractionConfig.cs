using UnityEngine;

/// <summary>
/// Data-driven configuration storing immutable parameters for player interaction.
/// </summary>
[CreateAssetMenu(fileName = "PlayerInteractionConfig", menuName = "Grimhold/Config/PlayerInteractionConfig")]
public sealed class PlayerInteractionConfig : ScriptableObject
{
    [SerializeField, Min(0.001f)]
    private float _maximumDistance = 2f;

    [SerializeField]
    private LayerMask _targetLayerMask;

    public float MaximumDistance => _maximumDistance;
    public LayerMask TargetLayerMask => _targetLayerMask;

    private void OnValidate()
    {
        _maximumDistance = Mathf.Max(0.001f, _maximumDistance);

        if (_targetLayerMask.value == 0)
        {
            Debug.LogWarning($"{nameof(PlayerInteractionConfig)}: TargetLayerMask is currently empty.", this);
        }
    }
}
