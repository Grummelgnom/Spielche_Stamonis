using UnityEngine;

public class ExitGame : MonoBehaviour
{
    // Diese Funktion wird im Button-Inspector verknüpft
    public void QuitGame()
    {
#if UNITY_EDITOR
        // Stop Playmode im Unity Editor
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // Schließt die Anwendung im Build (EXE, APK, etc.)
        Application.Quit();
#endif
        Debug.Log("Exit Game button clicked!");

        // Funktioniert im Editor nicht sichtbar, nur im Build
        Application.Quit();
    }
}
