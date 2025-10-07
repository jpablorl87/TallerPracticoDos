using UnityEngine;

/// <summary>
/// El UIManager es responsable de controlar la visibilidad de los diferentes
/// paneles de la interfaz de usuario. Su diseño se basa en el principio de que
/// solo un panel puede estar activo a la vez, lo que simplifica la lógica
/// de navegación y evita que los elementos de UI se superpongan.
/// </summary>
public class UIManager : MonoBehaviour
{
    // --- REFERENCIAS DE PANELES ---

    // Las siguientes variables son referencias a los objetos de la UI en la
    // jerarquía de la escena. Se marcan con [SerializeField] para que puedan
    // ser asignadas desde el Inspector de Unity, permitiendo una fácil
    // configuración sin necesidad de buscarlas en el código.

    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private GameObject miniOptionsPanel;
    [SerializeField] private GameObject decorationPanel;
    [SerializeField] private MiniGamesUI miniGamesUI;

    // --- MÉTODOS PÚBLICOS DE CONTROL ---

    // Estos métodos son públicos para que otros scripts, como el `InteractionController`
    // o los botones de la UI, puedan llamarlos directamente para cambiar el estado de la interfaz.

    /// <summary>
    /// Activa el panel de inventario y asegura que todos los demás estén ocultos.
    /// Este método se llamaría, por ejemplo, al presionar el botón de "Inventario".
    /// </summary>
    public void OpenInventory()
    {
        // Activa el panel de inventario.
        inventoryPanel.SetActive(true);
        // Desactiva los demás paneles para garantizar que no haya solapamientos.
        miniOptionsPanel.SetActive(false);
        decorationPanel.SetActive(false);
    }

    /// <summary>
    /// Muestra el mini panel de opciones para un objeto seleccionado y oculta los demás.
    /// Es llamado por el `InteractionController` cuando el usuario hace clic en un objeto en la escena.
    /// </summary>
    public void ShowMiniOptions()
    {
        // Desactiva el inventario para evitar que ambos paneles estén visibles.
        inventoryPanel.SetActive(false);
        // Activa el mini panel de opciones.
        miniOptionsPanel.SetActive(true);
        // Desactiva el panel de decoración.
        decorationPanel.SetActive(false);
    }

    /// <summary>
    /// Muestra el panel de decoración y oculta los demás.
    /// Es llamado por el `InteractionController` después de que el usuario selecciona la opción "Decorar".
    /// </summary>
    public void OpenDecorationPanel()
    {
        // Desactiva el inventario y el mini panel.
        inventoryPanel.SetActive(false);
        miniOptionsPanel.SetActive(false);
        // Activa el panel de decoración.
        decorationPanel.SetActive(true);
    }

    /// <summary>
    /// Oculta todos los paneles de la UI.
    /// Este método es fundamental para limpiar la interfaz, por ejemplo,
    /// cuando el usuario ha completado una acción como colocar un objeto o
    /// ha seleccionado una opción en los mini paneles.
    /// </summary>
    public void HideAllPanels()
    {
        // Simplemente desactiva todos los paneles.
        inventoryPanel.SetActive(false);
        miniOptionsPanel.SetActive(false);
        decorationPanel.SetActive(false);
    }
    /// <summary>
    /// Muestra el panel de minijuegos y oculta los demás.
    /// </summary>
    public void OpenMiniGamePanel()
    {
        HideAllPanels();
        miniGamesUI.Show();
    }
}