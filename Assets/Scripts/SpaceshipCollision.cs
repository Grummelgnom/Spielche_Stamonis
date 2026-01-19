using UnityEngine;

public class SpaceshipCollision : MonoBehaviour
{
    [Header("Kamera Shake Einstellungen")]
    [SerializeField] private float collisionShakeDuration = 0.5f; // Dauer des Wackelns
    [SerializeField] private float CameraShake = 0.5f; // Kamera
    [SerializeField] private float collisionShakeMagnitude = 0.3f; // Intensität
    [SerializeField] private float collisionShakeFrequency = 25f; // Frequenz

    private PowerupCollector powerupCollector; // Referenz zum PowerupCollector

    private void Start()
    {
        //// Finde die Kamera mit CameraShake, falls nicht zugewiesen
        //if (cameraShake == null)
        //{
        //    cameraShake = Camera.main.GetComponent<CameraShake>();
        //    if (cameraShake == null)
        //    {
        //        Debug.LogError("CameraShake-Skript nicht gefunden! Bitte an die Hauptkamera anhängen.");
        //    }
        //}

        // Finde das PowerupCollector-Skript am gleichen GameObject
        powerupCollector = GetComponent<PowerupCollector>();
        if (powerupCollector == null)
        {
            Debug.LogError("PowerupCollector-Skript nicht gefunden! Bitte sicherstellen, dass es am gleichen GameObject ist.");
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Prüfe, ob Kollision mit einem Asteroiden (Tag: Cube) und ob nicht immun
        if (collision.gameObject.CompareTag("Cube") && powerupCollector != null && !powerupCollector.IsImmune())
        {
            Debug.Log($"Spaceship collided with Asteroid: {collision.gameObject.name}");
            // Starte Kamerawackeln
            //if (cameraShake != null)
            //{
            //    cameraShake.StartShake(collisionShakeDuration, collisionShakeMagnitude, collisionShakeFrequency);
            //}
            // Hier könntest du Schaden anwenden (z. B. Leben reduzieren), falls gewünscht
        }
        else if (powerupCollector != null && powerupCollector.IsImmune())
        {
            Debug.Log("Kollision ignoriert – Raumschiff ist immun.");
        }
    }
}