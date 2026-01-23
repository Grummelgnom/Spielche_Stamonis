using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Hänge das Skript an dein Canvas oder ein leeres GameObject in der MainMenu- oder GameOver-Scene
public class ControlSwitch : MonoBehaviour
{
    [Header("Buttons (Drag hier rein)")]
    public Button controlsButton;

    void Start()
    {
        // Listener für die Buttons setzen
        if (controlsButton != null)
            controlsButton.onClick.AddListener(NewGame);
    }

    public void NewGame()
    {
        SceneManager.LoadScene("Controls");  // Control Szene wird geladen
    }
}