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
    }

    private void FindClosestPlayer()
    {
        OwnPlayerController[] players = FindObjectsByType<OwnPlayerController>(FindObjectsSortMode.None);

        if (players.Length == 0)
        {
            Debug.LogWarning("No players found!");
            return;
        }

        Debug.Log($"Found {players.Length} players");

        float closestDistance = Mathf.Infinity;
        Transform closest = null;

        foreach (OwnPlayerController player in players)
        {
            if (player == null || !player.gameObject.activeInHierarchy) continue;

            float distance = Vector2.Distance(transform.position, player.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = player.transform;
            }
        }

        targetPlayer = closest;
        if (targetPlayer != null)
        {
            Debug.Log($"Found closest player: {targetPlayer.gameObject.name}");
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsServerInitialized) return;

        OwnPlayerController player = collision.GetComponent<OwnPlayerController>();
        if (player != null && Time.time > lastPlayerDamageTime + 1f)
        {
            Debug.Log($"Enemy1 hit player {player.Owner.ClientId}!");
            player.TakeDamageServerRpc();
            lastPlayerDamageTime = Time.time;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(int damage)
    {
        if (!IsServerInitialized) return;

        health.Value -= damage;
        Debug.Log($"Enemy took {damage} damage. Health: {health.Value}");

        if (health.Value <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("Enemy died!");
        if (IsServerInitialized)
        {
            ServerManager.Despawn(gameObject);
        }
    }
}
