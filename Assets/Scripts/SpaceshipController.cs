using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SpaceshipController : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Renderer of the spaceship for flashing effect")]
    private Renderer spaceshipRenderer;

    [SerializeField]
    [Tooltip("Main Camera used during gameplay (with tag 'MainCamera')")]
    private Camera mainCamera;

    [SerializeField]
    [Tooltip("Game Over Camera used during game over animation (named 'GameOverCamera')")]
    private Camera gameOverCamera;

    [SerializeField]
    [Tooltip("Duration of the shake effect on cube collision")]
    private float shakeDuration = 0.3f;

    [SerializeField]
    [Tooltip("Magnitude of the shake effect")]
    private float shakeMagnitude = 0.1f;

    [SerializeField]
    [Tooltip("Duration of the flash effect on cube collision")]
    private float flashDuration = 0.2f;

    [SerializeField]
    [Tooltip("Duration of the game over animation in seconds")]
    private float animationDuration = 5f;

    [SerializeField]
    [Tooltip("Base speed of rotation during tumbling (degrees per second)")]
    private float tumbleSpeed = 180f;

    [SerializeField]
    [Tooltip("Speed of movement towards the target point (units per second)")]
    private float moveSpeed = 20f;

    [SerializeField]
    [Tooltip("Scale reduction factor at the end of the animation")]
    private float finalScaleFactor = 0.1f;

    [SerializeField]
    [Tooltip("The distant point to move towards (e.g., future black hole position)")]
    private Transform targetPoint;

    [SerializeField]
    [Tooltip("Position offset for the Game Over Camera (relative to spaceship start position)")]
    private Vector3 gameOverCameraOffset = new Vector3(0f, 5f, -10f);

    private Vector3 originalPosition;
    private bool isGameOver = false;

    void Start()
    {
        if (spaceshipRenderer == null)
        {
            spaceshipRenderer = GetComponent<Renderer>();
            if (spaceshipRenderer == null)
            {
                Debug.LogError("Spaceship Renderer not assigned and not found on " + gameObject.name, gameObject);
            }
            else
            {
                Debug.Log("Spaceship Renderer found: " + spaceshipRenderer.name, spaceshipRenderer);
            }
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main; // Finds camera with tag "MainCamera"
            if (mainCamera == null)
            {
                Debug.LogError("Main Camera (with tag 'MainCamera') not found in scene!", gameObject);
            }
            else
            {
                Debug.Log("Main Camera found: " + mainCamera.gameObject.name + ", enabled: " + mainCamera.enabled, mainCamera.gameObject);
            }
        }

        if (gameOverCamera == null)
        {
            GameObject gameOverCamObj = GameObject.Find("GameOverCamera");
            if (gameOverCamObj != null)
            {
                gameOverCamera = gameOverCamObj.GetComponent<Camera>();
            }
            if (gameOverCamera == null)
            {
                Debug.LogError("GameOverCamera (named 'GameOverCamera') not found in scene or missing Camera component!", gameObject);
            }
            else
            {
                gameOverCamera.enabled = false; // Ensure GameOverCamera is disabled at start
                Debug.Log("GameOverCamera found: " + gameOverCamera.gameObject.name + ", disabled: " + !gameOverCamera.enabled, gameOverCamera.gameObject);
            }
        }

        originalPosition = transform.position;
    }

    public void HandleCubeCollision()
    {
        if (!isGameOver)
        {
            StartCoroutine(ShakeAndFlash());
        }
    }

    private IEnumerator ShakeAndFlash()
    {
        float elapsed = 0f;
        Color originalColor = spaceshipRenderer != null ? spaceshipRenderer.material.color : Color.white;

        while (elapsed < shakeDuration)
        {
            float x = originalPosition.x + Random.Range(-shakeMagnitude, shakeMagnitude);
            float y = originalPosition.y + Random.Range(-shakeMagnitude, shakeMagnitude);
            float z = originalPosition.z + Random.Range(-shakeMagnitude, shakeMagnitude);

            transform.position = new Vector3(x, y, z);

            if (spaceshipRenderer != null && elapsed < flashDuration)
            {
                spaceshipRenderer.material.color = Color.red;
            }
            else if (spaceshipRenderer != null)
            {
                spaceshipRenderer.material.color = originalColor;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = originalPosition;
        if (spaceshipRenderer != null)
        {
            spaceshipRenderer.material.color = originalColor;
        }
    }

    public void StartGameOver()
    {
        if (!isGameOver)
        {
            isGameOver = true;
            Debug.Log("Starting Game Over sequence on " + gameObject.name, gameObject);

            // Disable SpaceshipMovement to prevent interference
            SpaceshipMovement movement = GetComponent<SpaceshipMovement>();
            if (movement != null)
            {
                movement.enabled = false;
                Debug.Log("SpaceshipMovement disabled for Game Over.", movement);
            }

            // Switch cameras: disable Main Camera, enable GameOverCamera
            if (mainCamera != null)
            {
                mainCamera.enabled = false;
                Debug.Log("Main Camera disabled: " + mainCamera.gameObject.name + ", enabled: " + mainCamera.enabled, mainCamera.gameObject);
            }
            else
            {
                Debug.LogWarning("Main Camera is null, cannot disable!", gameObject);
            }

            if (gameOverCamera != null)
            {
                gameOverCamera.enabled = true;
                gameOverCamera.transform.position = originalPosition + gameOverCameraOffset;
                gameOverCamera.transform.LookAt(transform.position);
                Debug.Log("GameOverCamera enabled: " + gameOverCamera.gameObject.name + ", enabled: " + gameOverCamera.enabled + ", positioned at: " + gameOverCamera.transform.position, gameOverCamera.gameObject);
            }
            else
            {
                Debug.LogWarning("GameOverCamera is null, cannot enable!", gameObject);
            }

            StartCoroutine(GameOverAnimation());
        }
    }

    private IEnumerator GameOverAnimation()
    {
        // Disable Rigidbody physics to control movement manually
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            Debug.Log("Rigidbody set to kinematic for Game Over.", rb);
        }

        if (targetPoint == null)
        {
            Debug.LogWarning("Target Point not assigned! Using default direction.", gameObject);
            targetPoint = transform;
        }

        // Store initial values
        Vector3 initialScale = transform.localScale;
        Renderer renderer = spaceshipRenderer;
        Color initialColor = renderer != null ? renderer.material.color : Color.white;
        float elapsed = 0f;

        // Generate random tumble speeds for each axis with wider variation
        Vector3 tumbleSpeeds = new Vector3(
            Random.Range(tumbleSpeed * 0.2f, tumbleSpeed * 2f),
            Random.Range(tumbleSpeed * 0.2f, tumbleSpeed * 2f),
            Random.Range(tumbleSpeed * 0.2f, tumbleSpeed * 2f)
        );

        while (elapsed < animationDuration)
        {
            // Tumble: Rotate around all three axes with varied speeds
            transform.Rotate(tumbleSpeeds.x * Time.deltaTime, tumbleSpeeds.y * Time.deltaTime, tumbleSpeeds.z * Time.deltaTime, Space.World);

            // Move towards the target point
            Vector3 directionToTarget = (targetPoint.position - transform.position).normalized;
            transform.Translate(directionToTarget * moveSpeed * Time.deltaTime, Space.World);

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

            // Update GameOverCamera to follow the spaceship smoothly
            if (gameOverCamera != null)
            {
                gameOverCamera.transform.LookAt(transform.position);
                Debug.Log("GameOverCamera looking at spaceship at position: " + transform.position, gameOverCamera.gameObject);
            }

            // Update tumble speeds periodically for wilder effect
            if (elapsed % 0.5f < Time.deltaTime) // Every 0.5 seconds
            {
                tumbleSpeeds = new Vector3(
                    Random.Range(tumbleSpeed * 0.2f, tumbleSpeed * 2f),
                    Random.Range(tumbleSpeed * 0.2f, tumbleSpeed * 2f),
                    Random.Range(tumbleSpeed * 0.2f, tumbleSpeed * 2f)
                );
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Reload the scene to restart the game
        Debug.Log("Game Over: Reloading scene to restart game!", gameObject);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}