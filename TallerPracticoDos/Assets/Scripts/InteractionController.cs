using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Controla la interacción del usuario con objetos en la escena — selección, colocación, movimiento, eliminación, decoración.
/// </summary>
public class InteractionController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform sceneRoot;  // donde se crean los objetos base en el plano
    [SerializeField] private UIManager uiManager;

    [Header("Prefabs")]
    [SerializeField] private GameObject[] decorationPrefabs;  // prefabs que son decoraciones

    // Estado interno
    private PlayerControls inputActions;

    private GameObject selectedBasePrefab = null;     // objeto base seleccionado para colocar libremente
    private GameObject placedObject = null;            // objeto actualmente seleccionado en escena
    private GameObject ghostObject = null;             // objeto temporal al mover

    private bool waitingToPlaceBase = false;
    private bool isMovingObject = false;

    private GameObject pendingDecorationPrefab = null;  // decoración seleccionada para colocar en objeto existente
    private bool waitingToDecorate = false;

    public GameObject[] DecorationPrefabs => decorationPrefabs;

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
        if (IsPointerOverUI()) return;

        Vector2 screenPos = GetCurrentPointerPosition();

        // 1. Si estamos en modo colocar un objeto base
        if (waitingToPlaceBase && selectedBasePrefab != null)
        {
            Ray ray = mainCamera.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                GameObject obj = Instantiate(selectedBasePrefab, hit.point, Quaternion.identity, sceneRoot);
                if (obj.GetComponent<SelectableObject>() == null)
                {
                    obj.AddComponent<SelectableObject>();
                }
                placedObject = obj;

                waitingToPlaceBase = false;
                selectedBasePrefab = null;
            }
            return;
        }

        // 2. Si estamos moviendo un objeto existente (modo fantasma)
        if (isMovingObject && ghostObject != null)
        {
            Ray ray = mainCamera.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                EndMoveAtPosition(hit.point);
            }
            return;
        }

        // 3. Si estamos en modo decoración: esperamos que el tap sea sobre el objeto padre
        if (waitingToDecorate && pendingDecorationPrefab != null && placedObject != null)
        {
            Ray ray = mainCamera.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider.gameObject == placedObject)
                {
                    GameObject dec = Instantiate(pendingDecorationPrefab, placedObject.transform);
                    Vector3 localPos = placedObject.transform.InverseTransformPoint(hit.point);
                    dec.transform.localPosition = localPos;
                }
            }
            waitingToDecorate = false;
            pendingDecorationPrefab = null;
            return;
        }

        // 4. Si no estamos en ninguno de los modos especiales, chequear selección de objeto en escena
        Ray ray2 = mainCamera.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray2, out RaycastHit hit2))
        {
            SelectableObject so = hit2.collider.gameObject.GetComponent<SelectableObject>();
            if (so != null)
            {
                placedObject = so.gameObject;
                uiManager.ShowMiniOptions();
                return;
            }
        }
    }

    /// <summary>
    /// Llamado desde UI (InventoryUI) al hacer clic en un ítem del inventario.
    /// El índice corresponde a la lista de prefabs del inventario.
    /// </summary>
    public void OnInventoryItemSelected(int index)
    {
        var items = InventoryManager.Instance.GetItems();
        if (index < 0 || index >= items.Count) return;

        GameObject prefab = items[index];

        // Verificar si el prefab es decoración o base
        bool isDecor = false;
        for (int i = 0; i < decorationPrefabs.Length; i++)
        {
            if (prefab == decorationPrefabs[i])
            {
                isDecor = true;
                break;
            }
        }

        if (isDecor)
        {
            // Entrar en modo decoración
            pendingDecorationPrefab = prefab;
            waitingToDecorate = true;
        }
        else
        {
            // Modo colocar libre
            selectedBasePrefab = prefab;
            waitingToPlaceBase = true;
        }

        uiManager.HideAllPanels();
    }

    public void OnMoveOptionSelected()
    {
        if (placedObject != null)
        {
            placedObject.SetActive(false);
            ghostObject = Instantiate(placedObject, placedObject.transform.position, placedObject.transform.rotation, sceneRoot);
            isMovingObject = true;
            uiManager.HideAllPanels();
        }
    }

    public void OnDeleteOptionSelected()
    {
        if (placedObject != null)
        {
            Destroy(placedObject);
            placedObject = null;
        }
        uiManager.HideAllPanels();
    }

    public void OnDecorationItemSelected(int decorIndex)
    {
        if (decorIndex < 0 || decorIndex >= decorationPrefabs.Length) return;

        GameObject decorPrefab = decorationPrefabs[decorIndex];

        SelectableObject so = placedObject?.GetComponent<SelectableObject>();
        if (so != null && so.CanDecorate(decorPrefab.tag))
        {
            // Si es un tipo que requiere posicionamiento al hacer click
            if (decorPrefab.CompareTag("BallDecoration"))
            {
                waitingToDecorate = true;
                pendingDecorationPrefab = decorPrefab;
            }
            else
            {
                GameObject dec = Instantiate(decorPrefab, placedObject.transform);
                dec.transform.localPosition = Vector3.zero;
            }
        }
        else
        {
            Debug.LogWarning("Decoración no permitida para este objeto.");
        }
        uiManager.HideAllPanels();
    }

    private void EndMoveAtPosition(Vector3 pos)
    {
        placedObject.transform.position = pos;
        placedObject.SetActive(true);
        Destroy(ghostObject);
        ghostObject = null;
        isMovingObject = false;
    }

    private bool IsPointerOverUI()
    {
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
