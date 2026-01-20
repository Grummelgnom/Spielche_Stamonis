using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class BulletController : NetworkBehaviour
{
    [SerializeField] private float bulletSpeed = 20f;
    [SerializeField] private float maxDistance = 20f;

    private Rigidbody2D rb;
    private readonly SyncVar<Vector3> shootDirection = new SyncVar<Vector3>(Vector3.up);
    private Vector3 spawnPosition;
    private float distanceTraveled = 0f;
    private bool hasHit = false;

    public void InitializeBullet(Vector3 direction)
    {
        shootDirection.Value = direction.normalized;
        spawnPosition = transform.position;
        Debug.Log($"Bullet initialized at {spawnPosition} with direction: {shootDirection.Value}");
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        Debug.Log($"BulletController Start");
        Debug.Log($"Direction: {shootDirection.Value}");
        Debug.Log($"Speed: {bulletSpeed}");
        Debug.Log($"Max Distance: {maxDistance}");

        // Setze Velocity mit synchronisierter Direction!
        rb.linearVelocity = shootDirection.Value * bulletSpeed;

        Debug.Log($"Velocity set to: {rb.linearVelocity}");
    }

    private void Update()
    {
        // Tracke wie weit der Bullet geflogen ist
        if (spawnPosition != Vector3.zero)
        {
            distanceTraveled = Vector3.Distance(transform.position, spawnPosition);

            // Wenn Bullet zu weit weg → Despawn
            if (distanceTraveled > maxDistance)
            {
                Debug.Log($"Bullet despawned! Distance: {distanceTraveled} > Max: {maxDistance}");
                if (IsServer)
                {
                    Destroy(gameObject);
                }
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Nur Server handled Kollisionen
        if (!IsServer) return;

        // Verhindere mehrfache Treffer
        if (hasHit) return;

        // Ignoriere andere Bullets
        if (collision.GetComponent<BulletController>() != null)
            return;

        Debug.Log($"Bullet OnTriggerEnter2D: {collision.gameObject.name}");

        // Prüfe ob es ein Enemy ist
        EnemyController enemy = collision.GetComponent<EnemyController>();
        if (enemy != null)
        {
            Debug.Log("Bullet hit an ENEMY!");
            hasHit = true;
            enemy.TakeDamageServerRpc(10);
            Destroy(gameObject);
            return;
        }

        // Prüfe ob es ein Player ist (freundlicher Feuer vermeiden)
        OwnPlayerController player = collision.GetComponent<OwnPlayerController>();
        if (player != null)
        {
            Debug.Log("Bullet hit a PLAYER (friendly fire prevented)");
            return;
        }

        // Sonst: Treffer an Wände etc
        Debug.Log($"Bullet hit something else: {collision.gameObject.name}");
        Destroy(gameObject);
    }
}
