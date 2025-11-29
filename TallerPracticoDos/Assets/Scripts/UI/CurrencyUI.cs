using UnityEngine;
using TMPro;

public class CurrencyUI : MonoBehaviour
{
    [SerializeField] private TMP_Text coinsText;

    private void OnEnable()
    {
        // Asegurar que no quede "NewText" si CurrencyManager aún no existe
        if (coinsText != null)
            coinsText.text = "0";

        // Si ya existe CurrencyManager, suscribir y actualizar inmediatamente
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnCoinsChanged += UpdateUI;
            UpdateUI();
        }
        else
        {
            // Esperar a que el manager exista
            StartCoroutine(WaitForManager());
        }
    }

    private void OnDisable()
    {
        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.OnCoinsChanged -= UpdateUI;
    }

    private System.Collections.IEnumerator WaitForManager()
    {
        while (CurrencyManager.Instance == null)
            yield return null;

        CurrencyManager.Instance.OnCoinsChanged += UpdateUI;
        UpdateUI();
    }

    /// <summary>
    /// Actualiza el texto con el valor actual del CurrencyManager.
    /// </summary>
    private void UpdateUI()
    {
        if (coinsText != null && CurrencyManager.Instance != null)
            coinsText.text = CurrencyManager.Instance.Coins.ToString();
    }
}
