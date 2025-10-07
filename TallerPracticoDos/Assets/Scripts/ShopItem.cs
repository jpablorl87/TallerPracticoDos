using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Script para cada botón de compra de un ítem en la tienda.
/// </summary>
public class ShopItem : MonoBehaviour
{
    [Header("Item Settings")]
    [SerializeField] private string itemName;
    [SerializeField] private int price = 100;
    [SerializeField] private GameObject prefabToGive;

    [Header("UI References")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private Button buyButton;

    private void Awake()
    {
        // Inicializar UI
        if (nameText != null)
            nameText.text = itemName;
        if (priceText != null)
            priceText.text = price + " coins";

        if (buyButton != null)
            buyButton.onClick.AddListener(OnBuyClicked);
    }

    private void OnDestroy()
    {
        if (buyButton != null)
            buyButton.onClick.RemoveListener(OnBuyClicked);
    }

    private void OnBuyClicked()
    {
        // Intentar gastar monedas
        if (CurrencyManager.Instance.SpendCoins(price))
        {
            InventoryManager.Instance.AddItem(prefabToGive);
            Debug.Log("Compra exitosa: " + itemName);
            // Opcional: desactivar botón o eliminar UI del ítem
            buyButton.interactable = false;
        }
        else
        {
            Debug.Log("No tienes suficientes monedas para comprar " + itemName);
        }
    }
}