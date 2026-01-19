using FishNet.Object;
using UnityEngine;

public class BulletController : NetworkBehaviour
{
    [SerializeField] private float bulletSpeed = 20f;
    [SerializeField] private float maxDistance = 20f;  // ← EINSTELLBAR!

    private Rigidbody2D rb;
    private Vector3 shootDirection = Vector3.up;
    private Vector3 spawnPosition;
    private float distanceTraveled = 0f;

    public void InitializeBullet(Vector3 direction)
    {
        shootDirection = direction.normalized;
        spawnPosition = transform.position;
        Debug.Log($"Bullet initialized at {spawnPosition} with direction: {shootDirection}");
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        Debug.Log($"BulletController Start");
        Debug.Log($"Direction: {shootDirection}");
        Debug.Log($"Speed: {bulletSpeed}");
        Debug.Log($"Max Distance: {maxDistance}");

        // Setze Velocity - gerade nach oben!
        rb.linearVelocity = shootDirection * bulletSpeed;

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
        Debug.Log($"Bullet hit: {collision.gameObject.name}");
        if (IsServer)
        {
            Destroy(gameObject);
        }
    }
}
