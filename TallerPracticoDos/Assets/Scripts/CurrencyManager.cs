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

        LoadCoins();
    }

    /// <summary>
    /// Carga las monedas del almacenamiento persistente.
    /// </summary>
    private void LoadCoins()
    {
        Coins = PlayerPrefs.GetInt(CoinsKey, startingCoins);
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
    /// Añade monedas al total actual y guarda el cambio.
    /// </summary>
    public void AddCoins(int amount)
    {
        Coins += amount;
        SaveCoins();
    }

    /// <summary>
    /// Intenta gastar monedas. Retorna true si fue exitoso, false si no hay suficientes.
    /// </summary>
    public bool SpendCoins(int amount)
    {
        if (Coins < amount)
            return false;

        Coins -= amount;
        SaveCoins();
        return true;
    }
}