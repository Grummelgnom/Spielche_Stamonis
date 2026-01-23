using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class OwnPlayerController : NetworkBehaviour
{
    // Synchronisierte Spielerfarbe
    private readonly SyncVar<Color> playerColor = new SyncVar<Color>();

    // Ready-Status (für GameManager)
    private readonly SyncVar<bool> isReady = new SyncVar<bool>();
    public bool IsReady => isReady.Value;

    [Header("Health & Score")]
    public readonly SyncVar<int> lives = new SyncVar<int>(3);      // Lebenspunkte
    public readonly SyncVar<bool> isDead = new SyncVar<bool>(false); // Tot-Status
    public readonly SyncVar<int> score = new SyncVar<int>(0);       // Punkte
    public readonly SyncVar<bool> hasShield = new SyncVar<bool>(false); // Shield aktiv
    private int lastPowerUpScore = 0;

    [Header("Ultimate Settings")]
    public readonly SyncVar<float> ultimateMeter = new SyncVar<float>(0f); // Ultimate-Leiste (0-100)
    [SerializeField] private float ultimateDrainRate = 20f;
    [SerializeField] private int powerUpScoreInterval = 50;
    private bool ultimateActive = false;
    private Coroutine blinkCoroutine = null;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float minY = -4f;
    [SerializeField] private float maxY = 4f;
    [SerializeField] private float minX = -4f;
    [SerializeField] private float maxX = 4f;

    [Header("Input System")]
    [SerializeField] private InputAction moveAction;
    [SerializeField] private InputAction colorChangeAction;
    [SerializeField] private InputAction shootSingleAction;
    [SerializeField] private InputAction shootSpreadAction;

    [Header("Shooting")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fireRate = 0.2f;
    private float nextFireTime = 0f;
    private bool wasSinglePressed = false;
    private bool wasSpreadPressed = false;

    [Header("Player UI & Visuals")]
    [SerializeField] private TextMeshPro playerIndexText;
    private Renderer playerRenderer;
    private GameObject shieldVisual;
    private PlayerInput _playerInput;

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        // UI-Callbacks registrieren
        lives.OnChange += OnLivesChanged;
        score.OnChange += OnScoreChanged;
        hasShield.OnChange += OnShieldChanged;
        ultimateMeter.OnChange += OnUltimateChanged;
        playerColor.OnChange += OnColorChanged;

        UpdateLivesUI();
        UpdateScoreUI();
        UpdateUltimateUI();
        CreateShieldVisual();
    }

    public override void OnStopNetwork()
    {
        base.OnStopNetwork();
        // Callbacks deregistrieren
        lives.OnChange -= OnLivesChanged;
        score.OnChange -= OnScoreChanged;
        hasShield.OnChange -= OnShieldChanged;
        ultimateMeter.OnChange -= OnUltimateChanged;
        playerColor.OnChange -= OnColorChanged;
    }

    private void OnDisable()
    {
        if (!IsOwner) return;
        moveAction?.Disable();
        colorChangeAction?.Disable();
        shootSingleAction?.Disable();
        shootSpreadAction?.Disable();
        if (TimeManager != null)
            TimeManager.OnTick -= OnTick;
    }

    private void Start()
    {
        _playerInput = GetComponent<PlayerInput>();
        StartCoroutine(DelayedIsOwner());
    }

    // Verzögerter Owner-Setup (nach Netzwerk-Initialisierung)
    private IEnumerator DelayedIsOwner()
    {
        playerRenderer = GetComponentInChildren<Renderer>();
        if (playerRenderer != null)
        {
            playerRenderer.material = new Material(playerRenderer.material);
            playerRenderer.material.color = playerColor.Value;
        }

        if (playerIndexText != null && _playerInput != null)
            playerIndexText.text = $"Player {_playerInput.playerIndex}";

        yield return null;

        if (IsOwner)
        {
            // Owner-spezifische Initialisierung
            ChangeColorServerRpc(Random.value, Random.value, Random.value);
            moveAction?.Enable();
            colorChangeAction?.Enable();
            shootSingleAction?.Enable();
            shootSpreadAction?.Enable();
            if (TimeManager != null)
                TimeManager.OnTick += OnTick;
            UpdateLivesUI();
            UpdateScoreUI();
            UpdateUltimateUI();
        }
    }

    // Tick-basierte Input-Verarbeitung (nur Owner)
    private void OnTick()
    {
        if (!IsOwner || !isReady.Value)
            return;

        HandleInput();
        HandleShooting();
    }

    // Ready-Status serverseitig setzen + Namen an GameManager weitergeben
    [ServerRpc]
    public void SetReadyStateServerRpc(string name)
    {
        isReady.Value = true;

        if (OwnNetworkGameManager.Instance != null)
        {
            // ClientId 0 = Player1 (Host), andere = Player2
            if (Owner.ClientId == 0)
                OwnNetworkGameManager.Instance.Player1.Value = name;
            else
                OwnNetworkGameManager.Instance.Player2.Value = name;

            OwnNetworkGameManager.Instance.CheckAndStartGame();
        }
    }

    [ServerRpc]
    public void CmdSetReady(bool ready)
    {
        isReady.Value = ready;
    }

    // ServerRpc: Bewegung synchronisieren
    [ServerRpc]
    private void MoveServerRpc(Vector2 input)
    {
        float dt = (float)TimeManager.TickDelta;
        float newX = Mathf.Clamp(transform.position.x + input.x * moveSpeed * dt, minX, maxX);
        float newY = Mathf.Clamp(transform.position.y + input.y * moveSpeed * dt, minY, maxY);
        transform.position = new Vector3(newX, newY, transform.position.z);
    }

    private void HandleInput()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();
        if (input.sqrMagnitude > 0f)
            MoveServerRpc(input);
    }

    private void HandleShooting()
    {
        bool isSinglePressed = shootSingleAction.IsPressed();
        bool isSpreadPressed = shootSpreadAction.IsPressed();

        // Ultimate-Modus (unbegrenztes Schießen)
        if (ultimateActive)
        {
            if (isSinglePressed)
                ShootServerRpc(1);
            DrainUltimateServerRpc(ultimateDrainRate * (float)TimeManager.TickDelta);
            if (ultimateMeter.Value <= 0f)
                ultimateActive = false;
            return;
        }

        // Ultimate aktivieren
        if (isSinglePressed && ultimateMeter.Value >= 100f && !ultimateActive)
        {
            ultimateActive = true;
            return;
        }

        // Normales Schießen (Single/Spread)
        if (isSinglePressed && !wasSinglePressed && Time.time >= nextFireTime)
        {
            ShootServerRpc(1);
            nextFireTime = Time.time + fireRate;
        }
        wasSinglePressed = isSinglePressed;

        if (isSpreadPressed && !wasSpreadPressed && Time.time >= nextFireTime)
        {
            ShootServerRpc(2);
            nextFireTime = Time.time + fireRate;
        }
        wasSpreadPressed = isSpreadPressed;
    }

    // ServerRpc: Schuss-Muster (1=Single, 2=Spread)
    [ServerRpc]
    private void ShootServerRpc(int pattern)
    {
        Vector3 spawnPos = firePoint ? firePoint.position : transform.position;
        Vector2 baseDirection = Vector2.up;

        if (pattern == 1)
            SpawnBullet(spawnPos, baseDirection);
        else if (pattern == 2)
        {
            // 3-Schuss-Spread
            SpawnBullet(spawnPos + new Vector3(-0.4f, 0, 0), Rotate(baseDirection, 30f));
            SpawnBullet(spawnPos, baseDirection);
            SpawnBullet(spawnPos + new Vector3(0.4f, 0, 0), Rotate(baseDirection, -30f));
        }

        PlaySoundObserversRpc(1);  // Lasersound
    }

    // Kugel spawnen und netzwerk-synchronisieren
    private void SpawnBullet(Vector3 position, Vector2 direction)
    {
        GameObject bulletObj = Instantiate(bulletPrefab, position, Quaternion.identity);
        var bc = bulletObj.GetComponent<BulletController>();
        if (bc != null)
        {
            bc.InitializeBullet(direction);
            bc.shooterClientId = Owner.ClientId;
        }
        ServerManager.Spawn(bulletObj, Owner);
    }

    // Richtung rotieren (für Spread-Shot)
    private Vector2 Rotate(Vector2 v, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(radians);
        float cos = Mathf.Cos(radians);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }

    // Schaden empfangen (Server-only)
    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc()
    {
        if (hasShield.Value)
            return;

        lives.Value--;
        if (lives.Value <= 0)
            Die();
    }

    // Spieler stirbt
    private void Die()
    {
        isDead.Value = true;
        PlaySoundObserversRpc(3);  // Tod-Sound
        if (IsServerInitialized)
            SubmitScoreTargetRpc(Owner, score.Value);
        DisablePlayerObserversRpc();
        OwnNetworkGameManager.Instance.OnPlayerDied(Owner.ClientId);
    }

    // Highscore an Client senden
    [TargetRpc]
    private void SubmitScoreTargetRpc(FishNet.Connection.NetworkConnection conn, int finalScore)
    {
        string playerName = Owner.ClientId == 0 ?
            OwnNetworkGameManager.Instance.Player1.Value :
            OwnNetworkGameManager.Instance.Player2.Value;

        if (string.IsNullOrEmpty(playerName))
            playerName = $"Player{Owner.ClientId}";

        HighscoreClient.Instance?.SubmitScore(playerName, finalScore);
        Invoke(nameof(ShowHighscoreList), 1f);
    }

    private void ShowHighscoreList()
    {
        FindFirstObjectByType<HighscoreDisplay>()?.ShowHighscores();
    }

    // Spieler deaktivieren (alle Clients)
    [ObserversRpc]
    private void DisablePlayerObserversRpc()
    {
        gameObject.SetActive(false);
    }

    // Shield aktivieren (ServerRpc)
    [ServerRpc(RequireOwnership = false)]
    public void ActivateShieldServerRpc(float duration)
    {
        if (isDead.Value)
            return;

        hasShield.Value = true;
        float flickerStartTime = duration - 2f;
        if (flickerStartTime > 0)
            StartFlickerObserversRpc(flickerStartTime);

        Invoke(nameof(DeactivateShield), duration);
        PlaySoundObserversRpc(2);  // PowerUp-Sound
    }

    private void DeactivateShield()
    {
        hasShield.Value = false;
    }

    // Sounds auf allen Clients
    [ObserversRpc]
    private void PlaySoundObserversRpc(int type)
    {
        if (SimpleSoundManager.Instance == null)
            return;

        switch (type)
        {
            case 1: SimpleSoundManager.Instance.PlayLaserSound(); break;
            case 2: SimpleSoundManager.Instance.PlayPowerUpSound(); break;
            case 3: SimpleSoundManager.Instance.PlayPlayerDeathSound(); break;
        }
    }

    // ───── UI-Callbacks ─────
    private void OnLivesChanged(int prev, int next, bool asServer)
    {
        UpdateLivesUI();
    }

    private void UpdateLivesUI()
    {
        if (!IsOwner) return;
        TMP_Text livesUI = GameObject.Find("LivesText")?.GetComponent<TMP_Text>();
        if (livesUI != null)
        {
            livesUI.text = lives.Value <= 0 ? "GAME OVER" : $"Lives: {lives.Value}";
            livesUI.color = lives.Value <= 0 ? Color.red : Color.white;
        }
    }

    public void AddScore(int points)
    {
        if (!IsServerInitialized) return;
        score.Value += points;

        // Ultimate füllen (außer im Ultimate-Modus)
        if (!ultimateActive)
            ultimateMeter.Value = Mathf.Clamp(ultimateMeter.Value + points, 0f, 100f);

        // PowerUp spawnen (alle X Punkte)
        if (score.Value >= lastPowerUpScore + powerUpScoreInterval)
        {
            lastPowerUpScore = score.Value;
            EnemySpawner.Instance?.SpawnPowerUpNow();
        }
    }

    private void OnScoreChanged(int prev, int next, bool asServer)
    {
        UpdateScoreUI();
    }

    private void UpdateScoreUI()
    {
        if (!IsOwner) return;
        TMP_Text scoreUI = GameObject.Find("ScoreText")?.GetComponent<TMP_Text>();
        if (scoreUI != null)
            scoreUI.text = $"Score: {score.Value}";
    }

    [ServerRpc]
    private void DrainUltimateServerRpc(float amount)
    {
        ultimateMeter.Value = Mathf.Max(0f, ultimateMeter.Value - amount);
    }

    private void OnUltimateChanged(float prev, float next, bool asServer)
    {
        UpdateUltimateUI();
        if (!IsOwner) return;

        SimpleSoundManager.Instance?.SetUltimateLoop(next >= 100f);

        // Blink-Effekt starten/beenden
        if (next >= 100f && blinkCoroutine == null)
            blinkCoroutine = StartCoroutine(BlinkUltimateSlider());
        else if (next < 100f && blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
            ResetSliderColor();
        }
    }

    private void UpdateUltimateUI()
    {
        if (!IsOwner) return;
        var slider = GameObject.Find("UltimateSlider")?.GetComponent<UnityEngine.UI.Slider>();
        if (slider != null)
            slider.value = ultimateMeter.Value;
    }

    private void ResetSliderColor()
    {
        var slider = GameObject.Find("UltimateSlider")?.GetComponent<UnityEngine.UI.Slider>();
        var img = slider?.fillRect.GetComponent<UnityEngine.UI.Image>();
        if (img != null)
            img.color = Color.yellow;
    }

    private IEnumerator BlinkUltimateSlider()
    {
        var slider = GameObject.Find("UltimateSlider")?.GetComponent<UnityEngine.UI.Slider>();
        var img = slider?.fillRect.GetComponent<UnityEngine.UI.Image>();
        while (img != null)
        {
            img.color = Color.yellow;
            yield return new WaitForSeconds(0.3f);
            img.color = Color.red;
            yield return new WaitForSeconds(0.3f);
        }
    }

    private void OnShieldChanged(bool prev, bool next, bool asServer)
    {
        shieldVisual?.SetActive(next);
    }

    // Visueller Shield-Effekt erstellen
    private void CreateShieldVisual()
    {
        shieldVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        shieldVisual.transform.SetParent(transform);
        shieldVisual.transform.localPosition = Vector3.zero;
        shieldVisual.transform.localScale = Vector3.one * 1.5f;
        Destroy(shieldVisual.GetComponent<Collider>());

        var r = shieldVisual.GetComponent<Renderer>();
        r.material = new Material(Shader.Find("Sprites/Default"));
        r.material.color = new Color(0f, 1f, 0f, 0.15f);
        shieldVisual.SetActive(false);
    }

    // Shield-Flimmern starten (ObserversRpc)
    [ObserversRpc]
    private void StartFlickerObserversRpc(float delay)
    {
        Invoke(nameof(StartFlickerLocal), delay);
    }

    private void StartFlickerLocal()
    {
        if (shieldVisual != null)
        {
            StartCoroutine(FlickerShield());
            SimpleSoundManager.Instance?.PlayShieldWarningSound();
        }
    }

    private IEnumerator FlickerShield()
    {
        while (hasShield.Value)
        {
            if (shieldVisual != null)
                shieldVisual.SetActive(!shieldVisual.activeSelf);
            yield return new WaitForSeconds(0.2f);
        }
        if (shieldVisual != null)
            shieldVisual.SetActive(false);
    }

    // Farbwechsel (Lobby)
    private void CheckForChangeColor()
    {
        if (colorChangeAction == null || !colorChangeAction.triggered)
            return;
        ChangeColorServerRpc(Random.value, Random.value, Random.value);
    }

    [ServerRpc]
    private void ChangeColorServerRpc(float r, float g, float b)
    {
        playerColor.Value = new Color(r, g, b);
    }

    private void OnColorChanged(Color prev, Color next, bool asServer)
    {
        if (playerRenderer != null)
            playerRenderer.material.color = next;
    }
}
