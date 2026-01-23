using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class BulletController : NetworkBehaviour
{
    [SerializeField] private float bulletSpeed = 20f; // Bewegungsgeschwindigkeit der Kugel
    [SerializeField] private float maxDistance = 20f; // Maximale Flugdistanz, bevor die Kugel zerstört wird
    public int shooterClientId = -1;                      // Client-Id des Spielers, der die Kugel abgefeuert hat

    private Rigidbody2D rb;                               // Referenz auf das Rigidbody2D der Kugel
    private readonly SyncVar<Vector3> shootDirection =    // Synchronisierte Schussrichtung über das Netzwerk
        new SyncVar<Vector3>(Vector3.up);

    private Vector3 spawnPosition;                        // Position, an der die Kugel gespawnt wurde
    private float distanceTraveled = 0f;                  // Bisher zurückgelegte Distanz
    private bool hasHit = false;                          // Verhindert mehrfaches Auslösen nach dem ersten Treffer

    // Wird nach dem Instantiieren aufgerufen, um die Flugrichtung zu setzen
    public void InitializeBullet(Vector3 direction)
    {
        shootDirection.Value = direction.normalized;      // Richtung normalisieren, damit nur die Richtung zählt
        spawnPosition = transform.position;               // Startposition der Kugel speichern
    }

    // Initialisierung beim Start des Objekts
    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();                 // Rigidbody2D-Referenz holen
        rb.linearVelocity = shootDirection.Value * bulletSpeed;  // Kugel in Schussrichtung beschleunigen
    }

    // Wird einmal pro Frame aufgerufen, um die Distanz zu prüfen
    private void Update()
    {
        // Nur prüfen, wenn eine gültige Spawnposition gesetzt wurde
        if (spawnPosition != Vector3.zero)
        {
            // Distanz zwischen aktueller Position und Spawnposition berechnen
            distanceTraveled = Vector3.Distance(transform.position, spawnPosition);

            // Wenn die maximale Distanz überschritten ist, Kugel auf dem Server zerstören
            if (distanceTraveled > maxDistance)
            {
                if (IsServer)
                {
                    Destroy(gameObject);
                }
            }
        }
    }

    // Kollisionserkennung über Trigger (z.B. mit Gegnern oder Umgebung)
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Nur der Server verarbeitet Trefferlogik
        if (!IsServer) return;

        // Wenn bereits etwas getroffen wurde, keine weitere Verarbeitung
        if (hasHit) return;

        // Andere Kugeln ignorieren
        if (collision.GetComponent<BulletController>() != null) return;

        // Prüfe, ob ein Gegner vom Typ EnemyController getroffen wurde
        EnemyController enemy1 = collision.GetComponent<EnemyController>();
        if (enemy1 != null)
        {
            hasHit = true;                                // Treffer markieren
            enemy1.TakeDamageServerRpc(10, shooterClientId); // Schaden an Gegner 1 mit Shooter-Info
            Destroy(gameObject);                          // Kugel nach Treffer zerstören
            return;
        }

        // Prüfe, ob ein Gegner vom Typ Enemy2Controller getroffen wurde
        Enemy2Controller enemy2 = collision.GetComponent<Enemy2Controller>();
        if (enemy2 != null)
        {
            hasHit = true;                                // Treffer markieren
            enemy2.TakeDamageServerRpc(10, shooterClientId); // Schaden an Gegner 2 mit Shooter-Info
            Destroy(gameObject);                          // Kugel nach Treffer zerstören
            return;
        }

        // Prüfen, ob ein Spieler getroffen wurde (Friendly Fire wird hier verhindert)
        OwnPlayerController player = collision.GetComponent<OwnPlayerController>();
        if (player != null)
        {
            // Spieler wird nicht beschädigt, Kugel bleibt bestehen (falls gewünscht)
            return;
        }

        // Alle anderen Objekte zerstören die Kugel einfach (z.B. Wände, Umgebung)
        Destroy(gameObject);
    }
}
