using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Botón de compra en tienda. Permite comprar objetos base o decorativos.
/// </summary>
public class ShopItem : MonoBehaviour
{
    [Header("Item Settings")]
    [SerializeField] private string itemName;
    [SerializeField] private int price = 100;
    [SerializeField] private GameObject prefabToGive;

    [Tooltip("Si es true, este objeto es una decoración, no se puede colocar libremente en el plano.")]
    [SerializeField] private bool isDecoration = false;

    [Header("UI References")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private Button buyButton;

    private void Awake()
    {
        if (nameText != null)
            nameText.text = itemName;

        if (priceText != null)
            priceText.text = price + " coins";

        if (buyButton != null)
        {
            buyButton.onClick.AddListener(OnBuyClicked);
        }
    }

    private void OnDestroy()
    {
        if (buyButton != null)
            buyButton.onClick.RemoveListener(OnBuyClicked);
    }

    private void OnBuyClicked()
    {
        if (CurrencyManager.Instance.SpendCoins(price))
        {
            // Puedes envolver el prefab en una estructura que guarde isDecoration
            InventoryManager.Instance.AddItem(prefabToGive);
            // Opcional: desactivar botón para evitar re-compras
            buyButton.interactable = false;
        }
        else
        {
            //Debug.Log("No tienes suficientes monedas para comprar " + itemName);
        }
    }
}
