using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }

    [Header("Starting Settings")]
    [SerializeField] private int startingCoins = 0;

    private const string CoinsKey = "PLAYER_COINS";
    public int Coins { get; private set; }
    public event Action OnCoinsChanged;

    // Última cantidad ganada en una sesión/minijuego (se usa para notificar PlayerMetrics cuando esté disponible)
    private float lastSessionCoins = 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            //Debug.Log("Destroying duplicate CurrencyManager");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        //Debug.Log("CurrencyManager Awake - Instance set");

#if UNITY_EDITOR
        PlayerPrefs.DeleteKey(CoinsKey); // Solo en editor si quieres pruebas limpias (opcional)
#endif

        LoadCoins();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Cada vez que se carga una escena, intentar notificar PlayerMetrics si hay coins pendientes
        TryNotifyPlayerMetrics();
    }

    private void LoadCoins()
    {
        Coins = PlayerPrefs.GetInt(CoinsKey, startingCoins);
        OnCoinsChanged?.Invoke();
    }

    private void SaveCoins()
    {
        PlayerPrefs.SetInt(CoinsKey, Coins);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Añade monedas al total actual y notifica PlayerMetrics si está presente.
    /// </summary>
    public void AddCoins(int amount)
    {
        if (amount <= 0) return;

        //Debug.Log($"AddCoins called with amount: {amount}");
        Coins += amount;
        SaveCoins();
        OnCoinsChanged?.Invoke();

        // Guardamos la info de la sesión para notificar al PlayerMetrics
        lastSessionCoins = amount;

        // Intentamos notificar inmediatamente (si PlayerMetrics está en la escena actual)
        TryNotifyPlayerMetrics();
    }

    /// <summary>
    /// Intenta gastar monedas. Devuelve true si fue exitoso.
    /// </summary>
    public bool SpendCoins(int amount)
    {
        if (Coins < amount)
            return false;

        Coins -= amount;
        SaveCoins();
        OnCoinsChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Intenta encontrar PlayerMetrics en la escena y notificar la última sesión de monedas.
    /// Si PlayerMetrics no está presente, deja lastSessionCoins intacto y esperará al siguiente SceneLoaded.
    /// </summary>
    private void TryNotifyPlayerMetrics()
    {
        if (lastSessionCoins <= 0f) return;

        var pm = FindFirstObjectByType<PlayerMetrics>();
        if (pm != null)
        {
            pm.RegisterGameSession(lastSessionCoins); // registra la sesión (normalización interna)
            //Debug.Log($"[CurrencyManager] Notified PlayerMetrics of coins: {lastSessionCoins}");
            lastSessionCoins = 0f; // ya notificado
        }
        else
        {
            //Debug.Log("[CurrencyManager] PlayerMetrics no encontrado en la escena actual. Se reintentará en la carga de la siguiente escena.");
        }
    }
}
