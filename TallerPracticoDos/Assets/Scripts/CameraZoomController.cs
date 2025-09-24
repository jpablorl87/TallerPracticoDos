using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

// El atributo [RequireComponent(typeof(Camera))] asegura que el objeto que
// tenga este script también tenga un componente Camera, lo que evita errores.
public class CameraZoomController : MonoBehaviour
{
    // --- CONFIGURACIÓN DESDE EL INSPECTOR ---

    // Las siguientes variables son públicas pero serializadas, lo que permite
    // ajustarlas desde el Inspector de Unity. Los atributos [Header] y [Tooltip]
    // mejoran la organización y la legibilidad en el Inspector.

    [Header("Zoom")]
    [Tooltip("Zoom mínimo permitido (más cercano).")]
    [SerializeField] private float minZoom = 2f;
    [Tooltip("Zoom máximo permitido (más alejado).")]
    [SerializeField] private float maxZoom = 10f;
    [Tooltip("Velocidad de zoom con el mouse.")]
    [SerializeField] private float zoomSpeedMouse = 0.1f;
    [Tooltip("Velocidad de zoom con gestos táctiles.")]
    [SerializeField] private float zoomSpeedTouch = 0.05f;

    [Header("Desplazamiento (Pan)")]
    [Tooltip("Velocidad de movimiento de la cámara al arrastrar.")]
    [SerializeField] private float panSpeed = 0.005f;
    [Tooltip("Límites del área de movimiento de la cámara.")]
    [SerializeField] private Vector2 panLimitMin = new Vector2(-10f, -10f);
    [SerializeField] private Vector2 panLimitMax = new Vector2(10f, 10f);

    [Header("Input Actions")]
    [SerializeField] private InputActionAsset inputActionsAsset;

    // --- VARIABLES PRIVADAS ---

    // Referencias a componentes y datos de estado internos del script.
    private Camera mainCamera;
    private Vector3 initialPosition; // Almacena la posición inicial de la cámara.
    private InputAction zoomMouseAction;

    // Acciones de entrada que controlan el arrastre. Se han separado para
    // gestionar de forma independiente la posición y el estado del botón.
    private InputAction dragPositionAction; // Lee la posición del cursor/toque.
    private InputAction dragButtonAction;   // Lee el estado del botón (presionado/soltado).

    // Variables de estado para el zoom táctil (pinch).
    private float previousPinchDistance = 0f;

    // Variables de estado para el arrastre (pan).
    private Vector2 lastDragPosition; // Almacena la posición del cursor en el frame anterior.
    private bool isDragging = false;  // Flag que indica si se está arrastrando activamente.
    private bool canPan = false;      // Flag que permite el pan solo si se ha hecho zoom.

    // --- MÉTODOS UNITY ---

    // Awake se llama una vez al inicio del ciclo de vida del script.
    private void Awake()
    {
        // Obtiene el componente Camera del objeto.
        mainCamera = GetComponent<Camera>();
        // Establece el tamaño de la cámara en su valor máximo al inicio.
        mainCamera.orthographicSize = maxZoom;
        // Guarda la posición inicial para poder volver a ella.
        initialPosition = transform.position;

        // Busca el mapa de acciones de entrada y asigna las acciones a las variables.
        var gameplayMap = inputActionsAsset.FindActionMap("Gameplay");
        zoomMouseAction = gameplayMap.FindAction("ZoomMouse");
        dragPositionAction = gameplayMap.FindAction("DragPosition");
        dragButtonAction = gameplayMap.FindAction("DragButton");
    }

    // OnEnable se llama cada vez que el objeto se activa.
    private void OnEnable()
    {
        // Habilita todas las acciones de entrada.
        zoomMouseAction.Enable();
        dragPositionAction.Enable();
        dragButtonAction.Enable();

        // Suscribe los métodos a los eventos de las acciones de entrada.
        // `performed` se usa para acciones de valor continuo como el scroll.
        zoomMouseAction.performed += OnMouseScrollZoom;

        // `started` y `canceled` se usan para controlar el inicio y fin del arrastre.
        dragButtonAction.started += OnDragStarted;
        dragButtonAction.canceled += OnDragCanceled;

        // Habilita el soporte para gestos táctiles mejorados (pinch).
        EnhancedTouchSupport.Enable();
    }

    // OnDisable se llama cuando el objeto se desactiva.
    private void OnDisable()
    {
        // Desuscribe los métodos para evitar errores y fugas de memoria.
        zoomMouseAction.performed -= OnMouseScrollZoom;
        dragButtonAction.started -= OnDragStarted;
        dragButtonAction.canceled -= OnDragCanceled;

        // Deshabilita todas las acciones de entrada.
        zoomMouseAction.Disable();
        dragPositionAction.Disable();
        dragButtonAction.Disable();

        // Deshabilita el soporte para gestos táctiles.
        EnhancedTouchSupport.Disable();
    }

