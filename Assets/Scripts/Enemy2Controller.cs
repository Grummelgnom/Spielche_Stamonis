using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class Enemy2Controller : NetworkBehaviour
{
    // Synchronisierte Lebenspunkte (Server ist Autorität, Clients bekommen den Wert repliziert)
    public readonly SyncVar<int> health = new SyncVar<int>(20);

    [Header("Movement Settings")]
    public float moveDownSpeed = 2f;     // Vertikale Geschwindigkeit nach unten
    public float zigzagSpeed = 3f;       // Geschwindigkeit der Zickzack-Animation (Sinus-Frequenz)
    public float zigzagWidth = 2f;       // Breite der horizontalen Auslenkung

    [Header("Shooting Settings")]
    [SerializeField] private GameObject enemyBulletPrefab; // Prefab für gegnerische Kugeln
    [SerializeField] private float shootInterval = 3f;     // Zeit zwischen Schüssen

    private float nextShootTime = 0f;    // Zeitpunkt, wann der nächste Schuss erlaubt ist
    private Rigidbody2D rb;             // Rigidbody2D für Bewegung über Physik
    private float zigzagTime = 0f;      // Zeitparameter für die Sinus-Bewegung

    private void Awake()
    {
        // Rigidbody2D einmalig cachen
        rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        // Bewegung/Schießen nur auf dem Server ausführen
        if (!IsServerInitialized)
            return;

        // Safety: Falls Rigidbody fehlt/noch nicht gesetzt ist, erneut holen
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
            if (rb == null)
                return;
        }

        // Sinus-Zeit fortschreiben (in FixedUpdate mit fixedDeltaTime)
        zigzagTime += Time.fixedDeltaTime * zigzagSpeed;

        // Horizontale Zickzack-Bewegung über Sinus
        float horizontalMovement = Mathf.Sin(zigzagTime) * zigzagWidth;

        // Velocity setzen: horizontal sinusförmig, vertikal konstant nach unten
        rb.linearVelocity = new Vector2(horizontalMovement, -moveDownSpeed);

        // Schießen in festen Intervallen (nur wenn Prefab gesetzt ist)
        if (Time.time >= nextShootTime && enemyBulletPrefab != null)
        {
            ShootDown();
            nextShootTime = Time.time + shootInterval;
        }
    }

    // Spawnt eine Kugel und initialisiert ihre Flugrichtung nach unten
    private void ShootDown()
    {
        // Kugel lokal instantiieren (Server) ...
        GameObject bullet = Instantiate(enemyBulletPrefab, transform.position, Quaternion.identity);

        // ... und über FishNet im Netzwerk spawnen
        ServerManager.Spawn(bullet);

        // Flugrichtung setzen (falls Controller vorhanden ist)
        bullet.GetComponent<EnemyBulletController>()?.InitializeBullet(Vector3.down);
    }

    private void Update()
    {
        // Server entscheidet, wann der Enemy \"weg\" ist
        if (!IsServerInitialized)
            return;

        // Falls der Gegner aus dem Bildschirm geflogen ist: despawnen (kein Killer)
        if (transform.position.y < -6f)
            Die(-1);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Kollisionen nur serverseitig auswerten
        if (!IsServerInitialized)
            return;

        // Prüfen, ob ein Spieler getroffen wurde
        OwnPlayerController player = collision.GetComponent<OwnPlayerController>();

        // Wenn Spieler lebt: Schaden zufügen und den Gegner entfernen (Kamikaze)
        if (player != null && !player.isDead.Value)
        {
            player.TakeDamageServerRpc();
            Die(-1);
        }
    }

    // Wird auf dem Server aufgerufen (z.B. von einer Bullet), um Schaden anzuwenden
    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(int damage, int shooterClientId)
    {
        // Sicherheit: nur ausführen, wenn Server läuft
        if (!IsServerInitialized)
            return;

        // HP reduzieren
        health.Value -= damage;

        // Wenn tot: Gegner entfernen und ggf. Punkte geben
        if (health.Value <= 0)
            Die(shooterClientId);
    }

    // Entfernt den Gegner und verteilt ggf. Score an den Killer
    private void Die(int killerClientId)
    {
        // Effekte/Sounds auf allen Clients abspielen
        PlayDeathEffectsObserversRpc();

        // Wenn ein gültiger Killer existiert: Score vergeben
        if (killerClientId >= 0)
        {
            // Alle Player suchen und den mit passender ClientId finden
            OwnPlayerController[] players =
                FindObjectsByType<OwnPlayerController>(FindObjectsSortMode.None);

            foreach (var player in players)
            {
                if (player.Owner.ClientId == killerClientId)
                {
                    player.AddScore(10);
                    break;
                }
            }
        }

        // Server despawnt das NetworkObject, damit es bei allen verschwindet
        if (IsServerInitialized)
            ServerManager.Despawn(gameObject);
    }

    // Visuelle/Auditive Effekte: laufen auf allen Beobachtern (Clients)
    [ObserversRpc]
    private void PlayDeathEffectsObserversRpc()
    {
        // Kamera kurz wackeln lassen (falls vorhanden)
        CameraShake.Instance?.Shake(0.2f, 0.15f);

        // Explosion-Sound abspielen (falls vorhanden)
        SimpleSoundManager.Instance?.PlayExplosionSound();
    }
}
