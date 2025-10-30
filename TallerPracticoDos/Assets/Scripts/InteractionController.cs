using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Controla la interacción del usuario con objetos en la escena — selección, colocación, movimiento, eliminación, decoración.
/// Versión corregida: maneja decoración prioritaria, colocación correcta en piso/pared, evita NRE al finalizar movimiento,
/// y hace el flujo ghost/placedObject más robusto.
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
    private GameObject placedObject = null;            // objeto actualmente seleccionado en escena (el "real")
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

        if (isMovingObject && ghostObject == null)
        {
            isMovingObject = false;
            placedObject?.SetActive(true);
            return;
        }

        Vector2 screenPos = GetCurrentPointerPosition();
        Ray ray = mainCamera.ScreenPointToRay(screenPos);
        RaycastHit hit;

        // Raycast general
        if (!Physics.Raycast(ray, out hit))
        {
            if (placedObject != null)
            {
                placedObject = null;
                uiManager.HideAllPanels();
            }
            return;
        }

        // --- Finalizar movimiento ---
        if (isMovingObject && ghostObject != null)
        {
            if (!IsPlacementValid(placedObject, hit)) return;

            bool validPlacement = TryPlaceOnGhost(ghostObject, hit);
            if (!validPlacement) return;

            placedObject.transform.position = ghostObject.transform.position;
            placedObject.transform.rotation = ghostObject.transform.rotation;

            Physics.SyncTransforms();
            placedObject.SetActive(true);

            Destroy(ghostObject);
            ghostObject = null;

            isMovingObject = false;
            waitingToPlaceBase = false;
            waitingToDecorate = false;
            pendingDecorationPrefab = null;

            uiManager.HideAllPanels();
            Debug.Log("[Move] Reubicación completada.");
            return;
        }

        // --- Colocar nuevo objeto ---
        if (waitingToPlaceBase && selectedBasePrefab != null)
        {
            SelectableObject soPrefab = selectedBasePrefab.GetComponent<SelectableObject>();
            Collider itemCollider = selectedBasePrefab.GetComponent<Collider>();
            if (itemCollider == null || soPrefab == null) return;
            if (!IsPlacementValid(selectedBasePrefab, hit)) return;

            GameObject obj = Instantiate(selectedBasePrefab, hit.point, Quaternion.identity, sceneRoot);
            ApplyPlacement(obj, hit);
            if (obj.GetComponent<SelectableObject>() == null)
                obj.AddComponent<SelectableObject>();

            placedObject = obj;
            waitingToPlaceBase = false;
            selectedBasePrefab = null;
            Debug.Log("[Placement] Objeto colocado: " + obj.name);
            return;
        }

        // --- Decorar objeto existente ---
        if (waitingToDecorate && pendingDecorationPrefab != null)
        {
            SelectableObject soHit = hit.collider.GetComponentInParent<SelectableObject>();
            if (soHit != null && soHit.CanDecorate(pendingDecorationPrefab.tag))
            {
                GameObject dec = Instantiate(pendingDecorationPrefab, soHit.transform);
                Vector3 localPos = soHit.transform.InverseTransformPoint(hit.point);
                dec.transform.localPosition = localPos;
                dec.transform.rotation = Quaternion.LookRotation(-hit.normal, Vector3.up);

                waitingToDecorate = false;
                waitingToPlaceBase = false;
                isMovingObject = false;
                pendingDecorationPrefab = null;
                uiManager.HideAllPanels();

                Debug.Log("[Decorate] Decoración aplicada.");
                return;
            }

            waitingToDecorate = false;
            pendingDecorationPrefab = null;
            uiManager.HideAllPanels();
            return;
        }

        // --- Seleccionar objeto existente ---
        SelectableObject soSelected = hit.collider.GetComponentInParent<SelectableObject>();
        if (soSelected != null)
        {
            placedObject = soSelected.gameObject;
            uiManager.ShowMiniOptions();
            Debug.Log("[Tap] Objeto seleccionado: " + placedObject.name);
            return;
        }

        // --- Clic fuera ---
        if (placedObject != null)
        {
            placedObject = null;
            uiManager.HideAllPanels();
            Debug.Log("[Tap] Clic fuera.");
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
        // Si ya estamos en modo mover, salir
        if (isMovingObject) return;

        // Verificar que haya un objeto seleccionado
        if (placedObject == null)
        {
            Debug.LogWarning("[Move] No hay objeto seleccionado para mover.");
            return;
        }

        // Siempre asegurarse de usar la raíz que tiene el SelectableObject
        SelectableObject so = placedObject.GetComponentInParent<SelectableObject>();
        if (so == null)
        {
            Debug.LogWarning("[Move] El objeto seleccionado no tiene SelectableObject.");
            return;
        }

        GameObject root = so.gameObject;

        // Log para depurar qué objeto realmente va a moverse
        Debug.Log("[Move] Solicitado mover objeto: " + root.name +
                  " (desde placedObject = " + placedObject.name + ")");

        // Cerrar el minioptions menu automáticamente
        uiManager.HideAllPanels();

        // Crear el ghost a partir del objeto raíz
        ghostObject = Instantiate(
            root,
            root.transform.position,
            root.transform.rotation,
            sceneRoot
        );

        // Desactivar el objeto real mientras el usuario elige la nueva posición
        root.SetActive(false);

        // Asegurarnos de que placedObject apunte al objeto raíz
        placedObject = root;

        // Activar modo mover
        isMovingObject = true;

        // Log adicional para confirmar activación
        Debug.Log("[Move] Modo mover ACTIVADO. Ghost creado: " +
                  ghostObject.name + " | Original desactivado: " + placedObject.name);
    }

    public void OnDeleteOptionSelected()
    {
        // Cerrar UI primero
        uiManager.HideAllPanels();

        // Si hay ghost activo -> destruirlo
        if (ghostObject != null)
        {
            Destroy(ghostObject);
            ghostObject = null;
        }

        // Si hay placedObject -> destruirlo y limpiar
        if (placedObject != null)
        {
            Destroy(placedObject);
            placedObject = null;
        }

        // Limpieza de flags
        isMovingObject = false;
        waitingToPlaceBase = false;
        waitingToDecorate = false;
        pendingDecorationPrefab = null;
        ghostObject = null;

        Debug.Log("[Delete] objeto eliminado y estado limpiado.");
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

    // --- NUEVO: EndMove recibe el hit ya calculado ---
    private void EndMoveAtPosition(RaycastHit hit)
    {
        if (!isMovingObject)
            return;

        // Si por algún motivo el objeto real no existe, usamos el ghost
        if (placedObject == null && ghostObject != null)
        {
            placedObject = ghostObject;
            ghostObject = null;
        }

        if (placedObject == null)
        {
            Debug.LogWarning("[Move] No hay objeto válido para finalizar movimiento.");
            isMovingObject = false;
            return;
        }

        //  Reactivar el objeto real y aplicar colocación en la nueva posición
        placedObject.SetActive(true);
        ApplyPlacement(placedObject, hit);

        //  Eliminar el ghost
        if (ghostObject != null)
        {
            Destroy(ghostObject);
            ghostObject = null;
        }

        //  Limpiar estados
        isMovingObject = false;
        waitingToPlaceBase = false;
        waitingToDecorate = false;
        pendingDecorationPrefab = null;

        //  Ocultar cualquier panel abierto
        uiManager.HideAllPanels();

        Debug.Log("[Move] Objeto reubicado correctamente.");
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
        SelectableObject soPrefab = prefab.GetComponent<SelectableObject>();
        if (soPrefab == null) return true; // Si no tiene reglas, permitir por defecto

        Vector3 normal = hit.normal.normalized;
        float floorTolerance = 0.1f;
        bool isFloor = Vector3.Dot(normal, Vector3.up) > 1f - floorTolerance;

        switch (soPrefab.allowedSurface)
        {
            case PlacementSurface.FloorOnly:
                return isFloor;
            case PlacementSurface.WallOnly:
                return !isFloor;
            case PlacementSurface.AnySurface:
            default:
                return true;
        }
    }

    /// <summary>
    /// Calcula y aplica la posición y rotación correctas a un objeto instanciado o movido,
    /// aplicando el offset necesario para que el borde toque el hit.point.
    /// </summary>
    private void ApplyPlacement(GameObject obj, RaycastHit hit)
    {
        SelectableObject so = obj.GetComponent<SelectableObject>();
        Collider placedCollider = obj.GetComponent<Collider>() ?? obj.GetComponentInChildren<Collider>();
        if (so == null || placedCollider == null)
        {
            Debug.LogWarning("ApplyPlacement: SelectableObject o Collider no encontrado en el objeto.");
            return;
        }

        // --- 1. Calcular rotación y posición base según orientación ---
        Quaternion targetRotation;
        bool isFloor = Vector3.Dot(hit.normal, Vector3.up) > 0.9f;

        if (so.placementOrientation == PlacementOrientation.Vertical)
        {
            // Piso
            targetRotation = Quaternion.Euler(0, obj.transform.eulerAngles.y, 0);
            obj.transform.rotation = targetRotation;

            // Colocamos el centro en el punto de impacto temporalmente
            obj.transform.position = hit.point;

            // Offset vertical (usar bounds extents en Y en world space)
            float offsetY = placedCollider.bounds.extents.y;
            obj.transform.position += hit.normal * offsetY;

            Debug.DrawRay(hit.point, hit.normal * 0.3f, Color.red, 2f);
        }
        else
        {
            // Pared
            targetRotation = Quaternion.LookRotation(-hit.normal, Vector3.up);
            obj.transform.rotation = targetRotation;

            // Colocar el centro en el punto de impacto
            obj.transform.position = hit.point;

            // Intentar corregir con ClosestPoint
            Vector3 closest = placedCollider.ClosestPoint(hit.point);
            Vector3 correction = hit.point - closest;

            // Si la corrección es mínima, usamos fallback con bounds
            if (correction.sqrMagnitude < 1e-6f)
            {
                Vector3 extents = placedCollider.bounds.extents;
                float depth = Mathf.Abs(Vector3.Dot(extents, hit.normal));
                correction = hit.normal * depth;
            }

            // Aplicar corrección (empujar hacia afuera)
            obj.transform.position += correction;
            obj.transform.position += hit.normal * 0.001f;

            Debug.DrawRay(hit.point, hit.normal * 0.3f, Color.red, 2f);
            Debug.DrawLine(hit.point, closest, Color.green, 2f);
        }

        // --- 2. Verificar colisiones con otros objetos antes de finalizar ---
        // (Ignoramos el piso y paredes, sólo bloqueamos si choca con otros objetos decorativos)

        Bounds worldBounds = placedCollider.bounds;
        Vector3 center = worldBounds.center;
        Vector3 halfExtents = worldBounds.extents;

        // Obtenemos todos los colliders cercanos
        Collider[] hits = Physics.OverlapBox(
            center,
            halfExtents,
            obj.transform.rotation,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore
        );

        int blockingCount = 0;
        foreach (var h in hits)
        {
            if (h == placedCollider)
                continue; // ignorar su propio collider

            // Si el collider pertenece a una superficie (piso/pared), lo ignoramos
            if (h.CompareTag("Floor") || h.CompareTag("Wall"))
                continue;

            // Si es otro objeto seleccionable (mueble, decoración, etc.), cuenta como colisión bloqueante
            if (h.GetComponentInParent<SelectableObject>() != null)
            {
                blockingCount++;
            }
        }

        if (blockingCount > 0)
        {
            Debug.Log($"[ApplyPlacement] Colisión con {blockingCount} objeto(s) decorativo(s). Cancelando colocación.");

            // Si es un movimiento, mantener ghost para intentar otra posición
            if (isMovingObject)
            {
                obj.SetActive(false);
                uiManager.HideAllPanels();
                Debug.Log("[ApplyPlacement] El objeto no puede colocarse aquí, intente otra posición.");
            }
            else
            {
                // Si es una nueva instancia, destruirla directamente
                Destroy(obj);
            }

            return;
        }
    }
    /// <summary>
    /// Intenta posicionar (sin destruir) el objeto `obj` en el hit.
    /// Devuelve true si la colocación es válida y no colisiona con otros muebles.
    /// En caso de éxito deja la posición y rotación aplicadas en el objeto.
    /// En caso de fallo el objeto queda en la posición calculada pero NO se reactiva (si estaba inactivo).
    /// </summary>
    private bool TryPlace(GameObject obj, RaycastHit hit)
    {
        if (obj == null) return false;

        SelectableObject so = obj.GetComponent<SelectableObject>();
        Collider placedCollider = obj.GetComponent<Collider>() ?? obj.GetComponentInChildren<Collider>();
        if (so == null || placedCollider == null) return false;

        // --- 1. Calcular rotación y posición base ---
        Quaternion targetRotation;
        bool isFloor = Vector3.Dot(hit.normal, Vector3.up) > 0.9f;

        if (so.placementOrientation == PlacementOrientation.Vertical)
        {
            // Piso
            targetRotation = Quaternion.Euler(0, obj.transform.eulerAngles.y, 0);
            obj.transform.rotation = targetRotation;

            obj.transform.position = hit.point;

            // Mover hacia arriba en la dirección de la normal
            float offsetY = placedCollider.bounds.extents.y;
            obj.transform.position += hit.normal * offsetY;
        }
        else
        {
            // Pared
            targetRotation = Quaternion.LookRotation(-hit.normal, Vector3.up);
            obj.transform.rotation = targetRotation;
            obj.transform.position = hit.point;

            // Calcular punto más cercano
            Vector3 closest = placedCollider.ClosestPoint(hit.point);
            Vector3 correction = hit.point - closest;

            // Si la corrección es mínima, usar fallback con bounds
            if (correction.sqrMagnitude < 1e-6f)
            {
                Vector3 extents = placedCollider.bounds.extents;
                float depth = Mathf.Abs(Vector3.Dot(extents, hit.normal));
                correction = hit.normal * depth;
            }

            obj.transform.position += correction;
            obj.transform.position += hit.normal * 0.001f;
        }

        // --- 2. Forzar actualización del collider ---
        // Esto recalcula los bounds con la nueva posición/rotación antes de hacer OverlapBox
        Physics.SyncTransforms();

        // --- 3. Comprobar colisiones con otros objetos (ignorar piso/pared) ---
        Bounds worldBounds = placedCollider.bounds;
        Vector3 center = worldBounds.center;
        Vector3 halfExtents = worldBounds.extents;

        Collider[] hits = Physics.OverlapBox(
            center,
            halfExtents,
            obj.transform.rotation,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore
        );

        int blockingCount = 0;
        foreach (var h in hits)
        {
            if (h == placedCollider) continue;
            if (h.CompareTag("Floor") || h.CompareTag("Wall")) continue;

            if (h.GetComponentInParent<SelectableObject>() != null)
                blockingCount++;
        }

        if (blockingCount > 0)
        {
            Debug.Log("[TryPlace] Colisión con " + blockingCount + " objetos. Colocación rechazada.");
            return false;
        }

        Debug.Log("[TryPlace] Colocación válida.");
        return true;
    }
    /// <summary>
    /// Intenta posicionar el ghost (activo) en la ubicación determinada por hit.
    /// Devuelve true si la posición candidate es válida (sin colisión con otros SelectableObject).
    /// NO destruye el ghost; solo lo mueve para validación.
    /// </summary>
    private bool TryPlaceOnGhost(GameObject ghost, RaycastHit hit)
    {
        if (ghost == null) return false;

        ghost.SetActive(true);

        SelectableObject so = ghost.GetComponent<SelectableObject>();
        Collider ghostCollider = ghost.GetComponent<Collider>() ?? ghost.GetComponentInChildren<Collider>();
        if (so == null || ghostCollider == null) return false;

        Quaternion targetRotation;
        bool isFloor = Vector3.Dot(hit.normal, Vector3.up) > 0.9f;

        if (so.placementOrientation == PlacementOrientation.Vertical)
        {
            targetRotation = Quaternion.Euler(0, ghost.transform.eulerAngles.y, 0);
            ghost.transform.rotation = targetRotation;
            ghost.transform.position = hit.point + hit.normal * ghostCollider.bounds.extents.y;
        }
        else
        {
            targetRotation = Quaternion.LookRotation(-hit.normal, Vector3.up);
            ghost.transform.rotation = targetRotation;
            ghost.transform.position = hit.point;

            Vector3 closest = ghostCollider.ClosestPoint(hit.point);
            Vector3 correction = hit.point - closest;
            if (correction.sqrMagnitude < 1e-6f)
            {
                Vector3 extents = ghostCollider.bounds.extents;
                float depth = Mathf.Abs(Vector3.Dot(extents, hit.normal));
                correction = hit.normal * depth;
            }

            ghost.transform.position += correction + hit.normal * 0.001f;
        }

        Physics.SyncTransforms();

        Bounds b = ghostCollider.bounds;
        Collider[] hits = Physics.OverlapBox(b.center, b.extents, ghost.transform.rotation, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

        SelectableObject rootSo = ghost.GetComponentInParent<SelectableObject>();
        int blockingCount = 0;

        foreach (var h in hits)
        {
            if (h == ghostCollider) continue;
            if (h.CompareTag("Floor") || h.CompareTag("Wall")) continue;

            SelectableObject otherSo = h.GetComponentInParent<SelectableObject>();
            if (otherSo != null && otherSo != rootSo)
                blockingCount++;

            DecorationMarker otherDecor = h.GetComponentInParent<DecorationMarker>();
            if (otherDecor != null)
            {
                SelectableObject decorParent = otherDecor.GetComponentInParent<SelectableObject>();
                if (decorParent != null && decorParent != rootSo)
                    blockingCount++;
            }
        }

        if (blockingCount > 0)
        {
            Debug.Log("[TryPlaceOnGhost] Colisión con " + blockingCount + " objetos.");
            return false;
        }

        return true;
    }

}
