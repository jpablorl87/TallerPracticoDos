using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Muestra los objetos del inventario visualmente en la UI.
/// Se suscribe a OnInventoryUpdated para refrescar automáticamente.
/// </summary>
public class InventoryUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RectTransform contentArea;  // contenedor de ítems
    [SerializeField] private GameObject itemTemplate;    // prefab visual (botón) para cada ítem

    [Header("Dependencies")]
    [SerializeField] private InteractionController interactionController;

    private void OnEnable()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryUpdated += RefreshUI;
        }
        RefreshUI();
    }

    private void OnDisable()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryUpdated -= RefreshUI;
        }
    }

    /// <summary>
    /// Limpia ítems previos y genera botones por cada ítem comprados.
    /// </summary>
    private void RefreshUI()
    {
        // Limpiar
        foreach (Transform child in contentArea)
        {
            Destroy(child.gameObject);
        }

        var items = InventoryManager.Instance.GetItems();
        for (int i = 0; i < items.Count; i++)
        {
            int index = i; // capturar para la lambda
            GameObject newItem = Instantiate(itemTemplate, contentArea);
            newItem.SetActive(true);

            // Mostrar nombre
            TMP_Text label = newItem.GetComponentInChildren<TMP_Text>();
            if (label != null)
                label.text = items[i].name;

            // Botón para seleccionar
            Button btn = newItem.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(() =>
                {
                    interactionController.OnInventoryItemSelected(index);
                });
            }
        }
    }
}