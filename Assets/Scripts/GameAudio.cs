using UnityEngine;

public class GameAudio : MonoBehaviour
{
    [Header("Audio Sources")]
    [SerializeField] private AudioSource ambienceSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Clips")]
    [SerializeField] private AudioClip ambienceClip;
    [SerializeField] private AudioClip movementClip;
    [SerializeField] private AudioClip meteorPassClip;
    [SerializeField] private AudioClip impactClip;

    [Header("Tuning")]
    [SerializeField, Min(0.01f)] private float movementInterval = 0.14f;

    private float movementTimer;

    private void Awake()
    {
        EnsureAudioSources();
    }

    public void PlayAmbience()
    {
        if (ambienceSource == null || ambienceClip == null || ambienceSource.isPlaying)
        {
            return;
        }

        ambienceSource.clip = ambienceClip;
        ambienceSource.loop = true;
        ambienceSource.Play();
    }

    public void TickMovement(bool isMoving)
    {
        if (!isMoving)
        {
            movementTimer = 0f;
            return;
        }

        movementTimer -= Time.deltaTime;
        if (movementTimer > 0f)
        {
            return;
        }

        PlayOneShot(movementClip, 0.4f);
        movementTimer = movementInterval;
    }

    public void PlayMeteorPass()
    {
        PlayOneShot(meteorPassClip, 0.55f);
    }

    public void PlayImpact()
    {
        PlayOneShot(impactClip, 1f);
    }

    private void EnsureAudioSources()
    {
        if (ambienceSource == null)
        {
            ambienceSource = gameObject.AddComponent<AudioSource>();
            ambienceSource.playOnAwake = false;
            ambienceSource.spatialBlend = 0f;
            ambienceSource.volume = 0.35f;
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f;
            sfxSource.volume = 0.8f;
        }
    }

    private void PlayOneShot(AudioClip clip, float volumeScale)
    {
        if (clip == null || sfxSource == null)
        {
            return;
        }

        sfxSource.PlayOneShot(clip, volumeScale);
    }
}
