using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class EnemyController : NetworkBehaviour
{
    public readonly SyncVar<int> health = new SyncVar<int>(20);
    public float moveSpeed = 2f;
    private Transform targetPlayer;
    private Rigidbody2D rb;

    [Header("Shooting Settings")]
    [SerializeField] private GameObject enemyBulletPrefab;
    [SerializeField] private float shootInterval = 3f;
    private float nextShootTime = 0f;

    private void Awake() { rb = GetComponent<Rigidbody2D>(); }

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null) Debug.LogError("ERROR: Enemy hat KEINEN Rigidbody2D!");
    }

    private void FixedUpdate()
    {
        if (!IsServerInitialized) return;
        if (rb == null) { rb = GetComponent<Rigidbody2D>(); if (rb == null) return; }
        if (targetPlayer == null || !targetPlayer.gameObject.activeInHierarchy) FindClosestPlayer();
        if (targetPlayer != null) { Vector2 direction = (targetPlayer.position - transform.position).normalized; rb.linearVelocity = direction * moveSpeed; }
        else rb.linearVelocity = Vector2.zero;

        if (Time.time >= nextShootTime && enemyBulletPrefab != null) { Shoot360(); nextShootTime = Time.time + shootInterval; }
    }

    private void Shoot360()
    {
        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f;
            float radians = angle * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
            GameObject bullet = Instantiate(enemyBulletPrefab, transform.position, Quaternion.identity);
            ServerManager.Spawn(bullet);
            bullet.GetComponent<EnemyBulletController>()?.InitializeBullet(direction);
        }
    }

    private void FindClosestPlayer()
    {
        OwnPlayerController[] players = FindObjectsByType<OwnPlayerController>(FindObjectsSortMode.None);
        float closestDistance = Mathf.Infinity;
        Transform closest = null;
        foreach (OwnPlayerController player in players)
        {
            if (player == null || !player.gameObject.activeInHierarchy || player.isDead.Value) continue;
            float distance = Vector2.Distance(transform.position, player.transform.position);
            if (distance < closestDistance) { closestDistance = distance; closest = player.transform; }
        }
        targetPlayer = closest;
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
