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
        Ray ray = mainCamera.ScreenPointToRay(screenPos);
        RaycastHit hit;

        // 0. FINALIZACIÓN DE REUBICACIÓN (Tap para soltar)
        if (isMovingObject && ghostObject != null)
        {
            if (Physics.Raycast(ray, out hit))
            {
                EndMoveAtPosition(hit.point);
                return;
            }
        }

        // 1. COLOCACIÓN INICIAL (Colocar un nuevo prefab)
        if (waitingToPlaceBase && selectedBasePrefab != null)
        {
            if (Physics.Raycast(ray, out hit /*, Mathf.Infinity, surfaceLayer */))
            {
                // ... (Lógica de validación) ...
                SelectableObject soPrefab = selectedBasePrefab.GetComponent<SelectableObject>();
                Collider itemCollider = selectedBasePrefab.GetComponent<Collider>();

                if (itemCollider == null || soPrefab == null) { /* Error */ return; }
                if (!IsPlacementValid(selectedBasePrefab, hit)) { /* Warning */ return; }

                GameObject obj = Instantiate(selectedBasePrefab, hit.point, Quaternion.identity, sceneRoot);
                ApplyPlacement(obj, hit); // Aplicar offset y rotación

                if (obj.GetComponent<SelectableObject>() == null)
                {
                    obj.AddComponent<SelectableObject>();
                }
                placedObject = obj;

                waitingToPlaceBase = false;
                selectedBasePrefab = null;
                return;
            }
        }

        // 2. INICIAR REUBICACIÓN (Seleccionar un objeto existente)
        if (placedObject != null && !isMovingObject)
        {
            if (Physics.Raycast(ray, out hit) && hit.collider.gameObject == placedObject)
            {
                isMovingObject = true;
                placedObject.SetActive(false);

                ghostObject = Instantiate(placedObject, placedObject.transform.position, placedObject.transform.rotation, sceneRoot);

                // RESTAURACIÓN DE UI
                uiManager.ShowMiniOptions();
                return;
            }
        }

        // 4. Deseleccionar
        if (placedObject != null && !isMovingObject)
        {
            placedObject = null;
            uiManager.HideAllPanels();
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
        // Re-lanza el Raycast en el punto final para obtener la normal (hit.normal) correcta.
        Ray ray = mainCamera.ScreenPointToRay(GetCurrentPointerPosition());
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            placedObject.SetActive(true);

            // Llama a la lógica de offset y rotación con el hit.point final
            ApplyPlacement(placedObject, hit);

            // Limpieza
            Destroy(ghostObject);
            ghostObject = null;
            isMovingObject = false;
        }
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
    /// <summary>
    /// Verifica si el objeto se puede colocar en la superficie golpeada por el Raycast.
    /// </summary>
    private bool IsPlacementValid(GameObject prefab, RaycastHit hit)
    {
        // Se asume que el prefab tiene el componente SelectableObject para leer la regla.
        SelectableObject soPrefab = prefab.GetComponent<SelectableObject>();
        if (soPrefab == null) return true; // Si no tiene reglas, permitir por defecto

        // Obtiene la normal de la superficie golpeada.
        Vector3 normal = hit.normal.normalized;

        // Utilizamos el producto punto (Dot product) para determinar si la superficie es horizontal (piso).
        // Vector3.up (0, 1, 0) vs normal. Si es cercano a 1, es un piso.
        float floorTolerance = 0.1f; // Pequeña tolerancia para inclinaciones
        bool isFloor = Vector3.Dot(normal, Vector3.up) > 1f - floorTolerance;

        switch (soPrefab.allowedSurface)
        {
            case PlacementSurface.FloorOnly:
                return isFloor;
            case PlacementSurface.WallOnly:
                // Consideramos pared cualquier superficie que no sea el piso o que sea casi vertical.
                return !isFloor;
            case PlacementSurface.AnySurface:
            default:
                return true; // Permitir en cualquier lugar
        }
    }
    /// <summary>
    /// Calcula y aplica la posición y rotación correctas a un objeto instanciado o movido, 
    /// aplicando el offset necesario para que el borde toque el hit.point.
    /// </summary>
    private void ApplyPlacement(GameObject obj, RaycastHit hit)
    {
        SelectableObject so = obj.GetComponent<SelectableObject>();
        Collider placedCollider = obj.GetComponent<Collider>();

        if (so == null || placedCollider == null) return;

        // 1. Calcular la Rotación FINAL
        Quaternion targetRotation;

        if (so.placementOrientation == PlacementOrientation.Vertical)
        {
            // CASO PISO
            targetRotation = Quaternion.identity;
        }
        else // PlacementOrientation.Horizontal (Pared)
        {
            // CASO PARED: Z mira hacia afuera de la pared
            targetRotation = Quaternion.LookRotation(-hit.normal, Vector3.up);

            // Forzar rotación en Y a 0, 90, 180 o 270 grados
            float yAngle = targetRotation.eulerAngles.y;
            float roundedYAngle = Mathf.Round(yAngle / 90f) * 90f;
            targetRotation = Quaternion.Euler(targetRotation.eulerAngles.x, roundedYAngle, targetRotation.eulerAngles.z);
        }

        // Aplicar la rotación final
        obj.transform.rotation = targetRotation;

        // Mover el objeto al punto de impacto (hit.point) temporalmente
        obj.transform.position = hit.point;

        // 2. Calcular Offset y Reposicionamiento
        float offsetDepth;

        if (so.placementOrientation == PlacementOrientation.Vertical)
        {
            // CASO PISO: Altura del Collider del Mundo (Y-extents). Funciona bien.
            offsetDepth = placedCollider.bounds.extents.y;

            // Elevar la posición central por el offset
            obj.transform.position += hit.normal * offsetDepth;

            // Asegurar que la rotación sea plana (X=0, Z=0)
            obj.transform.rotation = Quaternion.Euler(0, targetRotation.eulerAngles.y, 0);
        }
        else // Pared
        {
            // *** CORRECCIÓN CLAVE DE FLOTACIÓN EN PARED ***
            BoxCollider boxCollider = obj.GetComponent<BoxCollider>();

            if (boxCollider != null)
            {
                // La profundidad del objeto es la mitad de su tamaño local en el EJE Z (profundidad).
                // boxCollider.size.z * 0.5f * localScale.z
                offsetDepth = boxCollider.size.z * 0.5f * obj.transform.localScale.z;
            }
            else
            {
                // Fallback (Si no hay BoxCollider): Usamos el bounds del mundo
                // El bounds.extents nos da la mitad del tamaño AABB (alineado al mundo)
                // Usamos Dot Product para proyectar la distancia MÁXIMA del centro al borde
                // en la dirección de la normal.
                offsetDepth = Mathf.Abs(Vector3.Dot(placedCollider.bounds.extents, hit.normal));
            }

            // Reposicionar: Mover el centro del objeto hacia AFUERA de la pared
            obj.transform.position -= hit.normal * offsetDepth;
        }
    }
}
