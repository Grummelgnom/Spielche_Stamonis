using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Hänge das Skript an dein Canvas oder ein leeres GameObject in der MainMenu- oder GameOver-Scene
public class MenuManager : MonoBehaviour
{
    [Header("Buttons (Drag hier rein)")]
    public Button newGameButton;

    void Start()
    {
        // Listener für die Buttons setzen
        if (newGameButton != null)
            newGameButton.onClick.AddListener(NewGame);
    }

    // "New Game" / Restart: Einfach die Game-Scene laden
    public void NewGame()
    {
        SceneManager.LoadScene("GameSzene");  // Ersetze durch den genauen Namen deiner Spiel-Scene!
        // Alternativ mit Index: SceneManager.LoadScene(1);
    }
}