    // Update se llama en cada frame del juego.
    private void Update()
    {
        // Compilación condicional: solo ejecuta el código de los gestos táctiles
        // en plataformas móviles o en el editor.
#if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
        HandleTouchPinchZoom();
#endif
        // Llama al método que maneja el arrastre para el pan de la cámara.
        HandleDragPan();
    }

    // --- ZOOM ---

    // Método que se activa al mover la rueda del mouse.
    private void OnMouseScrollZoom(InputAction.CallbackContext context)
    {
        // Lee el valor del scroll y aplica el zoom.
        float scrollValue = context.ReadValue<float>();
        float zoomDelta = -scrollValue * zoomSpeedMouse;
        ApplyZoom(zoomDelta);
    }

    // Método que se activa con el gesto de "pinch" en pantallas táctiles.
    private void HandleTouchPinchZoom()
    {
        // Verifica si hay dos toques activos.
        var touches = Touch.activeTouches;
        if (touches.Count == 2)
        {
            // Calcula la distancia entre los dos toques.
            float currentDistance = Vector2.Distance(touches[0].screenPosition, touches[1].screenPosition);
            // Si es el primer frame del gesto, `previousPinchDistance` será 0.
            if (previousPinchDistance > 0f)
            {
                // Calcula el cambio en la distancia y aplica el zoom.
                float delta = currentDistance - previousPinchDistance;
                ApplyZoom(-delta * zoomSpeedTouch);
            }
            // Almacena la distancia actual para el próximo frame.
            previousPinchDistance = currentDistance;
        }
        else
        {
            // Reinicia la distancia si no hay dos toques.
            previousPinchDistance = 0f;
        }
    }

    // --- EVENTOS DE ARRASTRE (PAN) ---

    // Se activa cuando se presiona el botón o se toca la pantalla.
    private void OnDragStarted(InputAction.CallbackContext context)
    {
        // Lee la posición inicial del cursor y activa el flag de arrastre.
        lastDragPosition = dragPositionAction.ReadValue<Vector2>();
        isDragging = true;
    }

    // Se activa cuando se suelta el botón o el dedo de la pantalla.
    private void OnDragCanceled(InputAction.CallbackContext context)
    {
        // Desactiva el flag de arrastre.
        isDragging = false;
    }

    // Método que gestiona el movimiento de la cámara en cada frame.
    private void HandleDragPan()
    {
        // Si no se puede hacer pan o no se está arrastrando, sale del método.
        if (!canPan || !isDragging)
        {
            return;
        }

        // Lee la posición actual del cursor.
        Vector2 currentScreenPosition = dragPositionAction.ReadValue<Vector2>();
        // Calcula el cambio de posición desde el último frame.
        Vector2 screenDelta = currentScreenPosition - lastDragPosition;

        // Calcula el movimiento de la cámara en el espacio del mundo.
        // Se usa `transform.right` y `transform.up` para movimientos relativos a la cámara,
        // asegurando un pan intuitivo. El signo negativo invierte el movimiento.
        Vector3 panMovement = -transform.right * screenDelta.x * panSpeed + -transform.up * screenDelta.y * panSpeed;

        // Aplica el movimiento y lo limita a los bordes del plano.
        Vector3 newPosition = transform.position + panMovement;
        newPosition.x = Mathf.Clamp(newPosition.x, panLimitMin.x, panLimitMax.x);
        newPosition.z = Mathf.Clamp(newPosition.z, panLimitMin.y, panLimitMax.y);

        transform.position = newPosition;

        // Actualiza la posición del último arrastre para el próximo cálculo.
        lastDragPosition = currentScreenPosition;
    }

    // --- APLICAR ZOOM ---

    // Método que aplica el cambio de zoom a la cámara.
    private void ApplyZoom(float delta)
    {
        // Calcula el nuevo tamaño y lo limita entre el zoom mínimo y máximo.
        float targetSize = mainCamera.orthographicSize + delta;
        float newSize = Mathf.Clamp(targetSize, minZoom, maxZoom);

        // Si la cámara vuelve al zoom máximo, la devuelve a su posición inicial.
        if (newSize >= maxZoom)
        {
            // Calcula un factor de interpolación para un movimiento suave.
            float t = Mathf.InverseLerp(mainCamera.orthographicSize, maxZoom, newSize);
            // Mueve la cámara suavemente de regreso a su posición inicial.
            transform.position = Vector3.Lerp(transform.position, initialPosition, t);
            mainCamera.orthographicSize = maxZoom;
            canPan = false;
        }
        else
        {
            // Si no está en el zoom máximo, simplemente actualiza el tamaño
            // y permite el pan.
            mainCamera.orthographicSize = newSize;
            canPan = true;
        }
    }
}