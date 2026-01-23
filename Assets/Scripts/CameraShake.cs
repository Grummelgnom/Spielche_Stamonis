using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    // Globale, statische Instanz, damit jede Klasse einfach auf das Kamerawackeln zugreifen kann
    public static CameraShake Instance { get; private set; }

    private void Awake()
    {
        // Singleton-Setup: Wenn noch keine Instanz existiert, diese setzen
        if (Instance == null)
            Instance = this;
        else
            // Falls bereits eine Instanz existiert, dieses Objekt zerstören, um Duplikate zu vermeiden
            Destroy(gameObject);
    }

    // Öffentliche Methode, um von außen das Kamerawackeln zu starten
    // duration = Wie lange das Wackeln dauert
    // magnitude = Wie stark die Kamera wackelt
    public void Shake(float duration = 0.2f, float magnitude = 0.15f)
    {
        // Coroutine starten, die das eigentliche Wackeln ausführt
        StartCoroutine(ShakeCoroutine(duration, magnitude));
    }

    // Coroutine, die Frame für Frame die Kameraposition zufällig verändert
    private IEnumerator ShakeCoroutine(float duration, float magnitude)
    {
        // Ursprüngliche lokale Position der Kamera speichern
        Vector3 originalPosition = transform.localPosition;
        float elapsed = 0f; // Zeit, die seit Start des Wackelns vergangen ist

        // Solange die vergangene Zeit kleiner als die gewünschte Dauer ist, weiter wackeln
        while (elapsed < duration)
        {
            // Zufällige Verschiebung in X- und Y-Richtung innerhalb des angegebenen Bereichs
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            // Neue lokale Position setzen (ursprüngliche Position + zufällige Verschiebung)
            transform.localPosition = originalPosition + new Vector3(x, y, 0f);

            // Zeit um die Dauer des letzten Frames erhöhen
            elapsed += Time.deltaTime;
            // Bis zum nächsten Frame warten
            yield return null;
        }

        // Nach dem Wackeln die Kamera wieder exakt auf ihre ursprüngliche Position setzen
        transform.localPosition = originalPosition;
    }
}
