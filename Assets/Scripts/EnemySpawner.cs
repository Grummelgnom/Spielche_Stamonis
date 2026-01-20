using FishNet.Managing;
using FishNet.Managing.Server;
using FishNet.Object;
using System;
using System.Collections;
using UnityEngine;

public class EnemySpawner : NetworkBehaviour
{
    public static EnemySpawner Instance { get; private set; }

    [Header("Enemy Prefabs")]
    [SerializeField] private GameObject enemy1Prefab;  // Roter Enemy (folgt Spieler)
    [SerializeField] private GameObject enemy2Prefab;  // Gelber Enemy (Zickzack)

    [Header("Spawn Settings")]
    [SerializeField] private int enemiesToSpawn = 3;
    [SerializeField] private float spawnDelay = 2f;
    [SerializeField] private Vector2 spawnAreaMin = new Vector2(-8, 5);
    [SerializeField] private Vector2 spawnAreaMax = new Vector2(8, 8);

    private int currentWave = 0;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        Debug.Log("EnemySpawner ready - waiting for game to start...");
    }

    public void StartWave()
    {
        if (!IsServer)
        {
            Debug.Log("Not server, returning");
            return;
        }

        currentWave++;
        Debug.Log($"Starting Wave {currentWave}!");
        StartCoroutine(SpawnEnemiesForWave(enemiesToSpawn));
    }

    private IEnumerator SpawnEnemiesForWave(int count)
    {
        for (int i = 0; i < count; i++)
        {
            SpawnEnemy();
            yield return new WaitForSeconds(spawnDelay);
        }

        Debug.Log($"Wave {currentWave} complete!");
    }

    private void SpawnEnemy()
    {
        // Zufällige Position im Spawn-Bereich (oben)
        float randomX = UnityEngine.Random.Range(spawnAreaMin.x, spawnAreaMax.x);
        float randomY = UnityEngine.Random.Range(spawnAreaMin.y, spawnAreaMax.y);
        Vector3 spawnPos = new Vector3(randomX, randomY, 0);

        // 50/50 Chance für Enemy1 oder Enemy2
        GameObject prefabToSpawn = UnityEngine.Random.value < 0.5f ? enemy1Prefab : enemy2Prefab;

        if (prefabToSpawn == null)
        {
            Debug.LogError("Enemy prefab is null!");
            return;
        }

        // Spawn Enemy
        GameObject enemyObj = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);

        Debug.Log($"Enemy spawned at {spawnPos}");

        // Als NetworkObject spawnen
        NetworkObject netObj = enemyObj.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            ServerManager.Spawn(enemyObj);
        }
    }
}
