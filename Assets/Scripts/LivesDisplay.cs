using UnityEngine;
using UnityEngine.UI;

public class LivesDisplay : MonoBehaviour
{
    [SerializeField]
    [Tooltip("The UI Button prefab for the life indicator")]
    private Button lifeButtonPrefab;

    [SerializeField]
    [Tooltip("Spacing between life indicators in pixels")]
    private float spacing = 60f;

    [SerializeField]
    [Tooltip("Rotation speed of life indicators (degrees per second)")]
    private float rotationSpeed = 90f;

    private Button[] lifeIndicators = new Button[3];
    private int currentLives = 3;

    void Start()
    {
        if (lifeButtonPrefab == null)
        {
            Debug.LogError("Life Button Prefab is not assigned!");
            return;
        }

        // Spawn three life indicator buttons
        for (int i = 0; i < 3; i++)
        {
            // Instantiate UI Button as child of this GameObject (Canvas)
            Button lifeButton = Instantiate(lifeButtonPrefab, transform);
            RectTransform rect = lifeButton.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(spacing * i, 0f); // Horizontal spacing
            lifeButton.interactable = false; // Disable interactivity
            lifeIndicators[i] = lifeButton;
        }
    }

    void Update()
    {
        // Rotate each active life indicator
        for (int i = 0; i < currentLives; i++)
        {
            if (lifeIndicators[i] != null)
            {
                lifeIndicators[i].GetComponent<RectTransform>().Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
            }
        }
    }

    public void RemoveLife()
    {
        if (currentLives > 0)
        {
            currentLives--;
            if (lifeIndicators[currentLives] != null)
            {
                lifeIndicators[currentLives].gameObject.SetActive(false);
                lifeIndicators[currentLives] = null;
            }

            if (currentLives <= 0)
            {
                // Start game over animation
                Debug.Log("Game Over: No lives remaining!");
                GameObject spaceship = GameObject.FindWithTag("Spaceship");
                if (spaceship != null)
                {
                    SpaceshipGameOver gameOver = spaceship.GetComponent<SpaceshipGameOver>();
                    if (gameOver != null)
                    {
                        gameOver.StartGameOver();
                    }
                    else
                    {
                        Debug.LogWarning("SpaceshipGameOver script not found on Spaceship!");
                        Destroy(spaceship);
                    }
                }
                else
                {
                    Debug.LogWarning("Spaceship with tag 'Spaceship' not found!");
                }
            }
        }
    }
}