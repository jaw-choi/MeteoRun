using UnityEngine;

public class GameAudio : MonoBehaviour
{
    private const string BgmVolumeKey = "bgm_volume";
    private const string SfxVolumeKey = "sfx_volume";

    public static GameAudio Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource ambienceSource = null;
    [SerializeField] private AudioSource sfxSource = null;

    [Header("Clips")]
    [SerializeField] private AudioClip ambienceClip = null;
    [SerializeField] private AudioClip meteorPassClip = null;
    [SerializeField] private AudioClip impactClip = null;

    [Header("Clip Volumes")]
    [SerializeField, Range(0f, 1f)] private float ambienceVolume = 0.35f;
    [SerializeField, Range(0f, 1f)] private float meteorPassVolume = 0.55f;
    [SerializeField, Range(0f, 1f)] private float impactVolume = 1f;

    [Header("Master Volumes")]
    [SerializeField, Range(0f, 1f)] private float bgmMasterVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float sfxMasterVolume = 1f;

    public float BgmVolume => bgmMasterVolume;
    public float SfxVolume => sfxMasterVolume;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        LoadSavedSettings();
        EnsureAudioSources();
        ApplyAmbienceVolume();
    }

    private void Start()
    {
        PlayAmbience();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void PlayAmbience()
    {
        if (ambienceSource == null || ambienceClip == null)
        {
            return;
        }

        ambienceSource.clip = ambienceClip;
        ambienceSource.loop = true;
        ApplyAmbienceVolume();

        if (!ambienceSource.isPlaying)
        {
            ambienceSource.Play();
        }
    }

    public void PlayMeteorPass()
    {
        PlayOneShot(meteorPassClip, meteorPassVolume);
    }

    public void PlayImpact()
    {
        PlayOneShot(impactClip, impactVolume);
    }

    public void SetBgmVolume(float volume)
    {
        bgmMasterVolume = Mathf.Clamp01(volume);
        ApplyAmbienceVolume();
        SaveFloat(BgmVolumeKey, bgmMasterVolume);
    }

    public void SetSfxVolume(float volume)
    {
        sfxMasterVolume = Mathf.Clamp01(volume);
        SaveFloat(SfxVolumeKey, sfxMasterVolume);
    }

    private void EnsureAudioSources()
    {
        if (ambienceSource == null)
        {
            ambienceSource = gameObject.AddComponent<AudioSource>();
            ambienceSource.playOnAwake = false;
            ambienceSource.spatialBlend = 0f;
            ambienceSource.volume = 1f;
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f;
            sfxSource.volume = 1f;
        }
    }

    private void ApplyAmbienceVolume()
    {
        if (ambienceSource != null)
        {
            ambienceSource.volume = ambienceVolume * bgmMasterVolume;
        }
    }

    private void LoadSavedSettings()
    {
        bgmMasterVolume = PlayerPrefs.GetFloat(BgmVolumeKey, bgmMasterVolume);
        sfxMasterVolume = PlayerPrefs.GetFloat(SfxVolumeKey, sfxMasterVolume);
    }

    private void PlayOneShot(AudioClip clip, float volumeScale)
    {
        if (clip == null || sfxSource == null)
        {
            return;
        }

        sfxSource.PlayOneShot(clip, volumeScale * sfxMasterVolume);
    }

    private static void SaveFloat(string key, float value)
    {
        PlayerPrefs.SetFloat(key, value);
        PlayerPrefs.Save();
    }
}
