using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class OwnPlayerController : NetworkBehaviour
{
    private readonly SyncVar<Color> playerColor = new SyncVar<Color>();
    private readonly SyncVar<bool> isReady = new SyncVar<bool>();
    public bool IsReady => isReady.Value;

    [Header("Health")]
    public readonly SyncVar<int> lives = new SyncVar<int>(3);
    public readonly SyncVar<bool> isDead = new SyncVar<bool>(false);
    public readonly SyncVar<int> score = new SyncVar<int>(0);
    [Header("Ultimate")]
    public readonly SyncVar<float> ultimateMeter = new SyncVar<float>(0f);
    [SerializeField] private float ultimateChargePerKill = 10f;  // Pro Kill +10%
    [SerializeField] private float ultimateDrainRate = 20f;      // 20% pro Sekunde
    [SerializeField] private float rapidFireRate = 0.05f;        // Sehr schnell!
    private bool ultimateActive = false;

    public readonly SyncVar<bool> hasShield = new SyncVar<bool>(false);

    private Renderer playerRenderer;
    private GameObject shieldVisual;
    private int lastPowerUpScore = 0;
    
    [Header("PowerUp Settings")]
    [SerializeField] private int powerUpScoreInterval = 50;  // Im Inspector einstellbar!

    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float minY = -4f;
    [SerializeField] private float maxY = 4f;
    [SerializeField] private float minX = -4f;
    [SerializeField] private float maxX = 4f;

    [Header("Input System")]
    [SerializeField] private InputAction moveAction;
    [SerializeField] private InputAction colorChangeAction;



    [Header("Shooting")]
    [SerializeField] private InputAction shootSingleAction;
    [SerializeField] private InputAction shootSpreadAction;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fireRate = 0.2f;  // ← DIESE ZEILE HINZUFÜGEN!
    private float nextFireTime = 0f;                  // ← DIESE ZEILE HINZUFÜGEN!
    private bool wasSinglePressed = false;
    private bool wasSpreadPressed = false;


    [Header("Player UI")]
    [SerializeField] private TextMeshPro playerIndexText;
    private PlayerInput _playerInput;


    #region Inits
    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        lives.OnChange += OnLivesChanged;
        score.OnChange += OnScoreChanged;
        hasShield.OnChange += OnShieldChanged;
        ultimateMeter.OnChange += OnUltimateChanged;  // ← NEU
        UpdateLivesUI();
        UpdateScoreUI();
        UpdateUltimateUI();  // ← NEU
        CreateShieldVisual();
    }


    public override void OnStopNetwork()
    {
        base.OnStopNetwork();
        lives.OnChange -= OnLivesChanged;
        score.OnChange -= OnScoreChanged;
        hasShield.OnChange -= OnShieldChanged;
        ultimateMeter.OnChange -= OnUltimateChanged;

    }

    private void OnDisable()
    {
        playerColor.OnChange -= OnColorChanged;
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

    private IEnumerator DelayedIsOwner()
    {
        playerColor.OnChange += OnColorChanged;
        playerRenderer = GetComponentInChildren<Renderer>();
        playerRenderer.material = new Material(playerRenderer.material);
        playerRenderer.material.color = playerColor.Value;

        var pi = GetComponent<PlayerInput>();
        Debug.Log($"pi = {pi}, text = {playerIndexText}");
        if (playerIndexText != null && pi != null)
        {
            int index = pi.playerIndex;
            playerIndexText.text = $"Player {index}";
        }

        yield return null;
        if (IsOwner)
        {
            ChangeColor(Random.value, Random.value, Random.value);

            moveAction?.Enable();
            colorChangeAction?.Enable();
            shootSingleAction?.Enable();
            shootSpreadAction?.Enable();
            Debug.Log("Owner enabled all actions!");
            if (TimeManager != null)
                TimeManager.OnTick += OnTick;

            UpdateLivesUI();
            UpdateScoreUI();
        }
    }
    #endregion

    private void OnTick()
    {
        if (!IsOwner) return;

        if (isReady.Value)
        {
            HandleInput();
            HandleShooting();
        }
        else
        {
            CheckForChangeColor();
        }
    }

    #region ReadyStateHandling
    [ServerRpc]
    public void SetReadyStateServerRpc(string name)
    {
        isReady.Value = !isReady.Value;

        if (transform.position.x < 0)
        {
            OwnNetworkGameManager.Instance.Player1.Value = name;
        }
        else
        {
            OwnNetworkGameManager.Instance.Player2.Value = name;
        }

        OwnNetworkGameManager.Instance.DisableNameField(Owner, isReady.Value);
        OwnNetworkGameManager.Instance.CheckAndStartGame();
        OwnNetworkGameManager.Instance.RpcUpdateReadyButtonText(isReady.Value);
    }

    [ServerRpc]
    public void CmdSetReady(bool ready)
    {
        isReady.Value = ready;
        Debug.Log($"Player {Owner.ClientId} ready: {ready}");
    }
    #endregion

    #region Movement
    private void HandleInput()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();
        if (input.sqrMagnitude > 0f)
            MoveServerRpc(input);
    }

    [ServerRpc]
    private void MoveServerRpc(Vector2 input)
    {
        float dt = (float)TimeManager.TickDelta;

        float newX = transform.position.x + input.x * moveSpeed * dt;
        float newY = transform.position.y + input.y * moveSpeed * dt;

        newX = Mathf.Clamp(newX, minX, maxX);
        newY = Mathf.Clamp(newY, minY, maxY);

        transform.position = new Vector3(newX, newY, transform.position.z);
    }
    #endregion

    #region Shooting
    private void HandleShooting()
    {
        bool isSinglePressed = shootSingleAction.IsPressed();
        bool isSpreadPressed = shootSpreadAction.IsPressed();

        // Ultimate Mode - läuft bis Meter leer!
        if (ultimateActive)
        {
            // Schieße wenn Linke Maus gedrückt
            if (isSinglePressed)
            {
                ShootServerRpc(1);
            }

            // Entlade immer (auch ohne Schuss!)
            DrainUltimateServerRpc(ultimateDrainRate * (float)TimeManager.TickDelta);

            // Deaktiviere wenn leer
            if (ultimateMeter.Value <= 0f)
            {
                ultimateActive = false;
                Debug.Log("Ultimate deactivated - meter empty!");
            }

            return;
        }

        // Ultimate starten bei 100%
        if (isSinglePressed && ultimateMeter.Value >= 100f && !ultimateActive)
        {
            ultimateActive = true;
            Debug.Log("ULTIMATE ACTIVATED!");
            return;
        }

        // Normal Shooting
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




    [ServerRpc]
    private void ShootServerRpc(int pattern)
    {
        Vector3 spawnPos = firePoint ? firePoint.position : transform.position;
        Vector2 baseDirection = Vector2.up;

        if (pattern == 1)
        {
            // Single Shot
            SpawnBullet(spawnPos, baseDirection);
        }
        else if (pattern == 2)
        {
            // Triple Shot - 3 Bullets im Fächer
            Vector3 leftOffset = new Vector3(-0.4f, 0f, 0f);
            Vector3 centerOffset = new Vector3(0f, 0f, 0f);
            Vector3 rightOffset = new Vector3(0.4f, 0f, 0f);

            SpawnBullet(spawnPos + leftOffset, Rotate(baseDirection, 30f));
            SpawnBullet(spawnPos + centerOffset, baseDirection);
            SpawnBullet(spawnPos + rightOffset, Rotate(baseDirection, -30f));
        }
    }

    private void SpawnBullet(Vector3 position, Vector2 direction)
    {
        GameObject bulletObj = Instantiate(bulletPrefab, position, Quaternion.identity);

        BulletController bulletCtrl = bulletObj.GetComponent<BulletController>();
        if (bulletCtrl != null)
        {
            bulletCtrl.InitializeBullet(direction);
            bulletCtrl.shooterClientId = Owner.ClientId;
        }

        NetworkObject netObj = bulletObj.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            ServerManager.Spawn(bulletObj, Owner);
        }
    }

    private Vector2 Rotate(Vector2 v, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }
    #endregion

    #region Shield
    [ServerRpc(RequireOwnership = false)]
    public void ActivateShieldServerRpc(float duration)
    {
        if (isDead.Value) return;

        Debug.Log($"SERVER: Activating shield for player {Owner.ClientId}, duration {duration}");

        hasShield.Value = true;

        // Starte Flackern auf ALLEN Clients
        float flickerStartTime = duration - 2f;
        if (flickerStartTime > 0)
        {
            StartFlickerObserversRpc(flickerStartTime);
        }

        Invoke(nameof(DeactivateShield), duration);
    }

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
        }
    }

    private IEnumerator FlickerShield()
    {
        while (hasShield.Value)
        {
            if (shieldVisual != null)
            {
                shieldVisual.SetActive(!shieldVisual.activeSelf);
            }
            yield return new WaitForSeconds(0.2f);
        }

        // Stelle sicher dass Shield am Ende aus ist
        if (shieldVisual != null)
        {
            shieldVisual.SetActive(false);
        }
    }

    private void DeactivateShield()
    {
        hasShield.Value = false;
        Debug.Log($"Player {Owner.ClientId} shield deactivated!");
    }

    private void OnShieldChanged(bool prev, bool next, bool asServer)
    {
        UpdateShieldVisual();
    }

    private void CreateShieldVisual()
    {
        shieldVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        shieldVisual.name = "ShieldVisual";
        shieldVisual.transform.SetParent(transform);
        shieldVisual.transform.localPosition = Vector3.zero;
        shieldVisual.transform.localScale = Vector3.one * 1.5f;

        Collider col = shieldVisual.GetComponent<Collider>();
        if (col != null) Destroy(col);

        Renderer shieldRenderer = shieldVisual.GetComponent<Renderer>();

        // Verwende Transparent Shader
        Material shieldMat = new Material(Shader.Find("Sprites/Default"));
        shieldMat.color = new Color(0f, 1f, 0f, 0.01f);  // Grün, 30% sichtbar
        shieldRenderer.material = shieldMat;

        shieldVisual.SetActive(false);
    }



    private void UpdateShieldVisual()
    {
        if (shieldVisual != null)
        {
            shieldVisual.SetActive(hasShield.Value);
        }
    }
    #endregion

    #region Health
    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc()
    {
        // Shield blockt Schaden!
        if (hasShield.Value)
        {
            Debug.Log($"Player {Owner.ClientId} blocked damage with shield!");
            return;
        }

        lives.Value--;
        Debug.Log($"Player {Owner.ClientId} took damage! Lives: {lives.Value}");

        if (lives.Value <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log($"Player {Owner.ClientId} died! GAME OVER! Final Score: {score.Value}");

        isDead.Value = true;

        // Submit auf dem Owner-Client via TargetRpc
        if (IsServerInitialized)
        {
            SubmitScoreTargetRpc(Owner, score.Value);
        }

        DisablePlayerObserversRpc();

        OwnNetworkGameManager gameManager = FindFirstObjectByType<OwnNetworkGameManager>();
        if (gameManager != null)
        {
            gameManager.OnPlayerDied(Owner.ClientId);
        }
    }

    [TargetRpc]
    private void SubmitScoreTargetRpc(NetworkConnection conn, int finalScore)
    {
        Debug.Log($"SubmitScoreTargetRpc called! Owner.ClientId={Owner.ClientId}, finalScore={finalScore}");

        if (HighscoreClient.Instance != null)
        {
            string playerName = OwnNetworkGameManager.Instance.Player1.Value;
            if (Owner.ClientId == 1)
                playerName = OwnNetworkGameManager.Instance.Player2.Value;

            Debug.Log($"Player Name: '{playerName}' (Length: {playerName?.Length ?? 0})");
            Debug.Log($"Final Score: {finalScore}");

            // Falls Name leer, verwende Default
            if (string.IsNullOrEmpty(playerName))
            {
                playerName = $"Player{Owner.ClientId}";
                Debug.LogWarning($"Name was empty! Using default: {playerName}");
            }

            Debug.Log($"Submitting score: Name={playerName}, Score={finalScore}");
            HighscoreClient.Instance.SubmitScore(playerName, finalScore);
        }
        else
        {
            Debug.LogError("HighscoreClient.Instance is NULL!");
        }
    }





    [ObserversRpc]
    private void DisablePlayerObserversRpc()
    {
        gameObject.SetActive(false);
        Debug.Log($"Player {Owner.ClientId} completely disabled!");
    }

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
            if (lives.Value <= 0)
            {
                livesUI.text = "GAME OVER";
                livesUI.color = Color.red;
            }
            else
            {
                livesUI.text = $"Lives: {lives.Value}";
                livesUI.color = Color.white;
            }
        }
    }

    public void AddScore(int points)
    {
        if (!IsServerInitialized) return;
        score.Value += points;
        Debug.Log($"Player {Owner.ClientId} score: {score.Value}");

        // Lade Ultimate Meter auf
        ChargeUltimate(ultimateChargePerKill);

        // Check für eigenes PowerUp
        if (score.Value >= lastPowerUpScore + powerUpScoreInterval)
        {
            lastPowerUpScore = score.Value;
            SpawnPowerUpForMe();
        }
    }


    private void SpawnPowerUpForMe()
    {
        EnemySpawner spawner = FindFirstObjectByType<EnemySpawner>();
        if (spawner != null)
        {
            spawner.SpawnPowerUpNow();
            Debug.Log($"Player {Owner.ClientId} spawned PowerUp at score {score.Value}!");
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
        {
            scoreUI.text = $"Score: {score.Value}";
        }
    }
    #endregion

    #region ColorChange
    private void CheckForChangeColor()
    {
        if (!colorChangeAction.triggered) return;

        float r = Random.value;
        float g = Random.value;
        float b = Random.value;
        ChangeColor(r, g, b);
    }

    [ServerRpc]
    private void ChangeColor(float r, float g, float b)
    {
        playerColor.Value = new Color(r, g, b);
    }

    private void OnColorChanged(Color prevColor, Color newColor, bool asServer)
    {
        playerRenderer.material.color = newColor;
    }
    #endregion
    #region Ultimate
    private Coroutine blinkCoroutine = null;

    public void ChargeUltimate(float amount)
    {
        if (!IsServerInitialized) return;

        // Nicht aufladen während Ultimate aktiv!
        if (ultimateActive) return;

        ultimateMeter.Value = Mathf.Clamp(ultimateMeter.Value + amount, 0f, 100f);
        Debug.Log($"Player {Owner.ClientId} ultimate charged: {ultimateMeter.Value}%");
    }

    [ServerRpc]
    private void DrainUltimateServerRpc(float amount)
    {
        ultimateMeter.Value = Mathf.Max(0f, ultimateMeter.Value - amount);
    }

    private void OnUltimateChanged(float prev, float next, bool asServer)
    {
        UpdateUltimateUI();

        // Blinken nur für Owner
        if (!IsOwner) return;

        // Starte Blinken bei 100%
        if (next >= 100f && blinkCoroutine == null)
        {
            blinkCoroutine = StartCoroutine(BlinkUltimateSlider());
        }
        // Stoppe Blinken wenn unter 100%
        else if (next < 100f && blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;

            // Setze Farbe zurück
            UnityEngine.UI.Slider slider = GameObject.Find("UltimateSlider")?.GetComponent<UnityEngine.UI.Slider>();
            if (slider != null)
            {
                UnityEngine.UI.Image fillImage = slider.fillRect?.GetComponent<UnityEngine.UI.Image>();
                if (fillImage != null)
                {
                    fillImage.color = Color.yellow;
                }
            }
        }
    }

    private void UpdateUltimateUI()
    {
        if (!IsOwner) return;

        UnityEngine.UI.Slider ultimateUI = GameObject.Find("UltimateSlider")?.GetComponent<UnityEngine.UI.Slider>();
        if (ultimateUI != null)
        {
            ultimateUI.value = ultimateMeter.Value;
        }
    }

    private IEnumerator BlinkUltimateSlider()
    {
        UnityEngine.UI.Slider slider = GameObject.Find("UltimateSlider")?.GetComponent<UnityEngine.UI.Slider>();
        if (slider == null) yield break;

        UnityEngine.UI.Image fillImage = slider.fillRect?.GetComponent<UnityEngine.UI.Image>();
        if (fillImage == null) yield break;

        while (true)
        {
            fillImage.color = Color.yellow;
            yield return new WaitForSeconds(0.3f);
            fillImage.color = Color.red;
            yield return new WaitForSeconds(0.3f);
        }
    }
    #endregion



}
