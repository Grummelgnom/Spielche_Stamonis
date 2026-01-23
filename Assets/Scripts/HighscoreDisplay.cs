using UnityEngine;
using TMPro;
using System.Text;
using UnityEngine.UI;
using FishNet.Managing.Scened;

public class HighscoreDisplay : MonoBehaviour
{
    [SerializeField] private GameObject highscorePanel;              // Panel mit Highscore-Liste
    [SerializeField] private TextMeshProUGUI highscoreListText;      // Text-Komponente für Scores

    [Header("Zurück-Button")]
    [SerializeField] private Button backToMenuButton;                // Button zum Zurück ins Menü

    [Header("Ziel-Szene nach Klick")]
    [SerializeField] private string targetSceneName = "MainMenu";    // Szene zum Laden (Inspector)

    private void Awake()
    {
        // Initial UI verstecken
        if (highscorePanel != null)
            highscorePanel.SetActive(false);

        if (backToMenuButton != null)
        {
            backToMenuButton.gameObject.SetActive(false);
            // Button-Listener konfigurieren
            backToMenuButton.onClick.RemoveAllListeners();
            backToMenuButton.onClick.AddListener(OnBackButtonClicked);
        }
    }

    // Highscores anzeigen (wird von Spiel-Manager aufgerufen)
    public void ShowHighscores()
    {
        if (HighscoreClient.Instance != null)
        {
            HighscoreClient.Instance.FetchHighscores(OnHighscoresReceived);
        }
    }

    // Callback: Highscores empfangen und anzeigen
    private void OnHighscoresReceived(HighscoreClient.HighscoreEntry[] scores)
    {
        if (highscorePanel != null)
            highscorePanel.SetActive(true);

        if (highscoreListText == null)
            return;

        if (scores == null || scores.Length == 0)
        {
            highscoreListText.text = "No highscores yet!";
        }
        else
        {
            // Top 10 anzeigen
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < scores.Length && i < 10; i++)
            {
                sb.AppendLine($"{i + 1}. {scores[i].player_name} - {scores[i].score}");
            }
            highscoreListText.text = sb.ToString();
        }

        // Zurück-Button anzeigen
        if (backToMenuButton != null)
            backToMenuButton.gameObject.SetActive(true);
    }

    // Highscore-Panel verstecken
    public void HideHighscores()
    {
        if (highscorePanel != null)
            highscorePanel.SetActive(false);

        if (backToMenuButton != null)
            backToMenuButton.gameObject.SetActive(false);
    }

    // Button-Event: Zurück ins Menü (lädt Szene)
    public void OnBackButtonClicked()
    {
        if (FishNetSceneLoader.Instance == null)
            return;

        // Zielszene setzen und laden
        FishNetSceneLoader.Instance.sceneName = targetSceneName;
        FishNetSceneLoader.Instance.LoadSceneButton();

        HideHighscores();
    }
}
