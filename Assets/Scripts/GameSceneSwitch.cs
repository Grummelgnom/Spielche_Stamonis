using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameSceneSwitch : MonoBehaviour
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
        SceneManager.LoadScene("GameSzene");  // Laden der Game-Szene
    }
}