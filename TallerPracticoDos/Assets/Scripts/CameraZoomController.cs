using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

// El atributo [RequireComponent(typeof(Camera))] asegura que el objeto que
// tenga este script tambi�n tenga un componente Camera, lo que evita errores.
public class CameraZoomController : MonoBehaviour
{
    // --- CONFIGURACI�N DESDE EL INSPECTOR ---

    // Las siguientes variables son p�blicas pero serializadas, lo que permite
    // ajustarlas desde el Inspector de Unity. Los atributos [Header] y [Tooltip]
    // mejoran la organizaci�n y la legibilidad en el Inspector.

    [Header("Zoom")]
    [Tooltip("Zoom m�nimo permitido (m�s cercano).")]
    [SerializeField] private float minZoom = 2f;
    [Tooltip("Zoom m�ximo permitido (m�s alejado).")]
    [SerializeField] private float maxZoom = 10f;
    [Tooltip("Velocidad de zoom con el mouse.")]
    [SerializeField] private float zoomSpeedMouse = 0.1f;
    [Tooltip("Velocidad de zoom con gestos t�ctiles.")]
    [SerializeField] private float zoomSpeedTouch = 0.05f;

    [Header("Desplazamiento (Pan)")]
    [Tooltip("Velocidad de movimiento de la c�mara al arrastrar.")]
    [SerializeField] private float panSpeed = 0.005f;
    [Tooltip("L�mites del �rea de movimiento de la c�mara.")]
    [SerializeField] private Vector2 panLimitMin = new Vector2(-10f, -10f);
    [SerializeField] private Vector2 panLimitMax = new Vector2(10f, 10f);

    [Header("Input Actions")]
    [SerializeField] private InputActionAsset inputActionsAsset;

    // --- VARIABLES PRIVADAS ---

    // Referencias a componentes y datos de estado internos del script.
    private Camera mainCamera;
    private Vector3 initialPosition; // Almacena la posici�n inicial de la c�mara.
    private InputAction zoomMouseAction;

    // Acciones de entrada que controlan el arrastre. Se han separado para
    // gestionar de forma independiente la posici�n y el estado del bot�n.
    private InputAction dragPositionAction; // Lee la posici�n del cursor/toque.
    private InputAction dragButtonAction;   // Lee el estado del bot�n (presionado/soltado).

    // Variables de estado para el zoom t�ctil (pinch).
    private float previousPinchDistance = 0f;

    // Variables de estado para el arrastre (pan).
    private Vector2 lastDragPosition; // Almacena la posici�n del cursor en el frame anterior.
    private bool isDragging = false;  // Flag que indica si se est� arrastrando activamente.
    private bool canPan = false;      // Flag que permite el pan solo si se ha hecho zoom.

    // --- M�TODOS UNITY ---

    // Awake se llama una vez al inicio del ciclo de vida del script.
    private void Awake()
    {
        // Obtiene el componente Camera del objeto.
        mainCamera = GetComponent<Camera>();
        // Establece el tama�o de la c�mara en su valor m�ximo al inicio.
        mainCamera.orthographicSize = maxZoom;
        // Guarda la posici�n inicial para poder volver a ella.
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

        // Suscribe los m�todos a los eventos de las acciones de entrada.
        // `performed` se usa para acciones de valor continuo como el scroll.
        zoomMouseAction.performed += OnMouseScrollZoom;

        // `started` y `canceled` se usan para controlar el inicio y fin del arrastre.
        dragButtonAction.started += OnDragStarted;
        dragButtonAction.canceled += OnDragCanceled;

        // Habilita el soporte para gestos t�ctiles mejorados (pinch).
        EnhancedTouchSupport.Enable();
    }

    // OnDisable se llama cuando el objeto se desactiva.
    private void OnDisable()
    {
        // Desuscribe los m�todos para evitar errores y fugas de memoria.
        zoomMouseAction.performed -= OnMouseScrollZoom;
        dragButtonAction.started -= OnDragStarted;
        dragButtonAction.canceled -= OnDragCanceled;

        // Deshabilita todas las acciones de entrada.
        zoomMouseAction.Disable();
        dragPositionAction.Disable();
        dragButtonAction.Disable();

        // Deshabilita el soporte para gestos t�ctiles.
        EnhancedTouchSupport.Disable();
    }

    // Update se llama en cada frame del juego.
    private void Update()
    {
        // Compilaci�n condicional: solo ejecuta el c�digo de los gestos t�ctiles
        // en plataformas m�viles o en el editor.
#if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
        HandleTouchPinchZoom();
#endif
        // Llama al m�todo que maneja el arrastre para el pan de la c�mara.
        HandleDragPan();
    }

