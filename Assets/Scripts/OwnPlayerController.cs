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

    private Renderer playerRenderer;

    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float minY = -4f;
    [SerializeField] private float maxY = 4f;
    [SerializeField] private float minX = -4f;
    [SerializeField] private float maxX = 4f;

    [Header("Input System")]
    [SerializeField] private InputAction moveAction;
    [SerializeField] private InputAction colorChangeAction;

    [Header("Shooting")]
    [SerializeField] private InputAction shootSingleAction;  // Linke Maustaste
    [SerializeField] private InputAction shootSpreadAction;  // Rechte Maustaste
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
        UpdateLivesUI();
    }

    public override void OnStopNetwork()
    {
        base.OnStopNetwork();
        lives.OnChange -= OnLivesChanged;
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

    #region Health
    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc()
    {
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

        // Deaktiviere Spieler (unsichtbar und inaktiv)
        DisablePlayerObserversRpc();

        // Benachrichtige GameManager
        OwnNetworkGameManager gameManager = FindFirstObjectByType<OwnNetworkGameManager>();
        if (gameManager != null)
        {
            gameManager.OnPlayerDied(Owner.ClientId);
        }
    }

    [ObserversRpc]
    private void DisablePlayerObserversRpc()
    {
        // Renderer ausschalten (unsichtbar)
        if (playerRenderer != null)
            playerRenderer.enabled = false;

        // Collider ausschalten
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false;

        // Controls ausschalten
        if (IsOwner)
        {
            moveAction?.Disable();
            shootSingleAction?.Disable();
            shootSpreadAction?.Disable();

            if (TimeManager != null)
                TimeManager.OnTick -= OnTick;
        }

        Debug.Log($"Player {Owner.ClientId} disabled!");
    }

    private void OnLivesChanged(int prev, int next, bool asServer)
    {
        UpdateLivesUI();
    }

    private void UpdateLivesUI()
    {
        if (!IsOwner) return;

        // Finde das UI Element dynamisch
        TMP_Text livesUI = GameObject.Find("LivesText")?.GetComponent<TMP_Text>();
        if (livesUI != null)
        {
            livesUI.text = $"Lives: {lives.Value}";
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
