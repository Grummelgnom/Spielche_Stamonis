using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class EnemyController : NetworkBehaviour
{
    public readonly SyncVar<int> health = new SyncVar<int>(20);

    public float moveSpeed = 2f;
    private Transform targetPlayer;
    private Rigidbody2D rb;
    private float lastPlayerDamageTime = 0f;

    [Header("Shooting Settings")]
    [SerializeField] private GameObject enemyBulletPrefab;
    [SerializeField] private float shootInterval = 3f;
    private float nextShootTime = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        Debug.Log("EnemyController Start called");
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            Debug.Log($"Got Rigidbody2D: {rb}");
        }
        else
        {
            Debug.LogError("ERROR: Enemy hat KEINEN Rigidbody2D! Prefab muss RB2D haben!");
        }
    }

    private void FixedUpdate()
    {
        if (!IsServerInitialized) return;

        if (rb == null)
        {
            Debug.LogWarning("rb was null, fetching again!");
            rb = GetComponent<Rigidbody2D>();
            if (rb == null) return;
        }

        if (targetPlayer == null || !targetPlayer.gameObject.activeInHierarchy)
        {
            FindClosestPlayer();
        }

        if (targetPlayer != null)
        {
            Vector2 direction = (targetPlayer.position - transform.position).normalized;
            rb.linearVelocity = direction * moveSpeed;
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }

        // Schießen - 8 Bullets im Kreis
        if (Time.time >= nextShootTime && enemyBulletPrefab != null)
        {
            Shoot360();
            nextShootTime = Time.time + shootInterval;
        }
    }

    private void Shoot360()
    {
        // 8 Bullets in alle Richtungen (alle 45°)
        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f;
            Vector2 direction = AngleToDirection(angle);

            GameObject bullet = Instantiate(enemyBulletPrefab, transform.position, Quaternion.identity);
            ServerManager.Spawn(bullet);

            EnemyBulletController bulletCtrl = bullet.GetComponent<EnemyBulletController>();
            if (bulletCtrl != null)
            {
                bulletCtrl.InitializeBullet(direction);
            }
        }

        Debug.Log("Enemy1 shot 8 bullets in 360°!");
    }

    private Vector2 AngleToDirection(float angleDegrees)
    {
        float angleRadians = angleDegrees * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(angleRadians), Mathf.Sin(angleRadians));
    }

    private void FindClosestPlayer()
    {
        OwnPlayerController[] players = FindObjectsByType<OwnPlayerController>(FindObjectsSortMode.None);

        if (players.Length == 0)
        {
            return;
        }

        float closestDistance = Mathf.Infinity;
        Transform closest = null;

        foreach (OwnPlayerController player in players)
        {
            if (player == null || !player.gameObject.activeInHierarchy || player.isDead.Value)
                continue;

            float distance = Vector2.Distance(transform.position, player.transform.position);
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
        if (!IsServerInitialized) return;

        OwnPlayerController player = collision.GetComponent<OwnPlayerController>();
        if (player != null && !player.isDead.Value)
        {
            Debug.Log($"Enemy1 hit player {player.Owner.ClientId}! Enemy destroyed!");
            player.TakeDamageServerRpc();
            Die(-1);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(int damage, int shooterClientId)
    {
        if (!IsServerInitialized) return;

        health.Value -= damage;
        Debug.Log($"Enemy took {damage} damage from player {shooterClientId}. Health: {health.Value}");

        if (health.Value <= 0)
        {
            Die(shooterClientId);
        }
    }

    private void Die(int killerClientId)
    {
        Debug.Log($"Enemy1 died! Killer: {killerClientId}");

        if (killerClientId >= 0)
        {
            OwnPlayerController[] players = FindObjectsByType<OwnPlayerController>(FindObjectsSortMode.None);
            foreach (var player in players)
            {
                if (player.Owner.ClientId == killerClientId)
                {
                    player.AddScore(10);
                    Debug.Log($"Gave 10 points to player {killerClientId}");
                    break;
                }
            }
        }

        if (IsServerInitialized)
        {
            ServerManager.Despawn(gameObject);
        }
    }
}
