using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbySwitch : MonoBehaviour
{
    [Header("Buttons (Drag hier rein)")]
    public Button LobbyButton;

    void Start()
    {
        // Listener für die Buttons setzen
        if (LobbyButton != null)
            LobbyButton.onClick.AddListener(NewGame);
    }

    // "New Game" / Restart: Einfach die Game-Scene laden
    public void NewGame()
    {
        SceneManager.LoadScene("Lobby");  // Ersetze durch den genauen Namen deiner Startszene!
        // Alternativ mit Index: SceneManager.LoadScene(1);
    }
}