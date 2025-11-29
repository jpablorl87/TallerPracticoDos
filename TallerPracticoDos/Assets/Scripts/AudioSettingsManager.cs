using UnityEngine;

public class AudioSettingsManager : MonoBehaviour
{
    public static AudioSettingsManager Instance;

    private float masterVolume = 1f;
    private float musicVolume = 1f;
    private float sfxVolume = 1f;

    public float MasterVolume => masterVolume;
    public float MusicVolume => musicVolume;
    public float SFXVolume => sfxVolume;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadVolumes();
        ApplyToAudioManager();
    }

    public void SetMasterVolume(float v)
    {
        masterVolume = v;
        PlayerPrefs.SetFloat("MasterVolume", v);
        ApplyToAudioManager();
    }

    public void SetMusicVolume(float v)
    {
        musicVolume = v;
        PlayerPrefs.SetFloat("MusicVolume", v);
        ApplyToAudioManager();
    }

    public void SetSFXVolume(float v)
    {
        sfxVolume = v;
        PlayerPrefs.SetFloat("SFXVolume", v);
        ApplyToAudioManager();
    }

    private void LoadVolumes()
    {
        masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
    }

    public void ApplyToAudioManager()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMasterVolume(masterVolume);
            AudioManager.Instance.SetMusicVolume(musicVolume);
            AudioManager.Instance.SetSFXVolume(sfxVolume);
        }
    }
}
