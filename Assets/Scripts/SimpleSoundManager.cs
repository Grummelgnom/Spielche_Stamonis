using UnityEngine;

public class SimpleSoundManager : MonoBehaviour
{
    public static SimpleSoundManager Instance { get; private set; }

    private AudioSource audioSource;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            audioSource = gameObject.AddComponent<AudioSource>();
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Erzeugt einen "Laser"-Schuss-Sound
    public void PlayLaserSound()
    {
        PlayTone(440f, 0.1f, 0.2f, true); // Von Hoch nach Tief
    }

    // Erzeugt einen "Explosion"-Sound
    public void PlayExplosionSound()
    {
        PlayTone(100f, 0.3f, 0.5f, false); // Tiefer Brummton
    }

    // Erzeugt einen "PowerUp"-Sound
    public void PlayPowerUpSound()
    {
        PlayTone(600f, 0.2f, 0.3f, true, true); // Ansteigend
    }

    private void PlayTone(float freq, float duration, float volume, bool sweep, bool up = false)
    {
        int sampleRate = 44100;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float currentFreq = freq;

            if (sweep)
            {
                if (up) currentFreq = freq + (i * 0.05f);
                else currentFreq = freq - (i * 0.05f);
            }

            samples[i] = Mathf.Sin(2 * Mathf.PI * currentFreq * t) * volume * (1f - (float)i / sampleCount);
        }

        AudioClip clip = AudioClip.Create("GeneratedTone", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        audioSource.PlayOneShot(clip);
    }
}
