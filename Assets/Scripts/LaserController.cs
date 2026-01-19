using UnityEngine;

public class LaserController : MonoBehaviour
{
    public float speed = 20f;  // Geschwindigkeit des Lasers
    public float lifetime = 5f;  // Zeit bis zur Auto-Zerst�rung
    public string targetTag = "Asteroid";  // Tag der Zielobjekte
    public AudioClip destroySound;  // MP3 oder WAV f�r Asteroiden-Zerst�rung

    private AudioSource audioSource;  // F�r optionalen Laser-Schuss-Sound

    void Start()
    {
        // Auto-Zerst�rung
        Destroy(gameObject, lifetime);

        // Geschwindigkeit setzen
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = transform.forward * speed;
        }

        // Optional: Laser-Schuss-Sound abspielen
        audioSource = GetComponent<AudioSource>();
        if (audioSource != null && audioSource.clip != null)
        {
            audioSource.Play();
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Pr�fen, ob das getroffene Objekt den richtigen Tag hat
        if (collision.gameObject.CompareTag(targetTag))
        {
            // Zerst�rungs-Sound abspielen
            if (destroySound != null)
            {
                // Tempor�res GameObject f�r den Sound
                GameObject soundObject = new GameObject("TempAudio");
                soundObject.transform.position = collision.transform.position; // Sound an der Kollisionsstelle
                AudioSource tempAudio = soundObject.AddComponent<AudioSource>();
                tempAudio.clip = destroySound;
                tempAudio.spatialBlend = 1f; // 3D-Sound
                tempAudio.Play();
                // Sound-Objekt nach Abspielen zerst�ren
                Destroy(soundObject, destroySound.length);
            }

            // Asteroid und Laser zerst�ren
            Destroy(collision.gameObject);
            Destroy(gameObject);
            Debug.Log($"{targetTag} und Laser zerst�rt!");
        }
    }
}