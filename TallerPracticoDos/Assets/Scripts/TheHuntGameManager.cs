using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Administra la lógica central del minijuego "The Hunt".
/// Controla el tiempo, el conteo de monedas, el fin del juego y la transición de escena.
/// </summary>
public class TheHuntGameManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text coinsText;         // Texto que muestra monedas actuales.
    [SerializeField] private TMP_Text timerText;         // Texto que muestra tiempo restante.
    [SerializeField] private GameObject endPanel;        // Panel que se muestra al finalizar.
    [SerializeField] private TMP_Text finalCoinsText;    // Texto que muestra resultado final dentro del panel.
    [SerializeField] private Button continueButton;      // Botón para regresar al terminar.

    [Header("Settings")]
    [SerializeField] private float gameDuration = 30f;   // Duración del minijuego en segundos.

    [Header("Spawner")]
    [SerializeField] private HuntEnemySpawner spawner;   // Referencia al spawner para detenerlo al final.

    public static TheHuntGameManager Instance { get; private set; }

    private int coins = 0;        // Monedas acumuladas en este juego
    private float timeLeft;       // Tiempo restante
    public bool GameIsRunning { get; private set; } = false;

    private void Awake()
    {
        // Patrón Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        AudioManager.OnMiniGameMusic += OnMiniMusic;
    }
    private void OnDestroy()
    {
        AudioManager.OnMiniGameMusic -= OnMiniMusic;
    }

    private void OnMiniMusic()
    {
        Debug.Log("[HuntGame] Música del minijuego");
    }
    private void Start()
    {
        // Iniciar el minijuego
        timeLeft = gameDuration;
        GameIsRunning = true;
        endPanel.SetActive(false);

        // Iniciar spawner
        spawner.StartSpawning();

        // Iniciar cuenta regresiva cada segundo
        StartCoroutine(GameLoop());
    }

    private IEnumerator GameLoop()
    {
        while (timeLeft > 0f)
        {
            yield return new WaitForSeconds(1f);
            timeLeft -= 1f;
            timerText.text = $"Time: {(int)timeLeft}s";
        }

        // Tiempo completado
        FinishGame();
    }

    /// <summary>
    /// Añade monedas cuando el jugador caza un enemigo.
    /// </summary>
    public void AddCoins(int amount)
    {
        coins += amount;
        coinsText.text = $"Coins: {coins}";
    }

    /// <summary>
    /// Termina el juego: detiene spawn, muestra panel final, impide más interacciones.
    /// </summary>
    private void FinishGame()
    {
        GameIsRunning = false;

        // Detener spawn inmediato
        if (spawner != null)
        {
            spawner.StopSpawning();
        }

        endPanel.SetActive(true);
        finalCoinsText.text = $"You got {coins} coins!";
    }

    /// <summary>
    /// Llamado al presionar "Continue" en el panel final.
    /// Pasa las monedas al sistema general y regresa a la escena principal.
    /// </summary>
    private void OnContinuePressed()
    {
        // Solo llama al flujo central que maneja monedas y retorno de escena
        MiniGameManager.Instance.FinishMiniGame(coins);
    }
}