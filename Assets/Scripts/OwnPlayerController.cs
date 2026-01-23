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

    [Header("Health & Score")]
    public readonly SyncVar<int> lives = new SyncVar<int>(3);
    public readonly SyncVar<bool> isDead = new SyncVar<bool>(false);
    public readonly SyncVar<int> score = new SyncVar<int>(0);
    public readonly SyncVar<bool> hasShield = new SyncVar<bool>(false);
    private int lastPowerUpScore = 0;

    [Header("Ultimate Settings")]
    public readonly SyncVar<float> ultimateMeter = new SyncVar<float>(0f);
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

    [Header("Shooting")]
    [SerializeField] private InputAction shootSingleAction;
    [SerializeField] private InputAction shootSpreadAction;
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
        if (TimeManager != null) TimeManager.OnTick -= OnTick;
    }

    private void Start()
    {
        _playerInput = GetComponent<PlayerInput>();
        StartCoroutine(DelayedIsOwner());
    }

    private IEnumerator DelayedIsOwner()
    {
        playerRenderer = GetComponentInChildren<Renderer>();
        if (playerRenderer != null) { playerRenderer.material = new Material(playerRenderer.material); playerRenderer.material.color = playerColor.Value; }
        var pi = GetComponent<PlayerInput>();
        if (playerIndexText != null && pi != null) playerIndexText.text = $"Player {pi.playerIndex}";
        yield return null;
        if (IsOwner)
        {
            ChangeColorServerRpc(Random.value, Random.value, Random.value);
            moveAction?.Enable();
            colorChangeAction?.Enable();
            shootSingleAction?.Enable();
            shootSpreadAction?.Enable();
            if (TimeManager != null) TimeManager.OnTick += OnTick;
            UpdateLivesUI();
            UpdateScoreUI();
            UpdateUltimateUI();
        }
    }

    private void OnTick()
    {
        if (!IsOwner) return;
        if (isReady.Value) { HandleInput(); HandleShooting(); }
        else CheckForChangeColor();
    }

    [ServerRpc]
    public void SetReadyStateServerRpc(string name)
    {
        isReady.Value = !isReady.Value;
        if (transform.position.x < 0) OwnNetworkGameManager.Instance.Player1.Value = name;
        else OwnNetworkGameManager.Instance.Player2.Value = name;
        OwnNetworkGameManager.Instance.CheckAndStartGame();
        
    }

    [ServerRpc] public void CmdSetReady(bool ready) { isReady.Value = ready; }

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
        if (input.sqrMagnitude > 0f) MoveServerRpc(input);
    }

    private void HandleShooting()
    {
        bool isSinglePressed = shootSingleAction.IsPressed();
        bool isSpreadPressed = shootSpreadAction.IsPressed();
        if (ultimateActive)
        {
            if (isSinglePressed) ShootServerRpc(1);
            DrainUltimateServerRpc(ultimateDrainRate * (float)TimeManager.TickDelta);
            if (ultimateMeter.Value <= 0f) ultimateActive = false;
            return;
        }
        if (isSinglePressed && ultimateMeter.Value >= 100f && !ultimateActive) { ultimateActive = true; return; }
        if (isSinglePressed && !wasSinglePressed && Time.time >= nextFireTime) { ShootServerRpc(1); nextFireTime = Time.time + fireRate; }
        wasSinglePressed = isSinglePressed;
        if (isSpreadPressed && !wasSpreadPressed && Time.time >= nextFireTime) { ShootServerRpc(2); nextFireTime = Time.time + fireRate; }
        wasSpreadPressed = isSpreadPressed;
    }

    [ServerRpc]
    private void ShootServerRpc(int pattern)
    {
        Vector3 spawnPos = firePoint ? firePoint.position : transform.position;
        Vector2 baseDirection = Vector2.up;
        if (pattern == 1) SpawnBullet(spawnPos, baseDirection);
        else if (pattern == 2)
        {
            SpawnBullet(spawnPos + new Vector3(-0.4f, 0, 0), Rotate(baseDirection, 30f));
            SpawnBullet(spawnPos, baseDirection);
            SpawnBullet(spawnPos + new Vector3(0.4f, 0, 0), Rotate(baseDirection, -30f));
        }
        PlaySoundObserversRpc(1);
    }

    private void SpawnBullet(Vector3 position, Vector2 direction)
    {
        GameObject bulletObj = Instantiate(bulletPrefab, position, Quaternion.identity);
        var bc = bulletObj.GetComponent<BulletController>();
        if (bc != null) { bc.InitializeBullet(direction); bc.shooterClientId = Owner.ClientId; }
        ServerManager.Spawn(bulletObj, Owner);
    }

    private Vector2 Rotate(Vector2 v, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(radians);
        float cos = Mathf.Cos(radians);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc()
    {
        if (hasShield.Value) return;
        lives.Value--;
        if (lives.Value <= 0) Die();
    }

    private void Die()
    {
        isDead.Value = true;
        PlaySoundObserversRpc(3);
        if (IsServerInitialized) SubmitScoreTargetRpc(Owner, score.Value);
        DisablePlayerObserversRpc();
        OwnNetworkGameManager.Instance.OnPlayerDied(Owner.ClientId);
    }

    [TargetRpc]
    private void SubmitScoreTargetRpc(FishNet.Connection.NetworkConnection conn, int finalScore)
    {
        string playerName = Owner.ClientId == 0 ? OwnNetworkGameManager.Instance.Player1.Value : OwnNetworkGameManager.Instance.Player2.Value;
        if (string.IsNullOrEmpty(playerName)) playerName = $"Player{Owner.ClientId}";
        HighscoreClient.Instance?.SubmitScore(playerName, finalScore);
        Invoke(nameof(ShowHighscoreList), 1f);
    }

    private void ShowHighscoreList() { FindFirstObjectByType<HighscoreDisplay>()?.ShowHighscores(); }

    [ObserversRpc] private void DisablePlayerObserversRpc() { gameObject.SetActive(false); }

    [ServerRpc(RequireOwnership = false)]
    public void ActivateShieldServerRpc(float duration)
    {
        if (isDead.Value) return;
        hasShield.Value = true;
        float flickerStartTime = duration - 2f;
        if (flickerStartTime > 0) StartFlickerObserversRpc(flickerStartTime);
        Invoke(nameof(DeactivateShield), duration);
        PlaySoundObserversRpc(2);
    }

    private void DeactivateShield() { hasShield.Value = false; }

    [ObserversRpc]
    private void PlaySoundObserversRpc(int type)
    {
        if (SimpleSoundManager.Instance == null) return;
        if (type == 1) SimpleSoundManager.Instance.PlayLaserSound();
        else if (type == 2) SimpleSoundManager.Instance.PlayPowerUpSound();
        else if (type == 3) SimpleSoundManager.Instance.PlayPlayerDeathSound();
    }

    private void OnLivesChanged(int prev, int next, bool asServer) { UpdateLivesUI(); }
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
        if (!ultimateActive) ultimateMeter.Value = Mathf.Clamp(ultimateMeter.Value + points, 0f, 100f);
        if (score.Value >= lastPowerUpScore + powerUpScoreInterval) { lastPowerUpScore = score.Value; EnemySpawner.Instance?.SpawnPowerUpNow(); }
    }

    private void OnScoreChanged(int prev, int next, bool asServer) { UpdateScoreUI(); }
    private void UpdateScoreUI()
    {
        if (!IsOwner) return;
        TMP_Text scoreUI = GameObject.Find("ScoreText")?.GetComponent<TMP_Text>();
        if (scoreUI != null) scoreUI.text = $"Score: {score.Value}";
    }

    [ServerRpc] private void DrainUltimateServerRpc(float amount) { ultimateMeter.Value = Mathf.Max(0f, ultimateMeter.Value - amount); }
    private void OnUltimateChanged(float prev, float next, bool asServer)
    {
        UpdateUltimateUI();
        if (!IsOwner) return;
        SimpleSoundManager.Instance?.SetUltimateLoop(next >= 100f);
        if (next >= 100f && blinkCoroutine == null) blinkCoroutine = StartCoroutine(BlinkUltimateSlider());
        else if (next < 100f && blinkCoroutine != null) { StopCoroutine(blinkCoroutine); blinkCoroutine = null; ResetSliderColor(); }
    }
    private void UpdateUltimateUI()
    {
        if (!IsOwner) return;
        var slider = GameObject.Find("UltimateSlider")?.GetComponent<UnityEngine.UI.Slider>();
        if (slider != null) slider.value = ultimateMeter.Value;
    }
    private void ResetSliderColor() { var slider = GameObject.Find("UltimateSlider")?.GetComponent<UnityEngine.UI.Slider>(); var img = slider?.fillRect.GetComponent<UnityEngine.UI.Image>(); if (img != null) img.color = Color.yellow; }
    private IEnumerator BlinkUltimateSlider()
    {
        var slider = GameObject.Find("UltimateSlider")?.GetComponent<UnityEngine.UI.Slider>();
        var img = slider?.fillRect.GetComponent<UnityEngine.UI.Image>();
        while (img != null) { img.color = Color.yellow; yield return new WaitForSeconds(0.3f); img.color = Color.red; yield return new WaitForSeconds(0.3f); }
    }

    private void OnShieldChanged(bool prev, bool next, bool asServer) { shieldVisual?.SetActive(next); }
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
    [ObserversRpc] private void StartFlickerObserversRpc(float delay) { Invoke(nameof(StartFlickerLocal), delay); }
    private void StartFlickerLocal() { if (shieldVisual != null) { StartCoroutine(FlickerShield()); SimpleSoundManager.Instance?.PlayShieldWarningSound(); } }
    private IEnumerator FlickerShield() { while (hasShield.Value) { if (shieldVisual != null) shieldVisual.SetActive(!shieldVisual.activeSelf); yield return new WaitForSeconds(0.2f); } if (shieldVisual != null) shieldVisual.SetActive(false); }

    private void CheckForChangeColor()
    {
        if (colorChangeAction == null || !colorChangeAction.triggered) return;
        ChangeColorServerRpc(Random.value, Random.value, Random.value);
    }
    [ServerRpc] private void ChangeColorServerRpc(float r, float g, float b) { playerColor.Value = new Color(r, g, b); }
    private void OnColorChanged(Color prev, Color next, bool asServer) { if (playerRenderer != null) playerRenderer.material.color = next; }
}
