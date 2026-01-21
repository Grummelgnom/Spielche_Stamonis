using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class EnemyBulletController : NetworkBehaviour
{
    [SerializeField] private float bulletSpeed = 10f;
    [SerializeField] private float maxDistance = 20f;

    private Rigidbody2D rb;
    private readonly SyncVar<Vector3> shootDirection = new SyncVar<Vector3>(Vector3.down);
    private Vector3 spawnPosition;
    private bool hasHit = false;

    public void InitializeBullet(Vector3 direction)
    {
        shootDirection.Value = direction.normalized;
        spawnPosition = transform.position;
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.linearVelocity = shootDirection.Value * bulletSpeed;
    }

    private void Update()
    {
        if (spawnPosition != Vector3.zero)
        {
            float distanceTraveled = Vector3.Distance(transform.position, spawnPosition);

            if (distanceTraveled > maxDistance)
            {
                if (IsServer)
                {
                    Destroy(gameObject);
                }
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsServer) return;
        if (hasHit) return;

        // Ignoriere andere Bullets (wichtig!)
        if (collision.GetComponent<BulletController>() != null ||
            collision.GetComponent<EnemyBulletController>() != null)
            return;

        // Nur Player treffen
        OwnPlayerController player = collision.GetComponent<OwnPlayerController>();
        if (player != null && !player.isDead.Value)
        {
            Debug.Log($"EnemyBullet hit player {player.Owner.ClientId}!");
            hasHit = true;
            player.TakeDamageServerRpc();
            Destroy(gameObject);
            return;
        }

        // Alles andere ignorieren (keine Wände etc)
    }
}
