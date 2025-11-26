using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    // === EVENTOS (solo AudioManager puede invocarlos) ===
    public static event System.Action OnMainSceneMusic;
    public static event System.Action OnMiniGameMusic;
    public static event System.Action OnEnemyHitSound;
    public static event System.Action OnCatHiss;
    public static event System.Action OnCatMeow;
    public static event System.Action OnCatPurr;

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
        }
    }

    // === MÉTODOS PARA DISPARAR SONIDOS ===
    public void PlayMainSceneMusic() => OnMainSceneMusic?.Invoke();
    public void PlayMiniGameMusic() => OnMiniGameMusic?.Invoke();
    public void PlayEnemyHitSound() => OnEnemyHitSound?.Invoke();
    public void PlayCatHiss() => OnCatHiss?.Invoke();
    public void PlayCatMeow() => OnCatMeow?.Invoke();
    public void PlayCatPurr() => OnCatPurr?.Invoke();
}
