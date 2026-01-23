using FishNet.Managing; // Add this to access NetworkManager
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartSceneRedirect : MonoBehaviour
{
    [Header("Szene, die im normalen Editor-Play zuerst geladen werden soll")]
    [SerializeField] private string editorStartScene = "MainMenu";  // Im Inspector änderbar

    private void Awake()
    {
        // Sofort prüfen
        if (!FishNet.InstanceFinder.NetworkManager.IsOnline)
        {
            Debug.Log($"[StartRedirect] Kein Netzwerk aktiv (Editor-Play) → Wechsel zu: {editorStartScene}");
            SceneManager.LoadScene(editorStartScene, LoadSceneMode.Single);
        }
        else
        {
            Debug.Log("[StartRedirect] Netzwerk aktiv (Server/Client/Host) → bleibe in aktueller Szene");
        }
    }
}