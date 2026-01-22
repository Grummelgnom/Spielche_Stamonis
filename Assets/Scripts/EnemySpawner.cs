using FishNet.Managing;
using FishNet.Managing.Server;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System;
using System.Collections;
using UnityEngine;
using TMPro;

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

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI waveCountdownText;

    private readonly SyncVar<int> countdownSeconds = new SyncVar<int>(0);
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

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        countdownSeconds.OnChange += OnCountdownChanged;
    }

    public override void OnStopNetwork()
    {
        base.OnStopNetwork();
        countdownSeconds.OnChange -= OnCountdownChanged;
    }

    private void OnCountdownChanged(int prev, int next, bool asServer)
    {
        UpdateCountdownText();
    }

    public void StartWave()
    {
        if (!IsServer)
        {
            Debug.Log("Not server, returning");
            return;
        }

        currentWave++;

        // Jede Wave spawnt 2 mehr Enemies!
        int enemiesThisWave = 3 + (currentWave - 1) * 2;

        Debug.Log($"Starting Wave {currentWave}! Enemies: {enemiesThisWave}");
        StartCoroutine(SpawnEnemiesForWave(enemiesThisWave));
    }

    private IEnumerator SpawnEnemiesForWave(int count)
    {
        for (int i = 0; i < count; i++)
        {
            SpawnEnemy();
            yield return new WaitForSeconds(spawnDelay);
        }

        Debug.Log($"Wave {currentWave} complete!");

        // Countdown für nächste Wave
        for (int i = 3; i >= 1; i--)
        {
            countdownSeconds.Value = i;
            yield return new WaitForSeconds(1f);
        }

        countdownSeconds.Value = 0;
        StartWave();
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

    private void UpdateCountdownText()
    {
        if (waveCountdownText == null) return;

        if (countdownSeconds.Value > 0)
            waveCountdownText.text = $"NEXT WAVE IN : {countdownSeconds.Value}";
        else
            waveCountdownText.text = "";
    }
    [Header("PowerUp Settings")]
    [SerializeField] private GameObject shieldPowerUpPrefab;
    [SerializeField] private int powerUpScoreInterval = 50;  // Alle 50 Punkte
    private int lastPowerUpScore = 0;

    // Entferne die Update() Methode wenn du sie für PowerUp hinzugefügt hattest!

    public void SpawnPowerUpNow()
    {
        if (!IsServerInitialized) return;

        if (shieldPowerUpPrefab != null)
        {
            SpawnPowerUp();
        }
    }


    private void SpawnPowerUp()
    {
        float randomX = UnityEngine.Random.Range(spawnAreaMin.x, spawnAreaMax.x);
        Vector3 spawnPos = new Vector3(randomX, spawnAreaMax.y, 0f);

        GameObject powerUp = Instantiate(shieldPowerUpPrefab, spawnPos, Quaternion.identity);
        ServerManager.Spawn(powerUp);

        Debug.Log($"Shield PowerUp spawned at {spawnPos}!");
    }


}
