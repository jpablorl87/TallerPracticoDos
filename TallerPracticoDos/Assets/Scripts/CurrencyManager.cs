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
        // Singleton: mantener una única instancia global
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Solo durante pruebas en editor, reiniciar monedas
#if UNITY_EDITOR
        PlayerPrefs.DeleteKey(CoinsKey);
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
