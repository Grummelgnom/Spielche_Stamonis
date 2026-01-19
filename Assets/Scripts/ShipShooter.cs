using UnityEngine;

public class ShipShooter : MonoBehaviour
{
    public GameObject laserPrefab;
    public float fireRate = 0.5f;
    public Transform firePoint;
    private AudioSource audioSource;  // Für Schuss-Sound
    public AudioClip laserSound;  // Ziehe WAV hier rein

    private float nextFireTime = 0f;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.Space) && Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + fireRate;
        }
    }

    void Shoot()
    {
        Vector3 spawnPos = firePoint ? firePoint.position : transform.position;
        Quaternion spawnRot = firePoint ? firePoint.rotation : transform.rotation;
        Instantiate(laserPrefab, spawnPos, spawnRot);

        // Schuss-Sound abspielen
        if (audioSource != null && laserSound != null)
        {
            audioSource.PlayOneShot(laserSound);
        }
    }
}