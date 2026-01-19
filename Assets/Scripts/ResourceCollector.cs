    //using UnityEngine;
    //using UnityEngine.UI;

    //public class ResourceCollector : MonoBehaviour
    //{
    //    [Header("Progress Bar Einstellungen")]
    //    [SerializeField] private Image progressBar; // Ziehe das "ProgressBarFill"-Image hierher
    //    [SerializeField] private int maxResources = 10; // Maximum im Inspector einstellbar

    //    [Header("Aktuelle Ressourcen")]
    //    [SerializeField] private int collectedResources = 0; // Aktueller Zähler (kann debuggt werden)

    //    private RectTransform progressRect; // Für Skalierung
    //    private float maxWidth; // Maximale Breite des Images

    //    private void Start()
    //    {
    //        // Initialisiere RectTransform und maximale Breite
    //        if (progressBar != null)
    //        {
    //            progressRect = progressBar.GetComponent<RectTransform>();
    //            maxWidth = progressRect.sizeDelta.x; // Speichere die anfängliche Breite als Maximum
    //            UpdateProgressBar(); // Starte mit 0
    //        }
    //    }

    //    private void OnTriggerEnter(Collider other)
    //    {
    //        string[] asteroidNames = {
    //            "Stein_1_blau", "Stein_2_blau", "Stein_3_blau", "Stein_4_blau", "Stein_5_blau",
    //            "Stein_1_grün", "Stein_2_grün", "Stein_3_grün", "Stein_4_grün", "Stein_5_grün"
    //        };

    //        string asteroidName = other.gameObject.name.Replace("(Clone)", "").Trim();
    //        foreach (string name in asteroidNames)
    //        {
    //            if (asteroidName == name)
    //            {
    //                collectedResources++;
    //                collectedResources = Mathf.Clamp(collectedResources, 0, maxResources);
    //                UpdateProgressBar();
    //                Destroy(other.gameObject);

    //                RessourceSpawner spawner = FindObjectOfType<RessourceSpawner>();
    //                if (spawner != null)
    //                {
    //                    spawner.RemoveAsteroid(other.gameObject);
    //                }

    //                Debug.Log($"Collected: {name}, Total: {collectedResources}/{maxResources}, Progress Bar Assigned: {(progressBar != null)}");
    //                break;
    //            }
    //        }
    //    }

    //    private void UpdateProgressBar()
    //    {
    //        if (progressBar != null && progressRect != null)
    //        {
    //            float currentWidth = (float)collectedResources / maxResources * maxWidth;
    //            Vector2 newSize = new Vector2(currentWidth, progressRect.sizeDelta.y); // Horizontale Skalierung der Breite
    //            progressRect.sizeDelta = newSize;
    //            Debug.Log($"Updating Progress Bar width to: {currentWidth}");
    //        }
    //        else
    //        {
    //            Debug.LogWarning("Progress Bar or RectTransform is not assigned!");
    //        }
    //    }
    //}