using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Hänge das Skript an dein Canvas oder ein leeres GameObject in der MainMenu- oder GameOver-Scene
public class ControlSwitch : MonoBehaviour
{
    [Header("Buttons (Drag hier rein)")]
    public Button controlButton;

    void Start()
    {
        // Listener für die Buttons setzen
        if (controlButton != null)
            controlButton.onClick.AddListener(NewGame);
    }

    // "New Game" / Restart: Einfach die Game-Scene laden
    public void NewGame()
    {
        SceneManager.LoadScene("Controls");  // Control Szene wird geladen
    }
}