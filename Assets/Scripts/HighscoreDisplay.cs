using UnityEngine;
using TMPro;
using System.Text;

public class HighscoreDisplay : MonoBehaviour
{
    [SerializeField] private GameObject highscorePanel;
    [SerializeField] private TextMeshProUGUI highscoreListText;

    private void Start()
    {
        // Panel ist am Start versteckt
        if (highscorePanel != null)
            highscorePanel.SetActive(false);
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
            return;
        }

        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < scores.Length && i < 10; i++)
        {
            sb.AppendLine($"{i + 1}. {scores[i].player_name} - {scores[i].score}");
        }

        highscoreListText.text = sb.ToString();
        Debug.Log($"Displayed {scores.Length} highscores");
    }

    public void HideHighscores()
    {
        if (highscorePanel != null)
            highscorePanel.SetActive(false);
    }
}
