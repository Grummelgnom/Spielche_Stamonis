using FishNet.Object;
using UnityEngine;

public class EnemyBulletController : NetworkBehaviour
{
    public float speed = 15f;
    public float maxDistance = 20f;
    public int damage = 10;

    private Vector3 direction;
    private Vector3 startPosition;
    private Rigidbody2D rb;

    public void InitializeBullet(Vector3 shootDirection)
    {
        direction = shootDirection.normalized;
        startPosition = transform.position;
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.linearVelocity = new Vector2(direction.x, direction.y) * speed;
        }
    }

    private void Update()
    {
        if (!IsServerInitialized) return;

        float distanceTraveled = Vector3.Distance(startPosition, transform.position);
        if (distanceTraveled >= maxDistance)
        {
            ServerManager.Despawn(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsServerInitialized) return;

        OwnPlayerController player = collision.GetComponent<OwnPlayerController>();
        if (player != null)
        {
            // Später: player.TakeDamage(damage);
            Debug.Log($"Enemy bullet hit Player {player.Owner.ClientId}!");
            ServerManager.Despawn(gameObject);
        }
    }
}
