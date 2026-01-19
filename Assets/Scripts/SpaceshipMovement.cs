using UnityEngine;

public class SpaceshipMovement : MonoBehaviour
{
    [Header("Bewegungseinstellungen")]
    [SerializeField] private float speed = 10f;
    [SerializeField] private float tiltAngleHorizontal = 20f; // Visueller Tilt für Links/Rechts
    [SerializeField] private float tiltAngleVertical = 20f;   // Visueller Tilt für Hoch/Runter
    [SerializeField] private float tiltSpeed = 5f;
    [SerializeField] private float maxOffsetHorizontal = 10f; // Maximaler Links/Rechts-Versatz
    [SerializeField] private float maxOffsetVertical = 10f;   // Maximaler Hoch/Runter-Versatz

    private Quaternion initialRotation;
    private Vector3 initialPosition;
    private Vector3 lrAxis; // Feste Links/Rechts-Achse (lokale Z-Achse)
    private Vector3 udAxis; // Feste Hoch/Runter-Achse (lokale Y-Achse)
    private bool isControllable = true; // Neue Variable zur Steuerungssperre

    public void SetControllable(bool controllable)
    {
        isControllable = controllable;
    }

    private void Start()
    {
        initialRotation = transform.rotation;
        initialPosition = transform.position;
        lrAxis = (initialRotation * Vector3.forward).normalized; // Lokale Z-Achse im Weltkoordinatensystem
        udAxis = (initialRotation * Vector3.up).normalized;      // Lokale Y-Achse im Weltkoordinatensystem
    }

    private void Update()
    {
        if (!isControllable) return; // Keine Steuerung, wenn deaktiviert

        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        // Bewegung entlang der Ursprungsachsen (unabhängig von der aktuellen Neigung)
        Vector3 newPos = transform.position +
                         lrAxis * (horizontalInput * speed * Time.deltaTime) +
                         udAxis * (verticalInput * speed * Time.deltaTime);

        // Clamping entlang der LR- und UD-Achse relativ zur Startposition
        float sHorizontal = Vector3.Dot(newPos - initialPosition, lrAxis);
        sHorizontal = Mathf.Clamp(sHorizontal, -maxOffsetHorizontal, maxOffsetHorizontal);

        float sVertical = Vector3.Dot(newPos - initialPosition, udAxis);
        sVertical = Mathf.Clamp(sVertical, -maxOffsetVertical, maxOffsetVertical);

        transform.position = initialPosition + lrAxis * sHorizontal + udAxis * sVertical;

        // Visuelle Neigung: Horizontal (um X-Achse) und Vertikal (um Z-Achse), invertiert
        float targetTiltHorizontal = horizontalInput * tiltAngleHorizontal; // Invertiert: positiv bei Rechts, negativ bei Links
        float targetTiltVertical = -verticalInput * tiltAngleVertical;      // Invertiert: positiv bei Runter, negativ bei Hoch
        Quaternion targetRotation = initialRotation *
                                   Quaternion.Euler(targetTiltHorizontal, 0f, targetTiltVertical);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, tiltSpeed * Time.deltaTime);
    }
}