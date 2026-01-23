using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class EnemyController : NetworkBehaviour
{
    // Synchronisierte Lebenspunkte (Server ist Autorität, Clients sehen die Änderungen)
    public readonly SyncVar<int> health = new SyncVar<int>(20);

    public float moveSpeed = 2f;                        // Geschwindigkeit, mit der der Enemy verfolgt

    private Transform targetPlayer;                     // Aktuelles Verfolgungs-Ziel (nächster lebender Spieler)
    private Rigidbody2D rb;                             // Rigidbody2D für physikalische Bewegung

    [Header("Shooting Settings")]
    [SerializeField] private GameObject enemyBulletPrefab; // Prefab für die gegnerischen Kugeln
    [SerializeField] private float shootInterval = 3f;     // Zeit zwischen den 360°-Salven

    private float nextShootTime = 0f;                   // Zeitpunkt für den nächsten Schuss

    private void Awake()
    {
        // Rigidbody2D cachen für bessere Performance
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        // Zusätzliche Sicherheit: Rigidbody erneut holen (falls Awake übersprungen wurde)
        rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        // Bewegung und Schießen nur serverseitig
        if (!IsServerInitialized)
            return;

        // Safety: Falls Rigidbody fehlt, erneut holen
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
            if (rb == null)
                return;
        }

        // Ziel neu suchen, wenn keins vorhanden oder inaktiv
        if (targetPlayer == null || !targetPlayer.gameObject.activeInHierarchy)
            FindClosestPlayer();

        // Richtung zum nächsten lebenden Spieler berechnen und verfolgen
        if (targetPlayer != null)
        {
            Vector2 direction = (targetPlayer.position - transform.position).normalized;
            rb.linearVelocity = direction * moveSpeed;
        }
        else
        {
            // Kein Ziel gefunden: stehen bleiben
            rb.linearVelocity = Vector2.zero;
        }

        // 360°-Salve in festen Intervallen abfeuern
        if (Time.time >= nextShootTime && enemyBulletPrefab != null)
        {
            Shoot360();
            nextShootTime = Time.time + shootInterval;
        }
    }

    // Feuert 8 Kugeln in 45°-Schritten (360°-Salve)
    private void Shoot360()
    {
        for (int i = 0; i < 8; i++)
        {
            // Winkel für jeden Schuss berechnen (0°, 45°, 90°, ..., 315°)
            float angle = i * 45f;
            float radians = angle * Mathf.Deg2Rad;

            // Richtung als Unit-Vector berechnen
            Vector2 direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));

            // Kugel spawnen
            GameObject bullet = Instantiate(enemyBulletPrefab, transform.position, Quaternion.identity);
            ServerManager.Spawn(bullet);
            bullet.GetComponent<EnemyBulletController>()?.InitializeBullet(direction);
        }
    }

    // Sucht den am nächsten lebenden, aktiven Spieler
    private void FindClosestPlayer()
    {
        OwnPlayerController[] players =
            FindObjectsByType<OwnPlayerController>(FindObjectsSortMode.None);

        float closestDistance = Mathf.Infinity;
        Transform closest = null;

        // Alle Spieler durchsuchen
        foreach (OwnPlayerController player in players)
        {
            // Überspringen, wenn Spieler ungültig oder tot/inaktiv
            if (player == null || !player.gameObject.activeInHierarchy || player.isDead.Value)
                continue;

            // Distanz berechnen
            float distance = Vector2.Distance(transform.position, player.transform.position);

            // Nächsten Spieler merken
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = player.transform;
            }
        }

        targetPlayer = closest;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Kollisionen nur serverseitig auswerten
        if (!IsServerInitialized)
            return;

        // Prüfen, ob ein lebender Spieler getroffen wurde (Kamikaze)
        OwnPlayerController player = collision.GetComponent<OwnPlayerController>();
        if (player != null && !player.isDead.Value)
        {
            player.TakeDamageServerRpc();
            Die(-1);  // Kein Killer (Selbstmord)
        }
    }

    // ServerRpc: Schaden empfangen (von Spieler-Kugeln)
    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(int damage, int shooterClientId)
    {
        // Sicherheit: nur auf Server
        if (!IsServerInitialized)
            return;

        // Lebenspunkte reduzieren
        health.Value -= damage;

        // Tot: Tod abarbeiten
        if (health.Value <= 0)
            Die(shooterClientId);
    }

    // Tod: Effekte, Score-Verteilung, Despawn
    private void Die(int killerClientId)
    {
        // Tod-Effekte auf allen Clients
        PlayDeathEffectsObserversRpc();

        // Score an Killer vergeben (falls vorhanden)
        if (killerClientId >= 0)
        {
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

        // Server despawnt das NetworkObject
        if (IsServerInitialized)
            ServerManager.Despawn(gameObject);
    }

    // Tod-Effekte: Kamera-Shake und Sound (auf allen Clients)
    [ObserversRpc]
    private void PlayDeathEffectsObserversRpc()
    {
        // Kamerawackeln (falls vorhanden)
        CameraShake.Instance?.Shake(0.2f, 0.15f);

        // Explosion-Sound (falls vorhanden)
        SimpleSoundManager.Instance?.PlayExplosionSound();
    }
}
