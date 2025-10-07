using UnityEngine;
using TMPro;

/// <summary>
/// Componente UI que actualiza el texto de monedas al mostrarse.
/// </summary>
public class CurrencyUI : MonoBehaviour
{
    [SerializeField] private TMP_Text coinsText;

    private void OnEnable()
    {
        UpdateUI();
    }

    /// <summary>
    /// Refresca el texto con la cantidad actual de monedas.
    /// </summary>
    public void UpdateUI()
    {
        if (coinsText != null && CurrencyManager.Instance != null)
        {
            coinsText.text = CurrencyManager.Instance.Coins.ToString();
        }
    }
}
