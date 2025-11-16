using UnityEngine;
using TMPro;

/// <summary>
/// Componente UI que muestra la cantidad actual de monedas en pantalla.
/// Escucha los cambios del CurrencyManager para actualizar en tiempo real.
/// </summary>
public class CurrencyUI : MonoBehaviour
{
    [SerializeField] private TMP_Text coinsText;

    private void OnEnable()
    {
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnCoinsChanged += UpdateUI;
        }
        UpdateUI();  // Mostrar inmediatamente
    }

    private void OnDisable()
    {
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnCoinsChanged -= UpdateUI;
        }
    }

    /// <summary>
    /// Actualiza el texto de monedas con el valor actual.
    /// </summary>
    public void UpdateUI()
    {
        if (coinsText != null && CurrencyManager.Instance != null)
        {
            coinsText.text = CurrencyManager.Instance.Coins.ToString();
        }
    }

    /// <summary>
    /// Sobrecarga que permite ser llamada con el parámetro del evento.
    /// </summary>
    private void UpdateUI(int newCoins)
    {
        if (coinsText != null)
        {
            coinsText.text = newCoins.ToString();
        }
    }
}