using System;
using UnityEngine;

/// <summary>
/// Sistema central de monedas del jugador. Guarda y recupera entre sesiones.
/// </summary>
public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }

    [Header("Starting Settings")]
    [SerializeField] private int startingCoins = 0;

    private const string CoinsKey = "PLAYER_COINS";
    public int Coins { get; private set; }
    public event Action OnCoinsChanged;

    private void Awake()
    {
        // Si ya existe una instancia válida, destruye esta y sal del método inmediatamente
        if (Instance != null && Instance != this)
        {
            Debug.Log("Destroying duplicate CurrencyManager");
            Destroy(gameObject);
            return;
        }

        // Solo ejecuta el resto si esta es la instancia válida
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("CurrencyManager Awake - Instance set");

#if UNITY_EDITOR
        PlayerPrefs.DeleteKey(CoinsKey); // Esto solo se debe ejecutar una vez
#endif

        LoadCoins();
    }

    /// <summary>
    /// Carga las monedas guardadas desde PlayerPrefs o usa el valor inicial.
    /// </summary>
    private void LoadCoins()
    {
        Coins = PlayerPrefs.GetInt(CoinsKey, startingCoins);
        OnCoinsChanged?.Invoke(); // Notifica listeners al cargar monedas
    }

    /// <summary>
    /// Guarda la cantidad actual de monedas.
    /// </summary>
    private void SaveCoins()
    {
        PlayerPrefs.SetInt(CoinsKey, Coins);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Añade monedas al total actual.
    /// </summary>
    public void AddCoins(int amount)
    {
        Debug.Log($"AddCoins called with amount: {amount}");
        Coins += amount;
        SaveCoins();
        OnCoinsChanged?.Invoke(); // Notifica listeners
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
        OnCoinsChanged?.Invoke(); // Notifica listeners
        return true;
    }
}
