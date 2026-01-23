using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OwnNetworkGameManager : NetworkBehaviour
{
    public static OwnNetworkGameManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private TextMeshPro readyButtonText; // 3D-Text, kein UGUI
    [SerializeField] private TMP_Text stateText;
    [SerializeField] private TMP_Text player1NameText;    // oben links
    [SerializeField] private TMP_Text player2NameText;    // oben rechts
    [SerializeField] private TMP_InputField PlayerNameField;
    [SerializeField] private Button ReadyButton;

    // Diese SyncVars bleiben: Player1 links, Player2 rechts
    public readonly SyncVar<string> Player1 = new SyncVar<string>();
    public readonly SyncVar<string> Player2 = new SyncVar<string>();

    [Header("Score")]
    private readonly SyncVar<int> scoreP1 = new SyncVar<int>();
    private readonly SyncVar<int> scoreP2 = new SyncVar<int>();

    [Header("Game")]
    private readonly SyncVar<GameState> gameState = new SyncVar<GameState>();
    public GameState CurrentState => gameState.Value;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        gameState.OnChange += OnStateChanged;
        scoreP1.OnChange += (oldVal, newVal, asServer) => UpdateStateText();
        scoreP2.OnChange += (oldVal, newVal, asServer) => UpdateStateText();

        Player1.OnChange += (oldVal, newVal, asServer) =>
        {
            ApplyNameToUI(1, newVal);
        };

        Player2.OnChange += (oldVal, newVal, asServer) =>
        {
            ApplyNameToUI(2, newVal);
        };
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        gameState.Value = GameState.WaitingForPlayers;
        scoreP1.Value = 0;
        scoreP2.Value = 0;
    }

    private void Start()
    {
        // unverändert …
        if (PlayerNameField != null)
            PlayerNameField.gameObject.SetActive(false);
        if (ReadyButton != null)
            ReadyButton.gameObject.SetActive(false);

        if (player1NameText != null)
            player1NameText.gameObject.SetActive(true);
        if (player2NameText != null)
            player2NameText.gameObject.SetActive(true);
    }


    public override void OnStartClient()
    {
        base.OnStartClient();

        // Eingabe-UI zeigen
        if (PlayerNameField != null)
            PlayerNameField.gameObject.SetActive(true);
        if (ReadyButton != null)
            ReadyButton.gameObject.SetActive(true);

        // Namen-Anzeige explizit aktivieren (oben links/rechts)
        if (player1NameText != null)
            player1NameText.gameObject.SetActive(true);
        if (player2NameText != null)
            player2NameText.gameObject.SetActive(true);

        // FIX: Initialwerte direkt einmal setzen (falls OnChange schon vorher passiert ist)
        if (player1NameText != null)
            player1NameText.text = string.IsNullOrEmpty(Player1.Value) ? "Player 1" : Player1.Value;

        if (player2NameText != null)
            player2NameText.text = string.IsNullOrEmpty(Player2.Value) ? "Player 2" : Player2.Value;

        Debug.Log("Client connected - UI & Namen-Anzeige aktiviert!");
    }

    // ────────────────────────────────────────────────
    // Ready-Button Logik + UI ausblenden (FIXED)
    // ────────────────────────────────────────────────
    private void ApplyNameToUI(int index, string value)
    {
        string finalName = string.IsNullOrEmpty(value)
            ? (index == 1 ? "Player 1" : "Player 2")
            : value;

        if (index == 1)
        {
            if (player1NameText != null)
                player1NameText.text = finalName;
        }
        else
        {
            if (player2NameText != null)
                player2NameText.text = finalName;
        }
    }


    public void SetPlayerReady()
    {
        // lokalen Owner-Player finden
        var localPlayer = FindObjectsByType<OwnPlayerController>(FindObjectsSortMode.None)
            .FirstOrDefault(p => p.IsOwner);

        if (localPlayer == null)
            return;

        // Wenn schon ready: nichts mehr machen (UI ist eh weg)
        if (localPlayer.IsReady)
            return;

        // Optional: Button-Farbe (rein lokal)
        if (ReadyButton != null)
            ReadyButton.image.color = Color.green;

        // FIX: Ready serverseitig setzen + Namen setzen
        localPlayer.SetReadyStateServerRpc(PlayerNameField != null ? PlayerNameField.text : "");

        // FIX: CmdSetReady NICHT togglen (sonst hängt es am alten SyncVar-Wert)
        localPlayer.CmdSetReady(true);

        // FIX: UI immer ausblenden nachdem ready gedrückt wurde
        DisableReadyUIForThisPlayer();
    }

    private void DisableReadyUIForThisPlayer()
    {
        if (PlayerNameField != null)
        {
            PlayerNameField.interactable = false;
            PlayerNameField.gameObject.SetActive(false);
        }

        if (ReadyButton != null)
        {
            ReadyButton.interactable = false;
            ReadyButton.gameObject.SetActive(false);
        }

        Debug.Log("Ready-UI für diesen Client ausgeblendet und deaktiviert");
    }

    // ────────────────────────────────────────────────
    // Rest des Skripts (unverändert)
    // ────────────────────────────────────────────────
    public void OnPlayerDied(int clientId)
    {
        Debug.Log($"Player {clientId} has died! Game Over for this player!");
        // Hier später: Check ob beide tot sind, dann komplett Game Over
    }

    [Server]
    public void CheckAndStartGame()
    {
        if (CurrentState != GameState.WaitingForPlayers) return;
        var players = FindObjectsByType<OwnPlayerController>(FindObjectsSortMode.None);
        if (players.Length >= 2 && players.All(p => p.IsReady))
        {
            gameState.Value = GameState.Playing;
            Debug.Log("Game started! Spawning enemies...");
            if (EnemySpawner.Instance != null)
            {
                EnemySpawner.Instance.StartWave();
                Debug.Log("EnemySpawner.StartWave() called!");
            }
            else
            {
                Debug.LogError("EnemySpawner.Instance is NULL!");
            }
        }
    }

    private void OnStateChanged(GameState oldState, GameState newState, bool asServer)
    {
        UpdateStateText();
    }

    private void UpdateStateText()
    {
        if (stateText == null) return;
        switch (gameState.Value)
        {
            case GameState.WaitingForPlayers:
                stateText.text = "Waiting for players...";
                break;
            case GameState.Playing:
                stateText.text = $"{scoreP1.Value}:{scoreP2.Value}";
                break;
            case GameState.Finished:
                stateText.text = "Finished";
                break;
        }
    }

    [Server]
    public void ScorePoint(int playerIndex)
    {
        if (gameState.Value != GameState.Playing) return;
        if (playerIndex == 0)
            scoreP1.Value++;
        else if (playerIndex == 1)
            scoreP2.Value++;
        if (scoreP1.Value >= 10 || scoreP2.Value >= 10)
        {
            gameState.Value = GameState.Finished;
        }
        else
        {
            StartCoroutine(OwnBallSpawner.Instance.SpawnBall(6f));
        }
    }
}

public enum GameState
{
    WaitingForPlayers,
    Playing,
    Finished
}
