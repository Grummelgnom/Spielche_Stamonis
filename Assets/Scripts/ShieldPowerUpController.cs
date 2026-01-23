using FishNet.Object;
using UnityEngine;

public class ShieldPowerUpController : NetworkBehaviour
{
    [SerializeField] private float fallSpeed = 2f;        // Fallgeschwindigkeit des PowerUps
    [SerializeField] private float shieldDuration = 5f;   // Dauer des Shields nach Pickup

    private Rigidbody2D rb;

    private void Start()
    {
        // Rigidbody initialisieren und Fallgeschwindigkeit setzen
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.down * fallSpeed;
        }
    }

    private void Update()
    {
        // Nur Server entscheidet über Despawn
        if (!IsServerInitialized)
            return;

        // Wenn PowerUp aus dem Bildschirm ist → despawnen
        if (transform.position.y < -6f)
        {
            ServerManager.Despawn(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Kollisionen nur serverseitig auswerten
        if (!IsServerInitialized)
            return;

        // Spieler prüfen
        OwnPlayerController player = collision.GetComponent<OwnPlayerController>();
        if (player != null && !player.isDead.Value)
        {
            // Shield aktivieren und PowerUp despawnen
            player.ActivateShieldServerRpc(shieldDuration);
            ServerManager.Despawn(gameObject);
        }
    }
}
