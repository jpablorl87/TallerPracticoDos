using UnityEngine;
using TMPro;
using UnityEngine.UI;
/// <summary>
/// Muestra los objetos del inventario visualmente en la UI.
/// Se suscribe al evento OnInventoryUpdated para actualizar automáticamente.
/// </summary>
public class InventoryUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RectTransform contentArea; // Contenedor donde se agregan los ítems
    [SerializeField] private GameObject itemTemplate;   // Prefab visual para representar cada ítem (debe ser un prefab UI)
    private void OnEnable()
    {
        // Suscribirse al evento de actualización
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryUpdated += RefreshUI;
        }
        RefreshUI();
    }

    private void OnDisable()
    {
        // Desuscribirse para evitar errores
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryUpdated -= RefreshUI;
        }
    }
    /// <summary>
    /// Actualiza la interfaz de usuario con los elementos actuales del inventario.
    /// </summary>
    private void RefreshUI()
    {
        // Limpiar contenido anterior
        foreach (Transform child in contentArea)
        {
            Destroy(child.gameObject);
        }
        // Agregar cada ítem como una entrada visual
        var items = InventoryManager.Instance.GetItems();
        foreach (GameObject item in items)
        {
            GameObject newItem = Instantiate(itemTemplate, contentArea);
            newItem.SetActive(true); // Asegúrate de que esté activo (en caso de estar desactivado como plantilla)
            // Opcional: Mostrar el nombre del prefab o un ícono personalizado
            TMP_Text label = newItem.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                label.text = item.name;
            }
        }
    }
}