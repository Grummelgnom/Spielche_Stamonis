using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class Enemy2Controller : NetworkBehaviour
{
    public readonly SyncVar<int> health = new SyncVar<int>(20);

    [Header("Movement Settings")]
    public float moveDownSpeed = 2f;
    public float zigzagSpeed = 3f;
    public float zigzagWidth = 2f;

    private Rigidbody2D rb;
    private float zigzagTime = 0f;
    private float lastPlayerDamageTime = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        if (!IsServerInitialized) return;

        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
            if (rb == null) return;
        }

        // Zickzack Bewegung
        zigzagTime += Time.fixedDeltaTime * zigzagSpeed;

        float horizontalMovement = Mathf.Sin(zigzagTime) * zigzagWidth;
        Vector2 velocity = new Vector2(horizontalMovement, -moveDownSpeed);

        rb.linearVelocity = velocity;
    }

    private void Update()
    {
        if (!IsServerInitialized) return;

        // Despawn wenn Enemy aus dem Bildschirm ist (zu weit unten)
        if (transform.position.y < -6f)
        {
            Debug.Log($"Enemy2 left screen at y={transform.position.y}, despawning");
            Die();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsServerInitialized) return;

        OwnPlayerController player = collision.GetComponent<OwnPlayerController>();
        if (player != null && !player.isDead.Value)
        {
            Debug.Log($"Enemy2 hit player {player.Owner.ClientId}! Enemy destroyed!");
            player.TakeDamageServerRpc();
            Die();  // Enemy stirbt beim Treffen
        }
    }


    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(int damage)
    {
        if (!IsServerInitialized) return;

        health.Value -= damage;

        if (health.Value <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (IsServerInitialized)
        {
            ServerManager.Despawn(gameObject);
        }
    }
}
