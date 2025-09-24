using UnityEngine;

/// <summary>
/// El UIManager es responsable de controlar la visibilidad de los diferentes
/// paneles de la interfaz de usuario. Su dise�o se basa en el principio de que
/// solo un panel puede estar activo a la vez, lo que simplifica la l�gica
/// de navegaci�n y evita que los elementos de UI se superpongan.
/// </summary>
public class UIManager : MonoBehaviour
{
    // --- REFERENCIAS DE PANELES ---

    // Las siguientes variables son referencias a los objetos de la UI en la
    // jerarqu�a de la escena. Se marcan con [SerializeField] para que puedan
    // ser asignadas desde el Inspector de Unity, permitiendo una f�cil
    // configuraci�n sin necesidad de buscarlas en el c�digo.

    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private GameObject miniOptionsPanel;
    [SerializeField] private GameObject decorationPanel;

    // --- M�TODOS P�BLICOS DE CONTROL ---

    // Estos m�todos son p�blicos para que otros scripts, como el `InteractionController`
    // o los botones de la UI, puedan llamarlos directamente para cambiar el estado de la interfaz.

    /// <summary>
    /// Activa el panel de inventario y asegura que todos los dem�s est�n ocultos.
    /// Este m�todo se llamar�a, por ejemplo, al presionar el bot�n de "Inventario".
    /// </summary>
    public void OpenInventory()
    {
        // Activa el panel de inventario.
        inventoryPanel.SetActive(true);
        // Desactiva los dem�s paneles para garantizar que no haya solapamientos.
        miniOptionsPanel.SetActive(false);
        decorationPanel.SetActive(false);
    }

    /// <summary>
    /// Muestra el mini panel de opciones para un objeto seleccionado y oculta los dem�s.
    /// Es llamado por el `InteractionController` cuando el usuario hace clic en un objeto en la escena.
    /// </summary>
    public void ShowMiniOptions()
    {
        // Desactiva el inventario para evitar que ambos paneles est�n visibles.
        inventoryPanel.SetActive(false);
        // Activa el mini panel de opciones.
        miniOptionsPanel.SetActive(true);
        // Desactiva el panel de decoraci�n.
        decorationPanel.SetActive(false);
    }

    /// <summary>
    /// Muestra el panel de decoraci�n y oculta los dem�s.
    /// Es llamado por el `InteractionController` despu�s de que el usuario selecciona la opci�n "Decorar".
    /// </summary>
    public void OpenDecorationPanel()
    {
        // Desactiva el inventario y el mini panel.
        inventoryPanel.SetActive(false);
        miniOptionsPanel.SetActive(false);
        // Activa el panel de decoraci�n.
        decorationPanel.SetActive(true);
    }

    /// <summary>
    /// Oculta todos los paneles de la UI.
    /// Este m�todo es fundamental para limpiar la interfaz, por ejemplo,
    /// cuando el usuario ha completado una acci�n como colocar un objeto o
    /// ha seleccionado una opci�n en los mini paneles.
    /// </summary>
    public void HideAllPanels()
    {
        // Simplemente desactiva todos los paneles.
        inventoryPanel.SetActive(false);
        miniOptionsPanel.SetActive(false);
        decorationPanel.SetActive(false);
    }
}