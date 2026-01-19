using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class SpaceflightPortal : MonoBehaviour
{
    public string spaceflightSceneName = "Spaceflight"; // Name der Spaceflight-Szene
    public Image fadeImage; // Schwarzes UI-Image zum Überblenden
    public float fadeDuration = 1f; // Dauer der Überblendung in Sekunden

    private void OnTriggerEnter(Collider other)
    {
        // Prüfen, ob der Player ins Trigger-Objekt läuft
        if (other.CompareTag("Player"))
        {
            StartCoroutine(TeleportToSpaceflight());
        }
    }

    private IEnumerator TeleportToSpaceflight()
    {
        // Starte Fade-Out (Bild wird schwarz)
        yield return StartCoroutine(Fade(1));

        // Szene wechseln
        SceneManager.LoadScene(spaceflightSceneName);

        // Optional: Nach dem Laden ein Fade-In machen
        yield return StartCoroutine(Fade(0));
    }

    private IEnumerator Fade(float targetAlpha)
    {
        if (fadeImage == null)
            yield break;

        Color color = fadeImage.color;
        float startAlpha = color.a;
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            color.a = Mathf.Lerp(startAlpha, targetAlpha, timer / fadeDuration);
            fadeImage.color = color;
            yield return null;
        }

        // Sicherstellen, dass der Endwert erreicht wird
        color.a = targetAlpha;
        fadeImage.color = color;
    }
}
