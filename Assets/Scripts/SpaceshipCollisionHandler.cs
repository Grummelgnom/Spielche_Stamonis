using UnityEngine;
using System.Collections;

public class SpaceshipCollisionHandler : MonoBehaviour
{
    [SerializeField]
    [Tooltip("The renderer of the spaceship for flashing effect")]
    private Renderer spaceshipRenderer;

    [SerializeField]
    [Tooltip("Duration of the flash effect in seconds")]
    private float flashDuration = 0.2f;

    [SerializeField]
    [Tooltip("Duration of the shake effect in seconds")]
    private float shakeDuration = 0.3f;

    [SerializeField]
    [Tooltip("Magnitude of the shake effect")]
    private float shakeMagnitude = 0.1f;

    [SerializeField]
    [Tooltip("The lives display script")]
    private LivesDisplay livesDisplay;

    private Color originalColor;

    void Start()
    {
        if (spaceshipRenderer == null)
        {
            spaceshipRenderer = GetComponent<Renderer>();
            if (spaceshipRenderer == null)
            {
                Debug.LogError("Spaceship Renderer is not assigned and could not be found!");
            }
        }
        originalColor = spaceshipRenderer != null ? spaceshipRenderer.material.color : Color.white;

        if (livesDisplay == null)
        {
            livesDisplay = FindObjectOfType<LivesDisplay>();
            if (livesDisplay == null)
            {
                Debug.LogError("LivesDisplay script not found in the scene!");
            }
        }
    }

    public void HandleCubeCollision()
    {
        // Notify LivesDisplay to remove a life
        if (livesDisplay != null)
        {
            livesDisplay.RemoveLife();
        }
        else
        {
            Debug.LogWarning("LivesDisplay is not assigned!");
        }

        // Start shake and flash effects
        StartCoroutine(Shake());
        StartCoroutine(Flash());
    }

    private IEnumerator Shake()
    {
        Vector3 originalPosition = transform.position;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            float x = Random.Range(-1f, 1f) * shakeMagnitude;
            float y = Random.Range(-1f, 1f) * shakeMagnitude;
            transform.position = originalPosition + new Vector3(x, y, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = originalPosition;
    }

    private IEnumerator Flash()
    {
        if (spaceshipRenderer == null) yield break;

        spaceshipRenderer.material.color = Color.red;
        yield return new WaitForSeconds(flashDuration);
        spaceshipRenderer.material.color = originalColor;
    }
}