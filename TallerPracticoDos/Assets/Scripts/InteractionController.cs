using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UIElements; // Nota: Este using no se utiliza en el c�digo. Puede ser eliminado.

/// <summary>
/// Controla la interacci�n del usuario con objetos en la escena, gestionando
/// la selecci�n, colocaci�n, movimiento, eliminaci�n y decoraci�n de objetos.
/// </summary>
public class InteractionController : MonoBehaviour
{
    // --- REFERENCIAS Y PREFABS ---

    [Header("References")]
    [SerializeField] private Camera mainCamera; // Referencia a la c�mara principal de la escena.
    [SerializeField] private Transform sceneRoot; // Contenedor para los objetos instanciados, mantiene la jerarqu�a limpia.
    [SerializeField] private UIManager uiManager; // Referencia al script que controla la interfaz de usuario.

    [Header("Prefabs")]
    [SerializeField] private GameObject[] inventoryPrefabs; // Array de prefabs disponibles en el inventario.
    [SerializeField] private GameObject[] decorationPrefabs; // Array de prefabs para decorar objetos.

    // --- VARIABLES DE ESTADO ---

    // Referencia al asset de Input Actions que gestiona los controles.
    private PlayerControls inputActions;

    // Variables para gestionar el estado de la interacci�n.
    private GameObject selectedInventoryPrefab = null;  // Almacena el prefab seleccionado en el inventario antes de colocarlo.
    private GameObject placedObject = null;             // Referencia al objeto en la escena que est� actualmente seleccionado.
    private GameObject ghostObject = null;              // La copia "fantasma" que se mueve antes de confirmar la posici�n.

    // Flags booleanos para controlar los diferentes estados del juego.
    private bool waitingToPlace = false;    // True cuando se ha seleccionado un objeto del inventario y se espera un clic para colocarlo.
    private bool isMovingObject = false;    // True cuando el usuario ha elegido mover un objeto y est� en modo "fantasma".

    private GameObject pendingDecorationPrefab = null; // Prefab de la decoraci�n a colocar, si requiere un tap.
    private bool waitingToDecorate = false;            // True cuando se ha seleccionado una decoraci�n (ej. bola de navidad) y se espera un tap para colocarla.

