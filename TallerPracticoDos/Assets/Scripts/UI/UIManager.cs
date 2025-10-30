using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Administra la visibilidad de los paneles UI y la generación de botones de decoración.
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("Paneles Principales")]
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private GameObject miniOptionsPanel;
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private GameObject minigamesPanel;

    [Header("MiniOptions Buttons (assign in inspector)")]
    [SerializeField] private Button moveButton;
    [SerializeField] private Button deleteButton;

    [Header("Decoración UI")]
    [Tooltip("Prefab de botón (UI) para una decoración, con Button + TMP_Text")]
    [SerializeField] private GameObject decorationItemTemplate;
    [Tooltip("Contenedor dentro del panel donde se instancian los botones decoración")]
    [SerializeField] private Transform decorationContentArea;

    // Referencia al controlador para mandar los eventos
    private InteractionController interactionController;

    private void Awake()
    {
        // Obtiene la referencia si no fue asignada desde el Inspector
        if (interactionController == null)
            interactionController = FindObjectOfType<InteractionController>();

        // Ensure buttons are not wiredup multiple times
        if (moveButton != null) moveButton.onClick.RemoveAllListeners();
        if (deleteButton != null) deleteButton.onClick.RemoveAllListeners();
    }

    private void Start()
    {
        // Wire to interaction controller (safe: single place)
        if (interactionController != null)
        {
            if (moveButton != null) moveButton.onClick.AddListener(interactionController.OnMoveOptionSelected);
            if (deleteButton != null) deleteButton.onClick.AddListener(interactionController.OnDeleteOptionSelected);
        }
    }

    /// <summary>
    /// Oculta todos los paneles.
    /// </summary>
    public void HideAllPanels()
    {
        inventoryPanel?.SetActive(false);
        miniOptionsPanel?.SetActive(false);
        shopPanel?.SetActive(false);
        minigamesPanel?.SetActive(false);
    }

    public void OpenInventoryPanel()
    {
        HideAllPanels();
        inventoryPanel?.SetActive(true);
    }

    public void OpenMinigamesPanel()
    {
        HideAllPanels();
        minigamesPanel?.SetActive(true);
    }

    public void OpenShopPanel()
    {
        HideAllPanels();
        shopPanel?.SetActive(true);
    }

    public void ShowMiniOptions()
    {
        // Cierra otros paneles y abre solo miniOptions
        HideAllPanels();
        miniOptionsPanel?.SetActive(true);
    }

    /// <summary>
    /// Cierra un panel específico. Usado por botones "Cerrar".
    /// </summary>
    public void ClosePanel(GameObject panel)
    {
        if (panel != null)
            panel.SetActive(false);
    }

    /// <summary>
    /// Este método es llamado por los botones del panel de decoración.
    /// Lo llamará el botón correspondiente con el índice que representa la decoración.
    /// </summary>
    /// <param name="decorIndex">Índice en el arreglo decorationPrefabs del InteractionController</param>
    public void OnDecorationButtonClicked(int decorIndex)
    {
        if (interactionController != null)
        {
            interactionController.OnDecorationItemSelected(decorIndex);
        }
        // Opcional: cerrar el panel tras seleccionar una decoración
        HideAllPanels();
    }
}