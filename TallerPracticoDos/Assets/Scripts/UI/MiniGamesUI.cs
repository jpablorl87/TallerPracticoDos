using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// Controla el panel de selección de minijuegos. Cada botón debe estar
/// asignado desde el Inspector para ejecutar el minijuego correspondiente.
/// </summary>
public class MiniGamesUI : MonoBehaviour
{
    // --- REFERENCIAS DE UI ---
    [Header("Panel")]
    [Tooltip("Panel completo del menú de minijuegos.")]
    [SerializeField] private GameObject miniGamesPanel;
    [Header("Botones")]
    [Tooltip("Botón para lanzar el minijuego 'The Hunt'.")]
    [SerializeField] private Button theHuntButton;
    private void Awake()
    {
        // Suscribimos eventos a los botones
        theHuntButton.onClick.AddListener(OnTheHuntSelected);
    }
    private void OnDestroy()
    {
        // Limpiar suscripción para evitar fugas de memoria
        theHuntButton.onClick.RemoveListener(OnTheHuntSelected);
    }
    /// <summary>
    /// Llamado cuando se pulsa el botón "The Hunt".
    /// </summary>
    private void OnTheHuntSelected()
    {
        MiniGameManager.Instance.StartMiniGame("MiniGame_TheHunt");
    }
    /// <summary>
    /// Muestra el panel del menú de minijuegos.
    /// </summary>
    public void Show()
    {
        miniGamesPanel.SetActive(true);
    }
    /// <summary>
    /// Oculta el panel del menú de minijuegos.
    /// </summary>
    public void Hide()
    {
        miniGamesPanel.SetActive(false);
    }
}