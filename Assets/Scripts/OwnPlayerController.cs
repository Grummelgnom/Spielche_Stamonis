using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEditor.Searcher.SearcherWindow.Alignment;


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
            int index = pi.playerIndex;              // 0,1,2,...
            playerIndexText.text = $"Player {index}";
        }

        //if (_playerInput != null && playerIndexText != null) {
        //    int index = _playerInput.playerIndex;
        //    playerIndexText.text = $"Ich bin Player {index}";
        //}

        yield return null; // Wait a frame to ensure ownership is set
        if (IsOwner)
        {
            ChangeColor(Random.value, Random.value, Random.value);

            moveAction?.Enable();
            colorChangeAction?.Enable();
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

    // Debug.Log, wenn Player ready.
    [ServerRpc]
    public void CmdSetReady(bool ready)
    {
        isReady.Value = ready;
        Debug.Log($"Player {Owner.ClientId} ready: {ready}");
    }

    #endregion

    #region Movement
    // Das hier ging so nicht wegen float wenn ich mich nicht irre.
    /* private void HandleInput()
    {
        float input = moveAction.ReadValue<float>();
        if(input != 0) Move(input);
    }
    */
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
        //Also possible: playerRenderer.material.color = playerColor.Value;
    }
#endregion
}
