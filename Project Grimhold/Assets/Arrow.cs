using Fusion;
using UnityEngine;

public class Arrow : NetworkBehaviour
{
    [SerializeField] private float speed = 15f;
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private int damage = 10;

    private Rigidbody2D rb;

    public override void Spawned()
    {
        rb = GetComponent<Rigidbody2D>();
        Debug.Log($"Arrow Spawned - {Object.Id}");
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority)
            return;

        // Movimiento
    }
}