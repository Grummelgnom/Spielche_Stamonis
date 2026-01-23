using FishNet.Managing;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartSceneRedirect : MonoBehaviour
{
    [Header("Szene für Editor-Play (kein Netzwerk)")]
    [SerializeField] private string editorStartScene = "MainMenu";  // Zielszene ohne Netzwerk

    private void Awake()
    {
        // Prüft, ob Netzwerk läuft → leitet zu Editor-Startszene um (Single-Player-Test)
        if (!FishNet.InstanceFinder.NetworkManager.IsOnline)
        {
            SceneManager.LoadScene(editorStartScene, LoadSceneMode.Single);
        }
    }
}
