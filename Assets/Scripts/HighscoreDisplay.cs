using UnityEngine;
using TMPro;
using System.Text;
using UnityEngine.UI;  // Für den Button
using FishNet.Managing.Scened;  // Für FishnetSceneLoader
public class HighscoreDisplay : MonoBehaviour
{
    [SerializeField] private GameObject highscorePanel;
    [SerializeField] private TextMeshProUGUI highscoreListText;

    // ────────────────────────────────────────────────
    // NEU: Button und Ziel-Szene (im Inspector zuweisbar)
    // ────────────────────────────────────────────────
    [Header("Zurück-Button")]
    [SerializeField] private Button backToMenuButton;

    [Header("Ziel-Szene nach Klick")]
    [SerializeField] private string targetSceneName = "MainMenu";  // Im Inspector änderbar

    private void Awake()
    {
        // Sicherstellen, dass alles anfangs unsichtbar ist
        if (highscorePanel != null)
            highscorePanel.SetActive(false);

        if (backToMenuButton != null)
        {
            backToMenuButton.gameObject.SetActive(false);

            // Listener einmalig hinzufügen
            backToMenuButton.onClick.RemoveAllListeners();
            backToMenuButton.onClick.AddListener(OnBackButtonClicked);
        }
        else
        {
            Debug.LogWarning("BackToMenuButton nicht im Inspector zugewiesen!");
        }
    }

    public void ShowHighscores()
    {
        if (HighscoreClient.Instance != null)
        {
            HighscoreClient.Instance.FetchHighscores(OnHighscoresReceived);
        }
        else
        {
            Debug.LogError("HighscoreClient.Instance is NULL!");
        }
    }

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
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < scores.Length && i < 10; i++)
            {
                sb.AppendLine($"{i + 1}. {scores[i].player_name} - {scores[i].score}");
            }
            highscoreListText.text = sb.ToString();
            Debug.Log($"Displayed {scores.Length} highscores");
        }

        // NEU: Button sichtbar machen, sobald Highscores geladen sind
        if (backToMenuButton != null)
            backToMenuButton.gameObject.SetActive(true);
    }

    public void HideHighscores()
    {
        if (highscorePanel != null)
            highscorePanel.SetActive(false);

        // Optional: Button wieder ausblenden
        if (backToMenuButton != null)
            backToMenuButton.gameObject.SetActive(false);
    }

    // NEU: Button-Klick → Szene laden
    public void OnBackButtonClicked()
    {
        Debug.Log("[Button] Klick erkannt – Zielszene: " + targetSceneName);

        if (FishNetSceneLoader.Instance == null)
        {
            Debug.LogError("[Button] FishNetSceneLoader.Instance ist null!");
            return;
        }

        Debug.Log("[Button] Loader gefunden – setze sceneName auf: " + targetSceneName);
        FishNetSceneLoader.Instance.sceneName = targetSceneName;

        Debug.Log("[Button] Rufe LoadSceneButton() auf");
        FishNetSceneLoader.Instance.LoadSceneButton();

        Debug.Log("[Button] Aufruf abgeschlossen – Panel wird geschlossen");
        HideHighscores();
    }
}