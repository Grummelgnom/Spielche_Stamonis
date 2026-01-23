using UnityEngine;

public class SimpleSoundManager : MonoBehaviour
{
    public static SimpleSoundManager Instance { get; private set; }
    private AudioSource audioSource;
    private AudioSource loopingSource;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            audioSource = gameObject.AddComponent<AudioSource>();
            loopingSource = gameObject.AddComponent<AudioSource>();
            loopingSource.loop = true;
            DontDestroyOnLoad(gameObject);
        }
        else { Destroy(gameObject); }
    }

    public void PlayLaserSound() { PlayTone(440f, 0.1f, 0.1f, true); }
    public void PlayPowerUpSound() { PlayTone(600f, 0.2f, 0.2f, true, true); }
    public void PlayExplosionSound() { PlayTone(80f, 0.25f, 0.3f, false); }
    public void PlayPlayerDeathSound() { PlayTone(200f, 0.8f, 0.4f, true, false); }
    public void PlayShieldWarningSound() { PlayTone(880f, 0.15f, 0.2f, false); }

    public void SetUltimateLoop(bool active)
    {
        if (active && !loopingSource.isPlaying)
        {
            loopingSource.clip = CreateToneClip(150f, 0.5f, 0.1f, false);
            loopingSource.Play();
        }
        else if (!active) { loopingSource.Stop(); }
    }

    private void PlayTone(float freq, float duration, float volume, bool sweep, bool up = false)
    {
        audioSource.PlayOneShot(CreateToneClip(freq, duration, volume, sweep, up));
    }

    private AudioClip CreateToneClip(float freq, float duration, float volume, bool sweep, bool up = false)
    {
        int sampleRate = 44100;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float currentFreq = sweep ? (up ? freq + (i * 0.05f) : freq - (i * 0.05f)) : freq;
            samples[i] = Mathf.Sin(2 * Mathf.PI * currentFreq * t) * volume * (1f - (float)i / sampleCount);
        }
        AudioClip clip = AudioClip.Create("GenTone", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
