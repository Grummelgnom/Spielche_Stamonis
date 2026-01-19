using UnityEngine;

public class SpaceshipMovementY : MonoBehaviour
{
    public float speed = 10f; // Speed of the spaceship

    void Update()
    {
        // Move the spaceship along the local Y-axis
        transform.Translate(Vector3.up * speed * Time.deltaTime);
    }
}
