using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class EnemyController : NetworkBehaviour
{
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private int maxHealth = 20;

    private readonly SyncVar<int> health = new SyncVar<int>();
    private GameObject targetPlayer;
    private Rigidbody2D rb;

    private void Start()
    {
        Debug.Log("EnemyController Start called");

        rb = GetComponent<Rigidbody2D>();
        Debug.Log($"Got Rigidbody2D: {rb}");

        if (rb == null)
        {
            Debug.LogError("ERROR: Enemy hat KEINEN Rigidbody2D! Prefab muss RB2D haben!");
            return;
        }

        if (!IsServerInitialized)
        {
            Debug.Log("Not server initialized, skipping health setup");
            return;
        }

        health.Value = maxHealth;
        Debug.Log($"Enemy spawned with {health.Value} HP");
    }

    private void FixedUpdate()
    {
        if (!IsServerInitialized) return;

        // SICHERHEIT: Hole rb immer neu falls null
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
            Debug.LogWarning("rb was null, fetching again!");
        }

        // Prüfe ob targetPlayer noch gültig ist
        if (targetPlayer == null || targetPlayer.GetComponent<OwnPlayerController>() == null)
        {
            targetPlayer = FindClosestPlayer();
        }

        // Bewege dich auf Spieler zu
        if (targetPlayer != null && rb != null)
        {
            MoveTowardsPlayer();
        }
    }

    private GameObject FindClosestPlayer()
    {
        OwnPlayerController[] players = FindObjectsByType<OwnPlayerController>(FindObjectsSortMode.None);

        if (players.Length == 0)
        {
            Debug.LogWarning("No players found!");
            return null;
        }

        Debug.Log($"Found {players.Length} players");

        GameObject closest = null;
        float closestDistance = float.MaxValue;

        foreach (var player in players)
        {
            float distance = Vector3.Distance(transform.position, player.gameObject.transform.position);
            if (distance < closestDistance)
            {
                closest = player.gameObject;
                closestDistance = distance;
            }
        }

        if (closest != null)
        {
            Debug.Log($"Found closest player: {closest.name}");
        }

        return closest;
    }

    private void MoveTowardsPlayer()
    {
        if (targetPlayer == null)
        {
            return;
        }

        if (targetPlayer.GetComponent<OwnPlayerController>() == null)
        {
            Debug.LogWarning($"TargetPlayer {targetPlayer.name} is not a valid player!");
            targetPlayer = null;
            return;
        }

        if (rb == null)
        {
            Debug.LogError("rb is null in MoveTowardsPlayer - CRITICAL!");
            return;
        }

        Vector3 direction = (targetPlayer.transform.position - transform.position).normalized;
        rb.linearVelocity = direction * moveSpeed;
    }

    [ServerRpc(RunLocally = true)]
    public void TakeDamageServerRpc(int damage)
    {
        health.Value -= damage;
        Debug.Log($"Enemy took {damage} damage! Health: {health.Value}");

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
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Bullet"))
        {
            Debug.Log("Enemy hit by bullet!");
            TakeDamageServerRpc(10);
        }
    }
}
