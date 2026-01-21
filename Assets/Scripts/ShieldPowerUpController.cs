using FishNet.Object;
using UnityEngine;

public class ShieldPowerUpController : NetworkBehaviour
{
    [SerializeField] private float fallSpeed = 2f;
    [SerializeField] private float shieldDuration = 5f;

    private Rigidbody2D rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.down * fallSpeed;
        }
    }

    private void Update()
    {
        if (!IsServerInitialized) return;

        // Despawn wenn zu weit unten
        if (transform.position.y < -6f)
        {
            ServerManager.Despawn(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsServerInitialized) return;

        OwnPlayerController player = collision.GetComponent<OwnPlayerController>();
        if (player != null && !player.isDead.Value)
        {
            Debug.Log($"PowerUp: Player {player.Owner.ClientId} picked up Shield! Calling ActivateShieldServerRpc...");
            player.ActivateShieldServerRpc(shieldDuration);
            Debug.Log($"PowerUp: ActivateShieldServerRpc called! Despawning PowerUp...");
            ServerManager.Despawn(gameObject);
        }
    }

}
