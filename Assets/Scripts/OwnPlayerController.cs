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
    public readonly SyncVar<bool> hasShield = new SyncVar<bool>(false);

    private Renderer playerRenderer;
    private GameObject shieldVisual;

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
        UpdateLivesUI();
        UpdateScoreUI();
        CreateShieldVisual();
    }

    public override void OnStopNetwork()
    {
        base.OnStopNetwork();
        lives.OnChange -= OnLivesChanged;
        score.OnChange -= OnScoreChanged;
        hasShield.OnChange -= OnShieldChanged;
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
        // Single Shot - Linke Maustaste
        bool isSinglePressed = shootSingleAction.IsPressed();
        if (isSinglePressed && !wasSinglePressed)
        {
            ShootServerRpc(1);
        }
        wasSinglePressed = isSinglePressed;

        // Spread Shot - Rechte Maustaste  
        bool isSpreadPressed = shootSpreadAction.IsPressed();
        if (isSpreadPressed && !wasSpreadPressed)
        {
            ShootServerRpc(2);
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

        Invoke(nameof(DeactivateShield), duration);
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
        // Erstelle grünen Ring um Player (einfacher!)
        shieldVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        shieldVisual.name = "ShieldVisual";
        shieldVisual.transform.SetParent(transform);
        shieldVisual.transform.localPosition = Vector3.zero;
        shieldVisual.transform.localScale = Vector3.one * 1.5f;

        // Entferne Collider
        Collider col = shieldVisual.GetComponent<Collider>();
        if (col != null) Destroy(col);

        // Einfaches grünes Material
        Renderer shieldRenderer = shieldVisual.GetComponent<Renderer>();
        shieldRenderer.material.color = new Color(0f, 1f, 0f, 0.5f);

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
        Debug.Log($"Player {Owner.ClientId} died! GAME OVER!");

        isDead.Value = true;

        DisablePlayerObserversRpc();

        OwnNetworkGameManager gameManager = FindFirstObjectByType<OwnNetworkGameManager>();
        if (gameManager != null)
        {
            gameManager.OnPlayerDied(Owner.ClientId);
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

        // Check für PowerUp Spawn
        EnemySpawner spawner = FindFirstObjectByType<EnemySpawner>();
        if (spawner != null)
        {
            spawner.CheckPowerUpSpawn(score.Value);
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
}
