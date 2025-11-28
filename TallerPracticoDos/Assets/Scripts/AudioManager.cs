using System;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    // === EVENTOS (solo AudioManager puede invocarlos) ===
    public static event Action OnMainSceneMusic;
    public static event Action OnMiniGameMusic;
    public static event Action OnEnemyHitSound;
    public static event Action OnCatHiss;
    public static event Action OnCatMeow;
    public static event Action OnCatPurr;

    [Header("Music")]
    [SerializeField] private AudioSource musicSource;// Asignar en inspector (AudioSource, loop = true)
    [SerializeField] private AudioClip mainSceneMusic;
    [SerializeField] private AudioClip miniGameMusic;

    [Header("SFX")]
    [SerializeField] private List<AudioSource> sfxSources = new();// Pool de AudioSources para SFX (asignar 2-6)
    [SerializeField] private AudioClip enemyHitClip;
    [SerializeField] private AudioClip catHissClip;
    [SerializeField] private AudioClip catMeowClip;
    [SerializeField] private AudioClip catPurrClip;

    // Pool index para reproducir SFX sin cortar otros
    private int sfxIndex = 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Seguridad: si no hay musicSource, intentamos buscar uno hijo o crear uno mínimo
        if (musicSource == null)
        {
            var ms = GetComponent<AudioSource>();
            if (ms != null)
                musicSource = ms;
            else
                musicSource = gameObject.AddComponent<AudioSource>();
        }

        // Asegurar que los sfxSources tienen al menos un AudioSource
        if (sfxSources.Count == 0)
        {
            // crear 2 sources si no hay ninguno (más robusto)
            for (int i = 0; i < 2; i++)
            {
                var go = new GameObject($"SFXSource_{i}");
                go.transform.SetParent(transform);
                var a = go.AddComponent<AudioSource>();
                a.playOnAwake = false;
                sfxSources.Add(a);
            }
        }
    }

    // === MÉTODOS PARA DISPARAR SONIDOS ===
    // Los métodos públicos reproducen y además invocan los eventos (para listeners externos)
    public void PlayMainSceneMusic()
    {
        if (mainSceneMusic != null)
        {
            musicSource.clip = mainSceneMusic;
            musicSource.loop = true;
            musicSource.Play();
        }
        OnMainSceneMusic?.Invoke();
    }

    public void PlayMiniGameMusic()
    {
        if (miniGameMusic != null)
        {
            musicSource.clip = miniGameMusic;
            musicSource.loop = true;
            musicSource.Play();
        }
        OnMiniGameMusic?.Invoke();
    }

    public void StopMusic()
    {
        musicSource.Stop();
    }

    public void PlayEnemyHitSound()
    {
        PlaySFX(enemyHitClip);
        OnEnemyHitSound?.Invoke();
    }

    public void PlayCatHiss()
    {
        PlaySFX(catHissClip);
        OnCatHiss?.Invoke();
    }

    public void PlayCatMeow()
    {
        PlaySFX(catMeowClip);
        OnCatMeow?.Invoke();
    }

    public void PlayCatPurr()
    {
        PlaySFX(catPurrClip);
        OnCatPurr?.Invoke();
    }

    // Helper: reproduce un clip en el siguiente source libre del pool (rotativo)
    private void PlaySFX(AudioClip clip)
    {
        if (clip == null || sfxSources == null || sfxSources.Count == 0) return;

        var src = sfxSources[sfxIndex];
        if (src == null)
        {
            // crear si falta
            var go = new GameObject($"SFXSource_generated_{sfxIndex}");
            go.transform.SetParent(transform);
            src = go.AddComponent<AudioSource>();
            sfxSources[sfxIndex] = src;
        }

        src.PlayOneShot(clip);
        sfxIndex = (sfxIndex + 1) % sfxSources.Count;
    }

    // VOLUMEN -------------------------------------------------------
    // Nota: AudioListener.volume afecta todo; se puede usar para master
    public void SetMasterVolume(float v)
    {
        AudioListener.volume = Mathf.Clamp01(v);
    }

    public void SetMusicVolume(float v)
    {
        if (musicSource != null)
            musicSource.volume = Mathf.Clamp01(v);
    }

    public void SetSFXVolume(float v)
    {
        if (sfxSources == null) return;
        v = Mathf.Clamp01(v);
        for (int i = 0; i < sfxSources.Count; i++)
        {
            if (sfxSources[i] != null)
                sfxSources[i].volume = v;
        }
    }
}
