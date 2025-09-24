using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UIElements; // Nota: Este using no se utiliza en el código. Puede ser eliminado.

/// <summary>
/// Controla la interacción del usuario con objetos en la escena, gestionando
/// la selección, colocación, movimiento, eliminación y decoración de objetos.
/// </summary>
public class InteractionController : MonoBehaviour
{
    // --- REFERENCIAS Y PREFABS ---

    [Header("References")]
    [SerializeField] private Camera mainCamera; // Referencia a la cámara principal de la escena.
    [SerializeField] private Transform sceneRoot; // Contenedor para los objetos instanciados, mantiene la jerarquía limpia.
    [SerializeField] private UIManager uiManager; // Referencia al script que controla la interfaz de usuario.

    [Header("Prefabs")]
    [SerializeField] private GameObject[] inventoryPrefabs; // Array de prefabs disponibles en el inventario.
    [SerializeField] private GameObject[] decorationPrefabs; // Array de prefabs para decorar objetos.

    // --- VARIABLES DE ESTADO ---

    // Referencia al asset de Input Actions que gestiona los controles.
    private PlayerControls inputActions;

    // Variables para gestionar el estado de la interacción.
    private GameObject selectedInventoryPrefab = null;  // Almacena el prefab seleccionado en el inventario antes de colocarlo.
    private GameObject placedObject = null;             // Referencia al objeto en la escena que está actualmente seleccionado.
    private GameObject ghostObject = null;              // La copia "fantasma" que se mueve antes de confirmar la posición.

    // Flags booleanos para controlar los diferentes estados del juego.
    private bool waitingToPlace = false;    // True cuando se ha seleccionado un objeto del inventario y se espera un clic para colocarlo.
    private bool isMovingObject = false;    // True cuando el usuario ha elegido mover un objeto y está en modo "fantasma".

    private GameObject pendingDecorationPrefab = null; // Prefab de la decoración a colocar, si requiere un tap.
    private bool waitingToDecorate = false;            // True cuando se ha seleccionado una decoración (ej. bola de navidad) y se espera un tap para colocarla.

    // --- MÉTODOS UNITY ---

    // Awake se llama al inicio del ciclo de vida del script.
    private void Awake()
    {
        // Crea una nueva instancia de la clase generada por el Input System.
        inputActions = new PlayerControls();
    }

    // OnEnable se llama cada vez que el objeto se activa.
    private void OnEnable()
    {
        // Habilita el mapa de acciones de "Gameplay".
        inputActions.Gameplay.Enable();
        // Suscribe el método OnTap al evento 'performed' de la acción Tap.
        inputActions.Gameplay.Tap.performed += OnTap;
    }

    // OnDisable se llama cuando el objeto se desactiva.
    private void OnDisable()
    {
        // Desuscribe el método para evitar fugas de memoria y errores.
        inputActions.Gameplay.Tap.performed -= OnTap;
        // Deshabilita el mapa de acciones.
        inputActions.Gameplay.Disable();
    }

    // --- MANEJO DE INPUT ---

    // Este método se ejecuta cada vez que el usuario hace un "tap" o clic.
    private void OnTap(InputAction.CallbackContext ctx)
    {
        // El primer paso es verificar si el tap ocurrió sobre la UI. Si es así,
        // ignoramos el tap para evitar conflictos con los botones.
        if (IsPointerOverUI())
        {
            return;
        }

        // Obtiene la posición del puntero, ya sea del mouse o de la pantalla táctil.
        Vector2 screenPos = GetCurrentPointerPosition();

        // El código usa una serie de "if" para manejar diferentes estados del juego.
        // Solo una de estas condiciones puede ser verdadera a la vez.

        // 1. Lógica para colocar un objeto del inventario.
        if (waitingToPlace && selectedInventoryPrefab != null)
        {
            // Crea un rayo desde la cámara hasta la posición del puntero.
            Ray ray = mainCamera.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                // Si el rayo impacta un objeto, instancia el prefab en ese punto.
                GameObject obj = Instantiate(selectedInventoryPrefab, hit.point, Quaternion.identity, sceneRoot);

                // Asegura que el objeto tenga el componente SelectableObject.
                if (obj.GetComponent<SelectableObject>() == null)
                    obj.AddComponent<SelectableObject>();

                placedObject = obj; // Marca el objeto como el seleccionado.

                // Restablece el estado para que no se coloquen más objetos con un solo tap.
                waitingToPlace = false;
                selectedInventoryPrefab = null;
            }
            return; // Sale del método para no ejecutar el resto de la lógica.
        }

