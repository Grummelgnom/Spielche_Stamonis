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
    [SerializeField] private InputAction shootAction;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    private bool wasShootPressed = false;

    [Header("Player UI")]
    [SerializeField] private TextMeshPro playerIndexText;
    private PlayerInput _playerInput;


    #region Inits
    private void OnDisable()
    {
        playerColor.OnChange -= OnColorChanged;
        if (!IsOwner) return;

        moveAction?.Disable();
        colorChangeAction?.Disable();
        shootAction?.Disable();
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
            shootAction?.Enable();
            Debug.Log("Owner enabled all actions!");
            if (TimeManager != null)
                TimeManager.OnTick += OnTick;
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
        // Prüfe ob Space gerade gedrückt wurde (State-Wechsel)
        bool isShootPressed = shootAction.IsPressed();

        if (isShootPressed && !wasShootPressed)
        {
            // Space wurde GERADE gedrückt!
            Debug.Log($"Client {Owner.ClientId}: Space PRESSED!");
            ShootServerRpc();
        }

        // Speichere State für nächsten Tick
        wasShootPressed = isShootPressed;
    }

    [ServerRpc]
    private void ShootServerRpc()
    {
        Debug.Log($"Server: Client {Owner.ClientId} shoots!");

        Vector3 spawnPos = firePoint ? firePoint.position : transform.position;
        Quaternion spawnRot = firePoint ? firePoint.rotation : Quaternion.identity;

        // Erstelle Bullet
        GameObject bulletObj = Instantiate(bulletPrefab, spawnPos, spawnRot);

        // Initialisiere Bullet
        BulletController bulletCtrl = bulletObj.GetComponent<BulletController>();
        if (bulletCtrl != null)
        {
            bulletCtrl.InitializeBullet(transform.up);
        }

        // Spawne als NetworkObject
        NetworkObject netObj = bulletObj.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            ServerManager.Spawn(bulletObj, Owner);
            Debug.Log("Bullet spawned!");
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
