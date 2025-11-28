using UnityEngine;
using UnityEngine.SceneManagement;
/// <summary>
/// Gestiona la lógica central de minijuegos:
/// - Selección de minijuego
/// - Transición entre escenas
/// - Almacena las monedas ganadas temporalmente
/// - Retorna a la escena principal al terminar el juego
/// </summary>
public class MiniGameManager : MonoBehaviour
{
    // --- SINGLETON ---
    public static MiniGameManager Instance { get; private set; }
    // --- CONFIGURACIÓN SERIALIZADA ---
    [Tooltip("Nombre de la escena principal a la que se regresará después del minijuego.")]
    [SerializeField] private string mainSceneName = "SampleScene";
    private bool hasFinished = false;
    // --- ESTADO ---
    public int CoinsEarned { get; private set; }
    private string currentMiniGameScene;
    private void Awake()
    {
        // Patrón Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        AudioManager.OnMiniGameMusic += PlayMusic;
        AudioManager.OnEnemyHitSound += PlayHitSound;
    }
    private void Start()
    {
        AudioManager.Instance.PlayMiniGameMusic();
    }
    private void PlayMusic()
    {
        Debug.Log("[MiniGameManager] Reproduciendo música del minijuego");
    }

    private void PlayHitSound()
    {
        Debug.Log("[MiniGameManager] Sonido de hit del minijuego");
    }
    /// <summary>
    /// Llama este método para iniciar un minijuego por nombre de escena.
    /// </summary>
    /// <param name="sceneName">Nombre exacto de la escena del minijuego.</param>
    public void StartMiniGame(string sceneName)
    {
        hasFinished = false;
        CoinsEarned = 0; // Reinicia las monedas para este minijuego
        currentMiniGameScene = sceneName;
        SceneManager.LoadScene(sceneName);
    }
    /// <summary>
    /// Llama este método desde el minijuego cuando haya terminado, para regresar a la escena principal.
    /// </summary>
    /// <param name="coinsEarned">Cantidad de monedas ganadas en el minijuego.</param>
    public void FinishMiniGame(int coinsEarned)
    {
        if (hasFinished) return; // evita duplicados
        hasFinished = true;

        CoinsEarned = coinsEarned;

        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.AddCoins(coinsEarned);
        }
        else
        {
            //Debug.LogWarning("[MiniGameManager] CurrencyManager no disponible al finalizar minijuego.");
        }
        Time.timeScale = 1f;//No más Pablonadas!
        SceneManager.LoadScene(mainSceneName);
    }
    private void OnDestroy()
    {
        AudioManager.OnMiniGameMusic -= PlayMusic;
        AudioManager.OnEnemyHitSound -= PlayHitSound;
    }

}
