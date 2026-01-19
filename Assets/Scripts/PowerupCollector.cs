using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class PowerupCollector : MonoBehaviour
{
    public GameObject originalPanel; // Verknüpfe dein ursprüngliches Panel hier (aus dem Canvas)
    public AudioSource audioSource;  // Verknüpfe den AudioSource hier
    public AudioClip powerupSound;   // Verknüpfe die Audiodatei hier
    public GameObject targetObject;  // Verknüpfe das Schwarze Loch hier
    public SpaceshipMovement spaceshipMovement; // Verknüpfe das Bewegungsskript hier
    public GameObject asteroidSpawner; // Verknüpfe das AsteroidSpawner-GameObject hier
    public GameObject warpJumpEnergySpawner; // Verknüpfe das WarpJumpEnergySpawner-GameObject hier
    [SerializeField] private float spacing = 80.0f;   // Abstand in Pixeln, im Inspector editierbar
    [SerializeField] private float initialSpeed = 5.0f; // Startgeschwindigkeit, einstellbar im Inspector
    [SerializeField] private float accelerationRate = 2.0f; // Beschleunigungsrate pro Sekunde, einstellbar im Inspector
    [SerializeField] private float rotationSpeed = 1.0f; // Drehgeschwindigkeit, einstellbar im Inspector
    private int energyCount = 0;
    private const int maxEnergy = 3; // Max 1 Symbole (für Test)
    private bool isCollecting = false; // Verhindert doppelte Trigger
    private bool isTransitioning = false; // Flag für die Übergangsanimation
    public bool isImmune = false; // Öffentliches Flag für Immunität

    void Start()
    {
        if (originalPanel != null)
        {
            originalPanel.SetActive(false);
        }
        if (spaceshipMovement == null)
        {
            spaceshipMovement = GetComponent<SpaceshipMovement>();
            if (spaceshipMovement == null)
            {
                Debug.LogError("SpaceshipMovement-Skript nicht gefunden!");
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("Trigger erkannt mit: " + other.name);

        if (isImmune) return; // Ignoriere alle Kollisionen, wenn immun

        if (other.CompareTag("Shield_Powerup") && !isCollecting)
        {
            Debug.Log("Shield_Powerup eingesammelt! Aktuelle Energy-Count: " + energyCount);
            StartCoroutine(CollectPowerup(other.gameObject));
        }
        // Hier könnten Asteroiden-Schäden normalerweise stattfinden, aber sie werden durch isImmune blockiert
    }

    private IEnumerator CollectPowerup(GameObject powerup)
    {
        isCollecting = true;

        if (energyCount < maxEnergy)
        {
            Destroy(powerup);
            Debug.Log("Powerup zerstört.");

            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("Kein Canvas gefunden!");
                yield break;
            }

            GameObject newPanel = Instantiate(originalPanel, canvas.transform);
            newPanel.name = "PowerUpPanel_Copy_" + energyCount;
            newPanel.SetActive(true);

            RectTransform rectTransform = newPanel.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                RectTransform originalRect = originalPanel.GetComponent<RectTransform>();
                rectTransform.anchorMax = originalRect.anchorMax;
                rectTransform.anchorMin = originalRect.anchorMin;
                rectTransform.pivot = originalRect.pivot;
                rectTransform.sizeDelta = originalRect.sizeDelta;
                rectTransform.anchoredPosition = originalRect.anchoredPosition + new Vector2(-spacing * energyCount, 0);
                Debug.Log("Neues Panel erstellt bei Position: " + rectTransform.anchoredPosition);
            }
            else
            {
                Debug.LogError("Kein RectTransform am neuen Panel!");
            }

            if (audioSource != null && powerupSound != null)
            {
                audioSource.PlayOneShot(powerupSound);
                Debug.Log("Powerup-Sound abgespielt.");
            }
            else
            {
                Debug.LogError("AudioSource oder Powerup-Sound nicht verknüpft!");
            }

            energyCount++;
            Debug.Log("Neue Energy-Count: " + energyCount);

            if (energyCount >= maxEnergy && targetObject != null)
            {
                StartCoroutine(StartTransition());
            }
        }

        yield return new WaitForSeconds(0.1f);
        isCollecting = false;
    }

    private IEnumerator StartTransition()
    {
        isTransitioning = true;
        isImmune = true; // Aktiviere Immunität während der Übergangsphase
        Debug.Log("Max Energy erreicht – Übergang startet mit Immunität.");

        // Deaktiviere Spawner
        if (asteroidSpawner != null)
        {
            asteroidSpawner.SetActive(false);
            Debug.Log("AsteroidSpawner deaktiviert.");
        }
        if (warpJumpEnergySpawner != null)
        {
            warpJumpEnergySpawner.SetActive(false);
            Debug.Log("WarpJumpEnergySpawner deaktiviert.");
        }

        if (spaceshipMovement != null)
        {
            spaceshipMovement.SetControllable(false);
            Debug.Log("Steuerung deaktiviert.");
        }

        Transform spaceshipTransform = transform; // Nutze this.transform, da das Skript am Raumschiff hängt
        Transform targetTransform = targetObject != null ? targetObject.transform : null;

        if (targetTransform == null)
        {
            Debug.LogError("TargetObject nicht verknüpft – Übergang abgebrochen!");
            yield break;
        }

        Debug.Log("Raumschiff-Position: " + spaceshipTransform.position + ", Ziel-Position: " + targetTransform.position);

        // Phase 1: Ausrichtung zum Schwarzen Loch mit 90 Grad Offset
        Quaternion targetRotation = Quaternion.LookRotation(targetTransform.position - spaceshipTransform.position) * Quaternion.Euler(0, 90, 0);
        while (Quaternion.Angle(spaceshipTransform.rotation, targetRotation) > 1f) // Toleranz von 1 Grad
        {
            spaceshipTransform.rotation = Quaternion.RotateTowards(spaceshipTransform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            Debug.Log("Ausrichtung: Aktueller Winkel: " + Quaternion.Angle(spaceshipTransform.rotation, targetRotation));
            yield return null;
        }

        // Phase 2: Flug mit Beschleunigung
        float currentSpeed = initialSpeed; // Starte mit der initialen Geschwindigkeit
        while (Vector3.Distance(spaceshipTransform.position, targetTransform.position) > 0.1f)
        {
            currentSpeed += accelerationRate * Time.deltaTime;
            spaceshipTransform.position = Vector3.MoveTowards(spaceshipTransform.position, targetTransform.position, currentSpeed * Time.deltaTime);
            Debug.Log("Bewegung: Aktuelle Geschwindigkeit: " + currentSpeed + ", Distanz: " + Vector3.Distance(spaceshipTransform.position, targetTransform.position));
            yield return null;
        }

        Debug.Log("Raumschiff am Ziel angekommen – Reset starten.");
        ResetScene();
    }

    private void ResetScene()
    {
        isImmune = false; // Deaktiviere Immunität nach Übergang
        Destroy(gameObject); // Zerstöre das Raumschiff

        Debug.Log("Lade Szene: world");
        SceneManager.LoadScene("world");
    }

    // Öffentliche Methode zur Prüfung der Immunität (für andere Skripte)
    public bool IsImmune()
    {
        return isImmune;
    }
}