    // --- M�TODOS UNITY ---

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
        // Suscribe el m�todo OnTap al evento 'performed' de la acci�n Tap.
        inputActions.Gameplay.Tap.performed += OnTap;
    }

    // OnDisable se llama cuando el objeto se desactiva.
    private void OnDisable()
    {
        // Desuscribe el m�todo para evitar fugas de memoria y errores.
        inputActions.Gameplay.Tap.performed -= OnTap;
        // Deshabilita el mapa de acciones.
        inputActions.Gameplay.Disable();
    }

    // --- MANEJO DE INPUT ---

    // Este m�todo se ejecuta cada vez que el usuario hace un "tap" o clic.
    private void OnTap(InputAction.CallbackContext ctx)
    {
        // El primer paso es verificar si el tap ocurri� sobre la UI. Si es as�,
        // ignoramos el tap para evitar conflictos con los botones.
        if (IsPointerOverUI())
        {
            return;
        }

        // Obtiene la posici�n del puntero, ya sea del mouse o de la pantalla t�ctil.
        Vector2 screenPos = GetCurrentPointerPosition();

        // El c�digo usa una serie de "if" para manejar diferentes estados del juego.
        // Solo una de estas condiciones puede ser verdadera a la vez.

        // 1. L�gica para colocar un objeto del inventario.
        if (waitingToPlace && selectedInventoryPrefab != null)
        {
            // Crea un rayo desde la c�mara hasta la posici�n del puntero.
            Ray ray = mainCamera.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                // Si el rayo impacta un objeto, instancia el prefab en ese punto.
                GameObject obj = Instantiate(selectedInventoryPrefab, hit.point, Quaternion.identity, sceneRoot);

                // Asegura que el objeto tenga el componente SelectableObject.
                if (obj.GetComponent<SelectableObject>() == null)
                    obj.AddComponent<SelectableObject>();

                placedObject = obj; // Marca el objeto como el seleccionado.

                // Restablece el estado para que no se coloquen m�s objetos con un solo tap.
                waitingToPlace = false;
                selectedInventoryPrefab = null;
            }
            return; // Sale del m�todo para no ejecutar el resto de la l�gica.
        }

        // 2. L�gica para confirmar la nueva posici�n de un objeto en movimiento.
        if (isMovingObject && ghostObject != null)
        {
            Ray ray = mainCamera.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                // Mueve el objeto original a la posici�n del tap y finaliza el modo de movimiento.
                EndMoveAtPosition(hit.point);
            }
            return;
        }

        // 3. L�gica para colocar una decoraci�n en un punto espec�fico del objeto.
        if (waitingToDecorate && pendingDecorationPrefab != null && placedObject != null)
        {
            Ray ray = mainCamera.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                // Verifica que el tap fue sobre el objeto que se desea decorar.
                if (hit.collider.gameObject == placedObject)
                {
                    // Instancia la decoraci�n como hijo del objeto principal para que se mueva con �l.
                    GameObject dec = Instantiate(pendingDecorationPrefab, placedObject.transform);

                    // Convierte la posici�n del impacto del rayo a coordenadas locales del objeto padre.
                    Vector3 localPos = placedObject.transform.InverseTransformPoint(hit.point);
                    dec.transform.localPosition = localPos;
                }
            }

            // Restablece los flags despu�s de colocar la decoraci�n.
            waitingToDecorate = false;
            pendingDecorationPrefab = null;
            return;
        }

        // 4. L�gica para seleccionar un objeto en la escena. Se ejecuta si no estamos en ning�n modo especial.
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
    // M�TODOS LLAMADOS DESDE UI
    // ====================

    // NOTA: Estos m�todos son p�blicos para que los botones de la UI puedan llamarlos.

    // Se activa al hacer clic en un bot�n de un objeto del inventario.
    public void OnInventoryItemSelected(int index)
    {
        // Valida que el �ndice sea v�lido.
        if (index < 0 || index >= inventoryPrefabs.Length) return;

        // Asigna el prefab seleccionado y activa el modo de espera para colocarlo.
        selectedInventoryPrefab = inventoryPrefabs[index];
        waitingToPlace = true;

        // Oculta todos los paneles de la UI para que la escena sea visible para el usuario.
        uiManager.HideAllPanels();
    }

    // Se activa al hacer clic en el bot�n de "Mover" en el mini panel de opciones.
    public void OnMoveOptionSelected()
    {
        if (placedObject != null)
        {
            // Desactiva el objeto original y crea un objeto "fantasma" que se mover�.
            placedObject.SetActive(false);
            ghostObject = Instantiate(placedObject, placedObject.transform.position, placedObject.transform.rotation, sceneRoot);

            // Activa el modo de movimiento y oculta los paneles de la UI.
            isMovingObject = true;
            uiManager.HideAllPanels();
        }
    }

    // Se activa al hacer clic en el bot�n de "Eliminar".
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

    // Se activa al hacer clic en el bot�n de "Decorar".
    public void OnDecorateOptionSelected()
    {
        if (placedObject != null)
        {
            // Muestra el panel de decoraci�n para que el usuario elija un �tem.
            uiManager.OpenDecorationPanel();
        }
    }

    // Se activa al hacer clic en un �tem del panel de decoraci�n.
    public void OnDecorationItemSelected(int decorIndex)
    {
        // Valida el �ndice.
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
                dec.transform.localPosition = Vector3.zero; // Posici�n local (0,0,0) con respecto al objeto padre.
            }
        }
        else
        {
            Debug.LogWarning("Este objeto no puede ser decorado con esta decoraci�n.");
        }

        // Oculta los paneles de la UI.
        uiManager.HideAllPanels();
    }

    // Se llama desde OnTap para finalizar el proceso de movimiento.
    private void EndMoveAtPosition(Vector3 pos)
    {
        // Restablece el objeto original a la nueva posici�n y lo activa.
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

    // Verifica si el puntero est� sobre un elemento de la UI.
    // Pointer.current?.deviceId ?? -1 maneja tanto el mouse como los toques.
    private bool IsPointerOverUI()
    {
        return EventSystem.current.IsPointerOverGameObject(Pointer.current?.deviceId ?? -1);
    }

    // Obtiene la posici�n actual del puntero del mouse o del toque.
    private Vector2 GetCurrentPointerPosition()
    {
        // Usa compilaci�n condicional para diferenciar entre plataformas m�viles y PC.
#if UNITY_ANDROID || UNITY_IOS
        // Para m�viles, lee la posici�n del toque primario.
        return Touchscreen.current?.primaryTouch.position.ReadValue() ?? Vector2.zero;
#else
        // Para PC, lee la posici�n del mouse.
        return Mouse.current?.position.ReadValue() ?? Vector2.zero;
#endif
    }
}