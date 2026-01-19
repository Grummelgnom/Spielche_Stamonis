using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SpaceshipGameOver : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Duration of the game over animation in seconds")]
    private float animationDuration = 2f;

    [SerializeField]
    [Tooltip("Speed of rotation during tumbling (degrees per second)")]
    private float tumbleSpeed = 180f;

    [SerializeField]
    [Tooltip("Speed of movement away from the camera (units per second)")]
    private float moveAwaySpeed = 20f;

    [SerializeField]
    [Tooltip("Scale reduction factor at the end of the animation")]
    private float finalScaleFactor = 0.1f;

    private bool isGameOver = false;

    public void StartGameOver()
    {
        if (!isGameOver)
        {
            isGameOver = true;
            StartCoroutine(GameOverAnimation());
        }
    }

    private IEnumerator GameOverAnimation()
    {
        // Disable Rigidbody physics to control movement manually
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true; // Prevent physics interference
        }

        // Store initial values
        Vector3 initialScale = transform.localScale;
        Renderer renderer = GetComponent<Renderer>();
        Color initialColor = renderer != null ? renderer.material.color : Color.white;
        float elapsed = 0f;

        // Generate random tumble rotation axes
        Vector3 tumbleAxis = Random.insideUnitSphere.normalized;

        while (elapsed < animationDuration)
        {
            // Tumble: Rotate around a random axis
            transform.Rotate(tumbleAxis * tumbleSpeed * Time.deltaTime, Space.World);

            // Move away along negative forward direction
            transform.Translate(-transform.forward * moveAwaySpeed * Time.deltaTime, Space.World);

            // Scale down gradually
            float t = elapsed / animationDuration;
            transform.localScale = Vector3.Lerp(initialScale, initialScale * finalScaleFactor, t);

            // Fade out gradually
            if (renderer != null)
            {
                Color color = renderer.material.color;
                color.a = Mathf.Lerp(1f, 0f, t);
                renderer.material.color = color;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Reload the scene to restart the game
        Debug.Log("Game Over: Reloading scene to restart game!");
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}