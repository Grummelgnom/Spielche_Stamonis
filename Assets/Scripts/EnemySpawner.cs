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
    // Singleton-Instanz für einfachen globalen Zugriff
    public static EnemySpawner Instance { get; private set; }

    [Header("Enemy Prefabs")]
    [SerializeField] private GameObject enemy1Prefab;  // Verfolger-Enemy (roter Enemy)
    [SerializeField] private GameObject enemy2Prefab;  // Zickzack-Enemy (gelber Enemy)

    [Header("Spawn Settings")]
    [SerializeField] private int enemiesToSpawn = 3;           // Basis-Anzahl pro Wave
    [SerializeField] private float spawnDelay = 2f;            // Verzögerung zwischen einzelnen Spawns
    [SerializeField] private Vector2 spawnAreaMin = new Vector2(-8, 5);  // Linke/untere Spawn-Grenze
    [SerializeField] private Vector2 spawnAreaMax = new Vector2(8, 8);   // Rechte/obere Spawn-Grenze

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI waveCountdownText;  // UI-Text für Countdown-Anzeige

    // Synchronisierter Countdown-Wert (alle Clients sehen denselben Wert)
    private readonly SyncVar<int> countdownSeconds = new SyncVar<int>(0);

    private int currentWave = 0;  // Aktuelle Wellennummer (nur Server)

    private void Awake()
    {
        // Singleton-Setup: nur eine Instanz erlauben
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        // Initialisierung abgeschlossen, wartet auf Netzwerk-Start
    }

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        // Callback für Countdown-Änderungen registrieren (UI-Update)
        countdownSeconds.OnChange += OnCountdownChanged;
    }

    public override void OnStopNetwork()
    {
        base.OnStopNetwork();
        // Callback deregistrieren beim Netzwerk-Stop
        countdownSeconds.OnChange -= OnCountdownChanged;
    }

    // Wird aufgerufen, wenn sich der Countdown ändert (auf allen Clients)
    private void OnCountdownChanged(int prev, int next, bool asServer)
    {
        UpdateCountdownText();
    }

    // Server startet die nächste Wave (wird extern aufgerufen, z.B. per Button)
    public void StartWave()
    {
        // Nur Server darf Waves steuern
        if (!IsServer)
            return;

        currentWave++;  // Wellennummer erhöhen

        // Schwierigkeit steigt: jede Wave 2 Enemies mehr
        int enemiesThisWave = 3 + (currentWave - 1) * 2;

        // Coroutine für Spawn-Sequenz starten
        StartCoroutine(SpawnEnemiesForWave(enemiesThisWave));
    }

    // Coroutine: Spawned Enemies mit Verzögerung, dann Countdown zur nächsten Wave
    private IEnumerator SpawnEnemiesForWave(int count)
    {
        // Alle Enemies dieser Wave einzeln spawnen
        for (int i = 0; i < count; i++)
        {
            SpawnEnemy();
            yield return new WaitForSeconds(spawnDelay);
        }

        // Wave beendet: Countdown zur nächsten starten
        for (int i = 3; i >= 1; i--)
        {
            // Countdown synchronisiert zu allen Clients
            countdownSeconds.Value = i;
            yield return new WaitForSeconds(1f);
        }

        // Countdown abgeschlossen: automatische nächste Wave
        countdownSeconds.Value = 0;
        StartWave();
    }

    // Spawnt einen einzelnen zufälligen Enemy im definierten Bereich
    private void SpawnEnemy()
    {
        // Zufällige Spawn-Position oben im Bildschirm
        float randomX = UnityEngine.Random.Range(spawnAreaMin.x, spawnAreaMax.x);
        float randomY = UnityEngine.Random.Range(spawnAreaMin.y, spawnAreaMax.y);
        Vector3 spawnPos = new Vector3(randomX, randomY, 0);

        // 50/50 Chance zwischen den beiden Enemy-Typen
        GameObject prefabToSpawn = UnityEngine.Random.value < 0.5f ? enemy1Prefab : enemy2Prefab;

        // Safety: Prefab prüfen
        if (prefabToSpawn == null)
            return;

        // Enemy instantiieren und als NetworkObject spawnen
        GameObject enemyObj = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
        NetworkObject netObj = enemyObj.GetComponent<NetworkObject>();

        if (netObj != null)
        {
            ServerManager.Spawn(enemyObj);
        }
    }

    // UI-Update für Countdown-Text (läuft auf allen Clients)
    private void UpdateCountdownText()
    {
        if (waveCountdownText == null)
            return;

        if (countdownSeconds.Value > 0)
            waveCountdownText.text = $"NEXT WAVE IN: {countdownSeconds.Value}";
        else
            waveCountdownText.text = "";
    }

    [Header("PowerUp Settings")]
    [SerializeField] private GameObject shieldPowerUpPrefab;        // Shield-PowerUp Prefab
    [SerializeField] private int powerUpScoreInterval = 50;         // Alle X Punkte einen PowerUp spawnen
    private int lastPowerUpScore = 0;                               // Letzter Score-Wert, bei dem gespawnt wurde

    // Sofortiger PowerUp-Spawn (Server-only, für Testing/Events)
    public void SpawnPowerUpNow()
    {
        if (!IsServerInitialized)
            return;

        if (shieldPowerUpPrefab != null)
        {
            SpawnPowerUp();
        }
    }

    // Spawnt einen Shield-PowerUp oben im Spawn-Bereich
    private void SpawnPowerUp()
    {
        // Zufällige X-Position, Y oben
        float randomX = UnityEngine.Random.Range(spawnAreaMin.x, spawnAreaMax.x);
        Vector3 spawnPos = new Vector3(randomX, spawnAreaMax.y, 0f);

        // PowerUp instantiieren und netzwerk-spawnen
        GameObject powerUp = Instantiate(shieldPowerUpPrefab, spawnPos, Quaternion.identity);
        ServerManager.Spawn(powerUp);
    }
}
