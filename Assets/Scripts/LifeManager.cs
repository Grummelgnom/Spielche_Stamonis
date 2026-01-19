using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class LifeManager : MonoBehaviour
{
    public GameObject[] lifePanels; // Verknüpfe die 3 Life-Panels ("Life_0", "Life_1", "Life_2") hier
    public GameObject explosionPrefab; // Verknüpfe dein Explosions-Prefab hier
    public AudioClip explosionSound; // Optional: Verknüpfe einen Explosions-Sound
    private AudioSource audioSource; // Für den Sound
    private int lives = 3;

    void Start()
    {
        // Überprüfe und initialisiere die Panels
        if (lifePanels.Length != 3)
        {
            Debug.LogError("Genau 3 Life-Panels müssen verknüpft sein! Aktuelle Anzahl: " + lifePanels.Length);
            return;
        }

        // Stelle sicher, dass alle Panels aktiv sind
        for (int i = 0; i < lives; i++)
        {
            if (lifePanels[i] != null)
            {
                lifePanels[i].SetActive(true); // Aktiviere alle 3 Panels
                Debug.Log("Lebens-Panel " + i + " aktiviert.");
            }
            else
            {
                Debug.LogError("Life-Panel " + i + " ist nicht verknüpft!");
            }
        }

        // Hole den AudioSource (falls vorhanden)
        audioSource = GetComponent<AudioSource>();
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("Kollision erkannt mit: " + other.name);

        if (other.CompareTag("Cube"))
        {
            Debug.Log("Asteroid-Kollision! Leben wird reduziert.");
            ReduceLife();
        }
    }

    public void ReduceLife()
    {
        if (lives > 0)
        {
            lives--;
            Debug.Log("Verbleibende Leben: " + lives);

            if (lifePanels[lives] != null)
            {
                lifePanels[lives].SetActive(false); // Deaktiviere das entsprechende Panel
                Debug.Log("Panel für Leben " + lives + " deaktiviert.");
            }

            if (lives <= 0)
            {
                StartCoroutine(ExplosionAndReset()); // Starte die Explosion und den Reset
            }
        }
    }

    private IEnumerator ExplosionAndReset()
    {
        // Instanziiere die Explosion am Raumschiff-Position
        if (explosionPrefab != null)
        {
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            Debug.Log("Explosion instanziiert.");
        }
        else
        {
            Debug.LogError("Explosion-Prefab nicht verknüpft!");
        }

        // Deaktiviere das Raumschiff sofort
        gameObject.SetActive(false);
        Debug.Log("Raumschiff deaktiviert.");

        // Optional: Spiele den Explosions-Sound ab
        if (audioSource != null && explosionSound != null)
        {
            audioSource.PlayOneShot(explosionSound);
            Debug.Log("Explosions-Sound abgespielt.");
        }

        // Warte auf die Dauer der Explosion (z. B. 2 Sekunden – passe an deine Effekt-Dauer an)
        yield return new WaitForSeconds(2.0f);

        // Reset der Szene
        Debug.Log("Keine Leben mehr – Szene wird zurückgesetzt!");
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}