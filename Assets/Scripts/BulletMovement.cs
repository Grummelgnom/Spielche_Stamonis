using UnityEngine;

public class BulletMovement : MonoBehaviour
{
    public float speed = 10f;

    void Update()
    {
        // Bewegt das Bullet nach unten (negative Y-Richtung)
        transform.Translate(Vector2.down * speed * Time.deltaTime);
    }

    // Optional: Bullet zerstören, wenn es den Bildschirm verlässt
    private void OnBecameInvisible()
    {
        Destroy(gameObject);
    }
}
