using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ShieldController : MonoBehaviour
{
    // Referenzen (im Inspector zuweisen)
    [SerializeField] private GameObject shield; // Schutzschild-GameObject (Sphere)
    [SerializeField] private Material shieldMaterial; // Material der Kugel f�r Flacker-Effekt
    [SerializeField] private Image[] energyBars; // Array mit 9 UI-Balken (gr�n, gelb, rot)
    [SerializeField] private GameObject explosionPrefab; // Prefab f�r Asteroiden-Explosion (Particle System)
    [SerializeField] private float drainRate = 0.5f; // Sekunden pro Energie-Balken-Verbrauch
    [SerializeField] private float rechargeRate = 1f; // Sekunden pro Energie-Balken-Aufladung
    [SerializeField] private float flickerInterval = 0.2f; // Intervall f�r Flackern (schnell)
    [SerializeField] private float flickerAlphaMin = 0.1f; // Min. Transparenz beim Flackern
    [SerializeField] private float flickerAlphaMax = 0.3f; // Max. Transparenz beim Flackern
    [SerializeField] private float destroyDelay = 0.5f; // Verz�gerung f�r Asteroiden-Zerst�rung (0 f�r instant)

    private int currentEnergy = 9; // Aktuelle Energie (0-9)
    private bool isShieldActive = false; // Ist das Schild aktiv? (Public machen, falls du es aus anderem Skript brauchst)
    private Coroutine drainCoroutine; // Referenz zur Drain-Coroutine
    private Coroutine rechargeCoroutine; // Referenz zur Recharge-Coroutine
    private Coroutine flickerCoroutine; // Referenz zur Flacker-Coroutine

    void Start()
    {
        // Initialisiere: Schild ist aus, alle Balken sichtbar
        if (shield != null)
            shield.SetActive(false);

        UpdateEnergyBars();
    }

    void Update()
    {
        // Pr�fe rechte Maustaste f�r Aktivierung
        if (Input.GetMouseButton(1) && currentEnergy > 0 && !isShieldActive)
        {
            ActivateShield();
        }
        else if (!Input.GetMouseButton(1) && isShieldActive)
        {
            DeactivateShield();
        }
    }

    // Kollision: Zerst�re Asteroid (verz�gert) und spawne Explosion (Trigger-Collider am Shield)
    private void OnTriggerEnter(Collider other)
    {
        // Pr�fe, ob es ein Asteroid ist (via Tag) und Schild aktiv
        if (isShieldActive && other.CompareTag("Cube"))
        {
            // Starte verz�gertes Destroy
            StartCoroutine(DestroyAsteroidDelayed(other.gameObject));
        }
    }

    // Coroutine: Verz�gertes Zerst�ren des Asteroiden mit Explosion
    private IEnumerator DestroyAsteroidDelayed(GameObject asteroid)
    {
        // Optional: Stoppe den Asteroiden sofort, um Kollision mit Schiff zu vermeiden (falls Velocity hoch)
        Rigidbody rb = asteroid.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero; // Stoppt Bewegung, damit er nicht weiterfliegt
        }

        // Warte die Verz�gerung (z. B. f�r Effekt)
        yield return new WaitForSeconds(destroyDelay);

        // Spawne Explosion an Asteroiden-Position
        if (explosionPrefab != null)
        {
            Instantiate(explosionPrefab, asteroid.transform.position, asteroid.transform.rotation);
        }

        // Despawn Asteroid
        Destroy(asteroid);
    }

    // Coroutine: Verbraucht Energie in Intervallen
    private IEnumerator DrainEnergy()
    {
        while (isShieldActive && currentEnergy > 0)
        {
            yield return new WaitForSeconds(drainRate);
            currentEnergy--;
            UpdateEnergyBars();

            // Flackern starten, wenn Energie niedrig
            if (currentEnergy <= 3 && flickerCoroutine == null && isShieldActive)
            {
                flickerCoroutine = StartCoroutine(FlickerShield());
            }
        }
        drainCoroutine = null;
    }

    // Coroutine: L�dt Energie in Intervallen auf
    private IEnumerator RechargeEnergy()
    {
        while (currentEnergy < 9 && !isShieldActive)
        {
            yield return new WaitForSeconds(rechargeRate);
            currentEnergy++;
            UpdateEnergyBars();
        }
        rechargeCoroutine = null;
    }

    // Coroutine: Flackert das Schild bei niedriger Energie
    private IEnumerator FlickerShield()
    {
        while (isShieldActive && currentEnergy <= 3 && shieldMaterial != null)
        {
            // Wechsel zwischen min und max Alpha
            Color color = shieldMaterial.color;
            color.a = flickerAlphaMin;
            shieldMaterial.color = color;
            yield return new WaitForSeconds(flickerInterval);

            color.a = flickerAlphaMax;
            shieldMaterial.color = color;
            yield return new WaitForSeconds(flickerInterval);
        }
        flickerCoroutine = null;
    }

    // Setzt das Schild-Material auf Standard-Alpha zur�ck
    private void ResetShieldMaterial()
    {
        if (shieldMaterial != null)
        {
            Color color = shieldMaterial.color;
            color.a = flickerAlphaMax; // Standard-Transparenz
            shieldMaterial.color = color;
        }
    }

    // Aktualisiert die Sichtbarkeit der Energie-Balken basierend auf currentEnergy
    private void UpdateEnergyBars()
    {
        for (int i = 0; i < energyBars.Length; i++)
        {
            if (energyBars[i] != null)
                energyBars[i].enabled = i < currentEnergy;
        }

        // Flackern starten/stoppen basierend auf Energie
        if (currentEnergy > 3 && flickerCoroutine != null)
        {
            StopCoroutine(flickerCoroutine);
            flickerCoroutine = null;
            ResetShieldMaterial();
        }
        else if (currentEnergy <= 3 && isShieldActive && flickerCoroutine == null)
        {
            flickerCoroutine = StartCoroutine(FlickerShield());
        }
    }

    // �ffentliche Methode zum Aktivieren des Schildes
    public void ActivateShield()
    {
        if (!isShieldActive && currentEnergy > 0 && shield != null)
        {
            isShieldActive = true;
            shield.SetActive(true);

            // Stoppe Aufladen und Flackern, falls sie laufen
            if (rechargeCoroutine != null)
            {
                StopCoroutine(rechargeCoroutine);
                rechargeCoroutine = null;
            }
            if (flickerCoroutine != null)
            {
                StopCoroutine(flickerCoroutine);
                flickerCoroutine = null;
                ResetShieldMaterial();
            }

            // Starte Verbrauch
            drainCoroutine = StartCoroutine(DrainEnergy());
        }
    }

    // �ffentliche Methode zum Deaktivieren des Schildes
    public void DeactivateShield()
    {
        if (isShieldActive)
        {
            isShieldActive = false;
            if (shield != null)
                shield.SetActive(false);

            // Stoppe Verbrauch und Flackern
            if (drainCoroutine != null)
            {
                StopCoroutine(drainCoroutine);
                drainCoroutine = null;
            }
            if (flickerCoroutine != null)
            {
                StopCoroutine(flickerCoroutine);
                flickerCoroutine = null;
                ResetShieldMaterial();
            }

            // Starte Aufladen, wenn n�tig
            if (currentEnergy < 9 && rechargeCoroutine == null)
            {
                rechargeCoroutine = StartCoroutine(RechargeEnergy());
            }
        }
    }

    // Public Getter f�r isShieldActive (f�r Zugriff aus anderen Skripts, z.B. PlayerController)
    public bool IsShieldActive()
    {
        return isShieldActive;
    }
}