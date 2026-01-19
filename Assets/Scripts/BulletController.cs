using FishNet.Object;
using UnityEngine;

public class BulletController : NetworkBehaviour
{
    [SerializeField] private float bulletSpeed = 20f;
    private Rigidbody2D rb;
    private Vector3 shootDirection = Vector3.up;

    public void InitializeBullet(Vector3 direction)
    {
        shootDirection = direction.normalized;
        Debug.Log($"Bullet initialized with direction: {shootDirection}");
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        Debug.Log($"BulletController Start - Setting velocity");
        Debug.Log($"Direction: {shootDirection}");
        Debug.Log($"Speed: {bulletSpeed}");

        // Setze Velocity
        rb.linearVelocity = shootDirection * bulletSpeed;

        Debug.Log($"Velocity set to: {rb.linearVelocity}");

        // Zerstöre Bullet nach 5 Sekunden falls nichts getroffen
        Destroy(gameObject, 5f);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log($"Bullet hit: {collision.gameObject.name}");
        if (IsServer)
        {
            Destroy(gameObject);
        }
    }
}
