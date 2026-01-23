using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OwnNetworkGameManager : NetworkBehaviour
{
    // Singleton für globalen Zugriff
    public static OwnNetworkGameManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private TextMeshPro readyButtonText;     // 3D-Text für Ready-Button
    [SerializeField] private TMP_Text stateText;              // Status-Text (Score/Warten)
    [SerializeField] private TMP_Text player1NameText;        // Name Player 1 (links)
    [SerializeField] private TMP_Text player2NameText;        // Name Player 2 (rechts)
    [SerializeField] private TMP_InputField PlayerNameField;  // Eingabefeld für Spielernamen
    [SerializeField] private Button ReadyButton;              // Ready-Button

    // Synchronisierte Spielernamen (links = Player1, rechts = Player2)
    public readonly SyncVar<string> Player1 = new SyncVar<string>();
    public readonly SyncVar<string> Player2 = new SyncVar<string>();

    [Header("Score")]
    private readonly SyncVar<int> scoreP1 = new SyncVar<int>();  // Score Player 1
    private readonly SyncVar<int> scoreP2 = new SyncVar<int>();  // Score Player 2

    [Header("Game")]
    private readonly SyncVar<GameState> gameState = new SyncVar<GameState>();  // Aktueller Spielzustand
    public GameState CurrentState => gameState.Value;

    private void Awake()
    {
        // Singleton-Setup
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        // Callbacks für UI-Updates registrieren
        gameState.OnChange += OnStateChanged;
        scoreP1.OnChange += (oldVal, newVal, asServer) => UpdateStateText();
        scoreP2.OnChange += (oldVal, newVal, asServer) => UpdateStateText();
        Player1.OnChange += (oldVal, newVal, asServer) => ApplyNameToUI(1, newVal);
        Player2.OnChange += (oldVal, newVal, asServer) => ApplyNameToUI(2, newVal);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        // Initialwerte setzen
        gameState.Value = GameState.WaitingForPlayers;
        scoreP1.Value = 0;
        scoreP2.Value = 0;
    }

    private void Start()
    {
        // Initial UI-Setup (Ready-UI verstecken, Namensanzeige zeigen)
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

        // Client-seitig Ready-UI zeigen
        if (PlayerNameField != null)
            PlayerNameField.gameObject.SetActive(true);
        if (ReadyButton != null)
            ReadyButton.gameObject.SetActive(true);

        // Namensanzeige aktivieren + Initialwerte setzen
        if (player1NameText != null)
        {
            player1NameText.gameObject.SetActive(true);
            player1NameText.text = string.IsNullOrEmpty(Player1.Value) ? "Player 1" : Player1.Value;
        }
        if (player2NameText != null)
        {
            player2NameText.gameObject.SetActive(true);
            player2NameText.text = string.IsNullOrEmpty(Player2.Value) ? "Player 2" : Player2.Value;
        }
    }

    // Namen in UI anzeigen (wird bei SyncVar-Änderung aufgerufen)
    private void ApplyNameToUI(int index, string value)
    {
        string finalName = string.IsNullOrEmpty(value)
            ? (index == 1 ? "Player 1" : "Player 2")
            : value;

        if (index == 1 && player1NameText != null)
            player1NameText.text = finalName;
        else if (index == 2 && player2NameText != null)
            player2NameText.text = finalName;
    }

    // Wird von Ready-Button aufgerufen (Client-seitig)
    public void SetPlayerReady()
    {
        // Lokalen OwnPlayer finden
        var localPlayer = FindObjectsByType<OwnPlayerController>(FindObjectsSortMode.None)
            .FirstOrDefault(p => p.IsOwner);

        if (localPlayer == null || localPlayer.IsReady)
            return;

        // Button optisch bestätigen (lokal)
        if (ReadyButton != null)
            ReadyButton.image.color = Color.green;

        // Namen und Ready-State serverseitig setzen
        localPlayer.SetReadyStateServerRpc(PlayerNameField != null ? PlayerNameField.text : "");
        localPlayer.CmdSetReady(true);

        // Ready-UI deaktivieren/ausblenden
        DisableReadyUIForThisClient();
    }

    // Ready-UI für diesen Client ausblenden und deaktivieren
    private void DisableReadyUIForThisClient()
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
    }

    // Server: Prüft, ob beide Spieler bereit → startet Spiel
    [Server]
    public void CheckAndStartGame()
    {
        if (CurrentState != GameState.WaitingForPlayers)
            return;

        var players = FindObjectsByType<OwnPlayerController>(FindObjectsSortMode.None);
        if (players.Length >= 2 && players.All(p => p.IsReady))
        {
            gameState.Value = GameState.Playing;
            // Enemy-Wellen starten
            if (EnemySpawner.Instance != null)
                EnemySpawner.Instance.StartWave();
        }
    }

    // Spieler gestorben (Game Over für diesen Spieler)
    public void OnPlayerDied(int clientId)
    {
        // TODO: Prüfen ob beide tot → Game Over
    }

    // Zustandsänderung → UI updaten
    private void OnStateChanged(GameState oldState, GameState newState, bool asServer)
    {
        UpdateStateText();
    }

    // Status-Text aktualisieren (Score oder Wartezustand)
    private void UpdateStateText()
    {
        if (stateText == null)
            return;

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

    // Server: Punkt für Spieler vergeben
    [Server]
    public void ScorePoint(int playerIndex)
    {
        if (gameState.Value != GameState.Playing)
            return;

        // Score erhöhen
        if (playerIndex == 0)
            scoreP1.Value++;
        else if (playerIndex == 1)
            scoreP2.Value++;

        // Spielende bei 10 Punkten
        if (scoreP1.Value >= 10 || scoreP2.Value >= 10)
            gameState.Value = GameState.Finished;
        else
            StartCoroutine(OwnBallSpawner.Instance.SpawnBall(6f));  // Nächste Runde
    }
}

// Spielzustände
public enum GameState
{
    WaitingForPlayers,  // Warten auf 2 ready Spieler
    Playing,            // Spiel läuft (Enemies spawnen)
    Finished            // Ein Spieler hat 10 Punkte erreicht
}
