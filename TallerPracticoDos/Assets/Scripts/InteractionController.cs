using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

/// <summary>
/// Controla la interacción del usuario con objetos en la escena:
/// - Taps para seleccionar objetos
/// - Colocar objetos desde el inventario
/// - Mover, eliminar o decorar objetos
/// </summary>
public class InteractionController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform sceneRoot;

    [Header("UI Panels")]
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private GameObject miniOptionsPanel;
    [SerializeField] private GameObject decorationPanel;

    [Header("Prefabs")]
    [SerializeField] private GameObject[] inventoryPrefabs;
    [SerializeField] private GameObject[] decorationPrefabs;

    private PlayerControls inputActions;

    private GameObject selectedInventoryPrefab = null;  // Objeto elegido del inventario
    private GameObject placedObject = null;            // Objeto en escena actualmente seleccionado
    private GameObject ghostObject = null;             // Objeto fantasma para mover

    private bool waitingToPlace = false;
    private bool isMovingObject = false;

    private GameObject pendingDecorationPrefab = null;
    private bool waitingToDecorate = false;

    private void Awake()
    {
        inputActions = new PlayerControls();
    }

    private void OnEnable()
    {
        inputActions.Gameplay.Enable();
        inputActions.Gameplay.Tap.performed += OnTap;
    }

    private void OnDisable()
    {
        inputActions.Gameplay.Tap.performed -= OnTap;
        inputActions.Gameplay.Disable();
    }

    private void OnTap(InputAction.CallbackContext ctx)
    {
        Vector2 screenPos = GetCurrentPointerPosition();

        // Corregido: Usamos una función auxiliar para verificar si el tap fue sobre la UI.
        if (IsPointerOverUI())
        {
            return;
        }

        // 1. Si estamos colocando algo del inventario
        if (waitingToPlace && selectedInventoryPrefab != null)
        {
            Ray ray = mainCamera.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                GameObject obj = Instantiate(selectedInventoryPrefab, hit.point, Quaternion.identity, sceneRoot);

                // Asegurarse de que tenga el componente SelectableObject
                if (obj.GetComponent<SelectableObject>() == null)
                    obj.AddComponent<SelectableObject>();

                placedObject = obj;

                // Resetear estado
                waitingToPlace = false;
                selectedInventoryPrefab = null;
            }
            return;
        }

        // 2. Si estamos moviendo un objeto (modo fantasma)
        if (isMovingObject && ghostObject != null)
        {
            Ray ray = mainCamera.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                EndMoveAtPosition(hit.point);
            }
            return;
        }

        // 3. Si estamos decorando (bola de navidad que se puede colocar en cualquier punto del objeto)
        if (waitingToDecorate && pendingDecorationPrefab != null && placedObject != null)
        {
            Ray ray = mainCamera.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider.gameObject == placedObject)
                {
                    GameObject dec = Instantiate(pendingDecorationPrefab, placedObject.transform);

                    // Convertir la posición del hit en coordenadas locales del objeto decorado
                    Vector3 localPos = placedObject.transform.InverseTransformPoint(hit.point);
                    dec.transform.localPosition = localPos;
                }
            }

            // Resetear estado de decoración
            waitingToDecorate = false;
            pendingDecorationPrefab = null;
            return;
        }

        // 4. Si se toca un objeto en la escena (que tenga SelectableObject), mostrar mini panel de opciones
        Ray ray2 = mainCamera.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray2, out RaycastHit hit2))
        {
            SelectableObject so = hit2.collider.gameObject.GetComponent<SelectableObject>();
            if (so != null)
            {
                placedObject = so.gameObject;
                ShowMiniOptions();
                return;
            }
        }

        // Si no hubo interacciones válidas, no hacemos nada
    }

    // ====================
    // MÉTODOS LLAMADOS DESDE UI
    // ====================

    public void OpenInventory()
    {
        inventoryPanel.SetActive(true);
    }

    public void OnInventoryItemSelected(int index)
    {
        if (index < 0 || index >= inventoryPrefabs.Length) return;

        selectedInventoryPrefab = inventoryPrefabs[index];
        waitingToPlace = true;

        inventoryPanel.SetActive(false);
    }

    public void OnMoveOptionSelected()
    {
        if (placedObject != null)
        {
            // Crear objeto fantasma y ocultar el original
            ghostObject = Instantiate(placedObject, placedObject.transform.position, placedObject.transform.rotation, sceneRoot);
            placedObject.SetActive(false);

            isMovingObject = true;
            miniOptionsPanel.SetActive(false);
        }
    }

    public void OnDeleteOptionSelected()
    {
        if (placedObject != null)
        {
            Destroy(placedObject);
            placedObject = null;
        }

        miniOptionsPanel.SetActive(false);
    }

    public void OnDecorateOptionSelected()
    {
        if (placedObject != null)
        {
            decorationPanel.SetActive(true);
            miniOptionsPanel.SetActive(false);
        }
    }

    public void OnDecorationItemSelected(int decorIndex)
    {
        if (decorIndex < 0 || decorIndex >= decorationPrefabs.Length) return;

        GameObject decorPrefab = decorationPrefabs[decorIndex];

        // Se agregó lógica para verificar si el objeto puede ser decorado.
        SelectableObject so = placedObject.GetComponent<SelectableObject>();
        if (so != null && so.CanDecorate(decorPrefab.tag))
        {
            // Si es una bola, se coloca con tap en el objeto
            if (decorPrefab.CompareTag("BallDecoration"))
            {
                pendingDecorationPrefab = decorPrefab;
                waitingToDecorate = true;
            }
            else
            {
                // Luces, estrella, etc. se colocan automáticamente
                GameObject dec = Instantiate(decorPrefab, placedObject.transform);
                dec.transform.localPosition = Vector3.zero;
            }
        }
        else
        {
            Debug.LogWarning("Este objeto no puede ser decorado con esta decoración.");
        }

        decorationPanel.SetActive(false);
    }

    private void EndMoveAtPosition(Vector3 pos)
    {
        placedObject.transform.position = pos;
        placedObject.SetActive(true);

        Destroy(ghostObject);
        ghostObject = null;
        isMovingObject = false;
    }

    public void ShowMiniOptions()
    {
        miniOptionsPanel.SetActive(true);

        // Aquí podrías mover el panel a la posición del objeto o mantenerlo centrado.
        // Depende de tu diseño de UI.
    }

    // ====================
    // FUNCIÓN AUXILIAR: DETECTAR SI EL TAP/Clic FUE SOBRE UI
    // ====================
    private bool IsPointerOverUI()
    {
        // Se usa la sobrecarga de IsPointerOverGameObject que funciona para todos los dispositivos de puntero.
        return EventSystem.current.IsPointerOverGameObject(Pointer.current?.deviceId ?? -1);
    }
    private Vector2 GetCurrentPointerPosition()
    {
#if UNITY_ANDROID || UNITY_IOS
    return Touchscreen.current?.primaryTouch.position.ReadValue() ?? Vector2.zero;
#else
        return Mouse.current?.position.ReadValue() ?? Vector2.zero;
#endif
    }
}