        // 2. Lógica para confirmar la nueva posición de un objeto en movimiento.
        if (isMovingObject && ghostObject != null)
        {
            Ray ray = mainCamera.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                // Mueve el objeto original a la posición del tap y finaliza el modo de movimiento.
                EndMoveAtPosition(hit.point);
            }
            return;
        }

        // 3. Lógica para colocar una decoración en un punto específico del objeto.
        if (waitingToDecorate && pendingDecorationPrefab != null && placedObject != null)
        {
            Ray ray = mainCamera.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                // Verifica que el tap fue sobre el objeto que se desea decorar.
                if (hit.collider.gameObject == placedObject)
                {
                    // Instancia la decoración como hijo del objeto principal para que se mueva con él.
                    GameObject dec = Instantiate(pendingDecorationPrefab, placedObject.transform);

                    // Convierte la posición del impacto del rayo a coordenadas locales del objeto padre.
                    Vector3 localPos = placedObject.transform.InverseTransformPoint(hit.point);
                    dec.transform.localPosition = localPos;
                }
            }

            // Restablece los flags después de colocar la decoración.
            waitingToDecorate = false;
            pendingDecorationPrefab = null;
            return;
        }

        // 4. Lógica para seleccionar un objeto en la escena. Se ejecuta si no estamos en ningún modo especial.
        Ray ray2 = mainCamera.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray2, out RaycastHit hit2))
        {
            // Busca el componente SelectableObject en el objeto impactado.
            SelectableObject so = hit2.collider.gameObject.GetComponent<SelectableObject>();
            if (so != null)
            {
                // Si se encuentra, guarda la referencia al objeto y muestra el panel de opciones.
                placedObject = so.gameObject;
                uiManager.ShowMiniOptions();
                return;
            }
        }
    }

    // ====================
    // MÉTODOS LLAMADOS DESDE UI
    // ====================

    // NOTA: Estos métodos son públicos para que los botones de la UI puedan llamarlos.

    // Se activa al hacer clic en un botón de un objeto del inventario.
    public void OnInventoryItemSelected(int index)
    {
        // Valida que el índice sea válido.
        if (index < 0 || index >= inventoryPrefabs.Length) return;

        // Asigna el prefab seleccionado y activa el modo de espera para colocarlo.
        selectedInventoryPrefab = inventoryPrefabs[index];
        waitingToPlace = true;

        // Oculta todos los paneles de la UI para que la escena sea visible para el usuario.
        uiManager.HideAllPanels();
    }

    // Se activa al hacer clic en el botón de "Mover" en el mini panel de opciones.
    public void OnMoveOptionSelected()
    {
        if (placedObject != null)
        {
            // Desactiva el objeto original y crea un objeto "fantasma" que se moverá.
            placedObject.SetActive(false);
            ghostObject = Instantiate(placedObject, placedObject.transform.position, placedObject.transform.rotation, sceneRoot);

            // Activa el modo de movimiento y oculta los paneles de la UI.
            isMovingObject = true;
            uiManager.HideAllPanels();
        }
    }

    // Se activa al hacer clic en el botón de "Eliminar".
    public void OnDeleteOptionSelected()
    {
        if (placedObject != null)
        {
            // Destruye el objeto seleccionado y limpia la referencia.
            Destroy(placedObject);
            placedObject = null;
        }

        // Oculta los paneles de la UI.
        uiManager.HideAllPanels();
    }

    // Se activa al hacer clic en el botón de "Decorar".
    public void OnDecorateOptionSelected()
    {
        if (placedObject != null)
        {
            // Muestra el panel de decoración para que el usuario elija un ítem.
            uiManager.OpenDecorationPanel();
        }
    }

    // Se activa al hacer clic en un ítem del panel de decoración.
    public void OnDecorationItemSelected(int decorIndex)
    {
        // Valida el índice.
        if (decorIndex < 0 || decorIndex >= decorationPrefabs.Length) return;

        GameObject decorPrefab = decorationPrefabs[decorIndex];

        // Busca el componente SelectableObject para verificar si el objeto puede ser decorado.
        SelectableObject so = placedObject.GetComponent<SelectableObject>();
        if (so != null && so.CanDecorate(decorPrefab.tag))
        {
            // Si el objeto es una "BallDecoration", entra en modo de espera para colocarla.
            if (decorPrefab.CompareTag("BallDecoration"))
            {
                pendingDecorationPrefab = decorPrefab;
                waitingToDecorate = true;
            }
            // Para otras decoraciones (ej. guirnaldas), las coloca inmediatamente en la base del objeto.
            else
            {
                GameObject dec = Instantiate(decorPrefab, placedObject.transform);
                dec.transform.localPosition = Vector3.zero; // Posición local (0,0,0) con respecto al objeto padre.
            }
        }
        else
        {
            Debug.LogWarning("Este objeto no puede ser decorado con esta decoración.");
        }

        // Oculta los paneles de la UI.
        uiManager.HideAllPanels();
    }

    // Se llama desde OnTap para finalizar el proceso de movimiento.
    private void EndMoveAtPosition(Vector3 pos)
    {
        // Restablece el objeto original a la nueva posición y lo activa.
        placedObject.transform.position = pos;
        placedObject.SetActive(true);

        // Destruye el objeto fantasma y limpia las referencias.
        Destroy(ghostObject);
        ghostObject = null;
        isMovingObject = false;
    }

    // ====================
    // FUNCIONES AUXILIARES
    // ====================

    // Verifica si el puntero está sobre un elemento de la UI.
    // Pointer.current?.deviceId ?? -1 maneja tanto el mouse como los toques.
    private bool IsPointerOverUI()
    {
        return EventSystem.current.IsPointerOverGameObject(Pointer.current?.deviceId ?? -1);
    }

    // Obtiene la posición actual del puntero del mouse o del toque.
    private Vector2 GetCurrentPointerPosition()
    {
        // Usa compilación condicional para diferenciar entre plataformas móviles y PC.
#if UNITY_ANDROID || UNITY_IOS
        // Para móviles, lee la posición del toque primario.
        return Touchscreen.current?.primaryTouch.position.ReadValue() ?? Vector2.zero;
#else
        // Para PC, lee la posición del mouse.
        return Mouse.current?.position.ReadValue() ?? Vector2.zero;
#endif
    }
}