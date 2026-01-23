using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class Enemy2Controller : NetworkBehaviour
{
    public readonly SyncVar<int> health = new SyncVar<int>(20);
    [Header("Movement Settings")]
    public float moveDownSpeed = 2f;
    public float zigzagSpeed = 3f;
    public float zigzagWidth = 2f;
    [Header("Shooting Settings")]
    [SerializeField] private GameObject enemyBulletPrefab;
    [SerializeField] private float shootInterval = 3f;
    private float nextShootTime = 0f;
    private Rigidbody2D rb;
    private float zigzagTime = 0f;

    private void Awake() { rb = GetComponent<Rigidbody2D>(); }

    private void FixedUpdate()
    {
        if (!IsServerInitialized) return;
        if (rb == null) { rb = GetComponent<Rigidbody2D>(); if (rb == null) return; }
        zigzagTime += Time.fixedDeltaTime * zigzagSpeed;
        float horizontalMovement = Mathf.Sin(zigzagTime) * zigzagWidth;
        rb.linearVelocity = new Vector2(horizontalMovement, -moveDownSpeed);
        if (Time.time >= nextShootTime && enemyBulletPrefab != null) { ShootDown(); nextShootTime = Time.time + shootInterval; }
    }

    private void ShootDown()
    {
        GameObject bullet = Instantiate(enemyBulletPrefab, transform.position, Quaternion.identity);
        ServerManager.Spawn(bullet);
        bullet.GetComponent<EnemyBulletController>()?.InitializeBullet(Vector3.down);
    }

    private void Update()
    {
        if (!IsServerInitialized) return;
        if (transform.position.y < -6f) Die(-1);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsServerInitialized) return;
        OwnPlayerController player = collision.GetComponent<OwnPlayerController>();
        if (player != null && !player.isDead.Value) { player.TakeDamageServerRpc(); Die(-1); }
    }

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(int damage, int shooterClientId)
    {
        if (!IsServerInitialized) return;
        health.Value -= damage;
        if (health.Value <= 0) Die(shooterClientId);
    }

    private void Die(int killerClientId)
    {
        PlayDeathEffectsObserversRpc();
        if (killerClientId >= 0)
        {
            OwnPlayerController[] players = FindObjectsByType<OwnPlayerController>(FindObjectsSortMode.None);
            foreach (var player in players) { if (player.Owner.ClientId == killerClientId) { player.AddScore(10); break; } }
        }
        if (IsServerInitialized) ServerManager.Despawn(gameObject);
    }

    [ObserversRpc]
    private void PlayDeathEffectsObserversRpc()
    {
        CameraShake.Instance?.Shake(0.2f, 0.15f);
        SimpleSoundManager.Instance?.PlayExplosionSound();
    }
}
