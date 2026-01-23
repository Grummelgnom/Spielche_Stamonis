using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class EnemyBulletController : NetworkBehaviour
{
    [SerializeField] private float bulletSpeed = 10f;   // Geschwindigkeit der gegnerischen Kugel
    [SerializeField] private float maxDistance = 20f;   // Maximale Distanz, bevor die Kugel verschwindet

    private Rigidbody2D rb;                             // Referenz auf das Rigidbody2D der Kugel
    private readonly SyncVar<Vector3> shootDirection =  // Synchronisierte Flugrichtung der Kugel
        new SyncVar<Vector3>(Vector3.down);

    private Vector3 spawnPosition;                      // Position, an der die Kugel gespawnt wurde
    private bool hasHit = false;                        // Verhindert mehrfaches Auslösen nach einem Treffer

    // Wird kurz nach dem Instantiieren aufgerufen, um Richtung und Startposition festzulegen
    public void InitializeBullet(Vector3 direction)
    {
        shootDirection.Value = direction.normalized;    // Richtung normalisieren, damit nur Richtung zählt
        spawnPosition = transform.position;             // Startposition der Kugel speichern
    }

    private void Start()
    {
        // Rigidbody holen und Anfangsgeschwindigkeit setzen
        rb = GetComponent<Rigidbody2D>();
        rb.linearVelocity = shootDirection.Value * bulletSpeed;
    }

    private void Update()
    {
        // Nur prüfen, wenn eine gültige Startposition gesetzt wurde
        if (spawnPosition != Vector3.zero)
        {
            // Distanz, die die Kugel seit dem Spawn zurückgelegt hat
            float distanceTraveled = Vector3.Distance(transform.position, spawnPosition);

            // Wenn die maximale Distanz überschritten ist, Kugel nur auf dem Server zerstören
            if (distanceTraveled > maxDistance)
            {
                if (IsServer)
                    Destroy(gameObject);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Kollisionen nur auf dem Server auswerten
        if (!IsServer) return;

        // Wenn bereits etwas getroffen wurde, nichts weiter tun
        if (hasHit) return;

        // Andere Kugeln komplett ignorieren (Spieler-Projektile und gegnerische Projektile)
        if (collision.GetComponent<BulletController>() != null ||
            collision.GetComponent<EnemyBulletController>() != null)
            return;

        // Nur Spieler treffen
        OwnPlayerController player = collision.GetComponent<OwnPlayerController>();
        if (player != null && !player.isDead.Value)
        {
            // Spieler Schaden zufügen und Kugel danach zerstören
            hasHit = true;
            player.TakeDamageServerRpc();
            Destroy(gameObject);
            return;
        }

        // Alle anderen Objekte (z.B. Wände) werden ignoriert, Kugel fliegt einfach weiter
    }
}
