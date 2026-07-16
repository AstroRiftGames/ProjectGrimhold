using UnityEngine;

#if UNITY_EDITOR
/// <summary>
/// Dibuja en el Editor el área de ataque y la dirección conceptual de MeleeAttack.
/// </summary>
public sealed class MeleeAttackGizmoDrawer : MonoBehaviour
{
    [SerializeField]
    private Transform _attackOrigin;

    [SerializeField]
    private MeleeAttackConfig _config;

    [SerializeField]
    private PlayerMovementNetworkController _movementController;

    private void OnDrawGizmosSelected()
    {
        if (_config == null)
        {
            return;
        }

        Vector2 origin = _attackOrigin != null ? (Vector2)_attackOrigin.position : (Vector2)transform.position;
        Vector2 direction = Vector2.down;

        if (_movementController != null && _movementController.Object != null && _movementController.Object.IsValid)
        {
            direction = _movementController.FacingDirection;
        }
        else if (Application.isPlaying)
        {
            var movement = GetComponentInParent<PlayerMovementNetworkController>();
            if (movement != null && movement.Object != null && movement.Object.IsValid)
            {
                direction = movement.FacingDirection;
            }
        }

        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = Vector2.down;
        }
        direction.Normalize();

        Vector2 attackCenter = origin + direction * _config.Range;

        // Dibujar punto de origen
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(origin, 0.05f);

        // Dibujar línea desde origen hasta el centro del área
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(origin, attackCenter);

        // Dibujar dirección normalizada
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(origin, origin + direction * 0.5f);

        // Dibujar círculo en el centro real del área
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackCenter, _config.Radius);

        // Dibujar línea desde origen hasta el extremo máximo del círculo
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
        Gizmos.DrawLine(origin, attackCenter + direction * _config.Radius);
    }
}
#endif