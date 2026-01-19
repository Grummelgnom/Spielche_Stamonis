using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class OwnNetworkGameManager : NetworkBehaviour
{
    public static OwnNetworkGameManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private TextMeshPro readyButtonText;   // 3D-Text, kein UGUI
    [SerializeField] private TMP_Text stateText;
    [SerializeField] private TMP_Text player1NameText;
    [SerializeField] private TMP_Text player2NameText; 
    [SerializeField] private TMP_InputField PlayerNameField;
    [SerializeField] private Button StartButton;

    public readonly SyncVar<string> Player1 = new SyncVar<string>();
    public readonly SyncVar<string> Player2 = new SyncVar<string>();

    [Header("Score")]
    private readonly SyncVar<int> scoreP1 = new SyncVar<int>();
    private readonly SyncVar<int> scoreP2 = new SyncVar<int>();

    [Header("Game")]
    private readonly SyncVar<GameState> gameState = new SyncVar<GameState>();
    public GameState CurrentState => gameState.Value;

    [ObserversRpc]   // läuft auf allen Clients
    public void RpcUpdateReadyButtonText(bool isReady)
    {
        if (readyButtonText == null)
            return;

        readyButtonText.text = isReady ? "Start" : "Unready";
    }


    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        gameState.OnChange += OnStateChanged;
        scoreP1.OnChange += (oldVal, newVal, asServer) => UpdateStateText();
        scoreP2.OnChange += (oldVal, newVal, asServer) => UpdateStateText();
        Player1.OnChange += (oldVal, newVal, asServer) =>
        {
            if (player1NameText != null)
                player1NameText.text = newVal;
        };
        Player2.OnChange += (oldVal, newVal, asServer) =>
        {
            if (player2NameText != null)
                player2NameText.text = newVal;
        };
    }

    
    public override void OnStartServer()
    {
        base.OnStartServer();
        gameState.Value = GameState.WaitingForPlayers; // WaitingForPlayers wird hier gesetzt
        scoreP1.Value = 0;
        scoreP2.Value = 0;
    }
    
    #region State-Handling

    [Server]
    public void CheckAndStartGame()
    {
        if (CurrentState != GameState.WaitingForPlayers) return;

        var players = FindObjectsByType<OwnPlayerController>(FindObjectsSortMode.None);
        if (players.Length >= 2 && players.All(p => p.IsReady))
        {
            gameState.Value = GameState.Playing; // Playing wird hier gesetzt
            StartCoroutine(OwnBallSpawner.Instance.SpawnBall(5f));
        }
    }

    public void SetPlayerReady()
    {
        foreach (var player in FindObjectsByType<OwnPlayerController>(FindObjectsSortMode.None))
        {
            if (player.IsOwner)
            {
                if(!player.IsReady)
                    StartButton.image.color = Color.green;
                else  
                    StartButton.image.color = Color.yellow; 
                player.SetReadyStateServerRpc(PlayerNameField.text);
                player.CmdSetReady(!player.IsReady);
            }
        }
    }

    [TargetRpc]
    public void DisableNameField(NetworkConnection con, bool isOff)
    {
        PlayerNameField.gameObject.SetActive(!isOff);
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
    #endregion

    #region Scoring
    [Server]
    public void ScorePoint(int playerIndex)
    {
        if (gameState.Value != GameState.Playing) return;
        if (playerIndex == 0)
            scoreP1.Value++;
        else if (playerIndex == 1)
            scoreP2.Value++;
        // Check for win condition
        if (scoreP1.Value >= 10 || scoreP2.Value >= 10)
        {
            gameState.Value = GameState.Finished; // Finished wird hier gesetzt
        }
        else
        {
            StartCoroutine(OwnBallSpawner.Instance.SpawnBall(6f));
        }
    }

    #endregion
}


public enum GameState 
{
    WaitingForPlayers,
    Playing,
    Finished
}