    // --- ZOOM ---

    // M�todo que se activa al mover la rueda del mouse.
    private void OnMouseScrollZoom(InputAction.CallbackContext context)
    {
        // Lee el valor del scroll y aplica el zoom.
        float scrollValue = context.ReadValue<float>();
        float zoomDelta = -scrollValue * zoomSpeedMouse;
        ApplyZoom(zoomDelta);
    }

    // M�todo que se activa con el gesto de "pinch" en pantallas t�ctiles.
    private void HandleTouchPinchZoom()
    {
        // Verifica si hay dos toques activos.
        var touches = Touch.activeTouches;
        if (touches.Count == 2)
        {
            // Calcula la distancia entre los dos toques.
            float currentDistance = Vector2.Distance(touches[0].screenPosition, touches[1].screenPosition);
            // Si es el primer frame del gesto, `previousPinchDistance` ser� 0.
            if (previousPinchDistance > 0f)
            {
                // Calcula el cambio en la distancia y aplica el zoom.
                float delta = currentDistance - previousPinchDistance;
                ApplyZoom(-delta * zoomSpeedTouch);
            }
            // Almacena la distancia actual para el pr�ximo frame.
            previousPinchDistance = currentDistance;
        }
        else
        {
            // Reinicia la distancia si no hay dos toques.
            previousPinchDistance = 0f;
        }
    }

    // --- EVENTOS DE ARRASTRE (PAN) ---

    // Se activa cuando se presiona el bot�n o se toca la pantalla.
    private void OnDragStarted(InputAction.CallbackContext context)
    {
        // Lee la posici�n inicial del cursor y activa el flag de arrastre.
        lastDragPosition = dragPositionAction.ReadValue<Vector2>();
        isDragging = true;
    }

    // Se activa cuando se suelta el bot�n o el dedo de la pantalla.
    private void OnDragCanceled(InputAction.CallbackContext context)
    {
        // Desactiva el flag de arrastre.
        isDragging = false;
    }

    // M�todo que gestiona el movimiento de la c�mara en cada frame.
    private void HandleDragPan()
    {
        // Si no se puede hacer pan o no se est� arrastrando, sale del m�todo.
        if (!canPan || !isDragging)
        {
            return;
        }

        // Lee la posici�n actual del cursor.
        Vector2 currentScreenPosition = dragPositionAction.ReadValue<Vector2>();
        // Calcula el cambio de posici�n desde el �ltimo frame.
        Vector2 screenDelta = currentScreenPosition - lastDragPosition;

        // Calcula el movimiento de la c�mara en el espacio del mundo.
        // Se usa `transform.right` y `transform.up` para movimientos relativos a la c�mara,
        // asegurando un pan intuitivo. El signo negativo invierte el movimiento.
        Vector3 panMovement = -transform.right * screenDelta.x * panSpeed + -transform.up * screenDelta.y * panSpeed;

        // Aplica el movimiento y lo limita a los bordes del plano.
        Vector3 newPosition = transform.position + panMovement;
        newPosition.x = Mathf.Clamp(newPosition.x, panLimitMin.x, panLimitMax.x);
        newPosition.z = Mathf.Clamp(newPosition.z, panLimitMin.y, panLimitMax.y);

        transform.position = newPosition;

        // Actualiza la posici�n del �ltimo arrastre para el pr�ximo c�lculo.
        lastDragPosition = currentScreenPosition;
    }

    // --- APLICAR ZOOM ---

    // M�todo que aplica el cambio de zoom a la c�mara.
    private void ApplyZoom(float delta)
    {
        // Calcula el nuevo tama�o y lo limita entre el zoom m�nimo y m�ximo.
        float targetSize = mainCamera.orthographicSize + delta;
        float newSize = Mathf.Clamp(targetSize, minZoom, maxZoom);

        // Si la c�mara vuelve al zoom m�ximo, la devuelve a su posici�n inicial.
        if (newSize >= maxZoom)
        {
            // Calcula un factor de interpolaci�n para un movimiento suave.
            float t = Mathf.InverseLerp(mainCamera.orthographicSize, maxZoom, newSize);
            // Mueve la c�mara suavemente de regreso a su posici�n inicial.
            transform.position = Vector3.Lerp(transform.position, initialPosition, t);
            mainCamera.orthographicSize = maxZoom;
            canPan = false;
        }
        else
        {
            // Si no est� en el zoom m�ximo, simplemente actualiza el tama�o
            // y permite el pan.
            mainCamera.orthographicSize = newSize;
            canPan = true;
        }
    }
}