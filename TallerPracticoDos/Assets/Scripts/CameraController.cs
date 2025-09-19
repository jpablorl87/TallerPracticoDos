using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

/// <summary>
/// Controla el zoom de la cámara mediante scroll del mouse o gesto "pinch" en pantallas táctiles.
/// También permite desplazar la cámara (pan) cuando se ha hecho zoom.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraZoomController : MonoBehaviour
{
    // --- CONFIGURACIÓN DESDE EL INSPECTOR ---

    [Header("Zoom")]
    [Tooltip("Zoom mínimo permitido (más cercano). Un valor de orthographicSize más pequeño significa más cercano.")]
    [SerializeField] private float minZoom = 2f;

    [Tooltip("Zoom máximo permitido (más alejado). Un valor de orthographicSize más grande significa más alejado.")]
    [SerializeField] private float maxZoom = 5f;

    [Tooltip("Velocidad de zoom al usar scroll del mouse")]
    [SerializeField] private float zoomSpeedMouse = 0.1f;

    [Tooltip("Velocidad de zoom al usar gesto de pinch en pantalla táctil")]
    [SerializeField] private float zoomSpeedTouch = 0.05f;

    [Header("Desplazamiento (Pan)")]
    [Tooltip("Velocidad del movimiento de la cámara al arrastrar")]
    [SerializeField] private float panSpeed = 0.1f;

    [Tooltip("Límites mínimos del área donde puede moverse la cámara")]
    [SerializeField] private Vector2 panLimitMin = new Vector2(-10f, -10f);

    [Tooltip("Límites máximos del área donde puede moverse la cámara")]
    [SerializeField] private Vector2 panLimitMax = new Vector2(10f, 10f);

    [Header("Input Actions")]
    [Tooltip("Asset de InputActions generado por el Input System")]
    [SerializeField] private InputActionAsset inputActionsAsset;

    // --- VARIABLES PRIVADAS ---

    private Camera mainCamera;
    private Vector3 initialPosition; // Posición de la cámara al inicio, fija.

    // Acciones de Input System
    private InputAction zoomMouseAction;
    private InputAction dragAction;

    // Estado del gesto de pinch
    private float previousPinchDistance = 0f;
    private Vector2? lastDragPosition = null;

    // Solo se permite pan si se ha hecho zoom (zoom < maxZoom)
    private bool canPan = false;

    // --- MÉTODOS UNITY ---

    private void Awake()
    {
        mainCamera = GetComponent<Camera>();
        mainCamera.orthographicSize = maxZoom; // La cámara comienza en el punto más alejado.
        initialPosition = transform.position;

        var gameplayMap = inputActionsAsset.FindActionMap("Gameplay");
        zoomMouseAction = gameplayMap.FindAction("ZoomMouse");
        dragAction = gameplayMap.FindAction("Drag");
    }

    private void OnEnable()
    {
        zoomMouseAction.Enable();
        dragAction.Enable();
        zoomMouseAction.performed += OnMouseScrollZoom;
        EnhancedTouchSupport.Enable();
    }

    private void OnDisable()
    {
        zoomMouseAction.performed -= OnMouseScrollZoom;
        zoomMouseAction.Disable();
        dragAction.Disable();
        EnhancedTouchSupport.Disable();
    }

    private void Update()
    {
#if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
        HandleTouchPinchZoom();
#endif
        HandleDragPan();
    }

    // --- ZOOM CON SCROLL DEL MOUSE ---

    private void OnMouseScrollZoom(InputAction.CallbackContext context)
    {
        float scrollValue = context.ReadValue<float>();
        float zoomDelta = -scrollValue * zoomSpeedMouse;
        ApplyZoom(zoomDelta);
    }

    // --- ZOOM CON GESTO PINCH ---

    private void HandleTouchPinchZoom()
    {
        // Obtiene una lista de todos los toques activos en la pantalla.
        var touches = Touch.activeTouches;

        // Solo ejecuta la lógica si hay exactamente dos dedos tocando la pantalla.
        if (touches.Count == 2)
        {
            // 1. Calcula la distancia actual entre el primer y el segundo dedo.
            // Vector2.Distance es una función que mide la longitud de la línea entre dos puntos.
            float currentDistance = Vector2.Distance(touches[0].screenPosition, touches[1].screenPosition);

            // 2. Verifica si ya se había registrado una distancia en el frame anterior.
            // previousPinchDistance es 0f la primera vez que se detectan dos dedos.
            if (previousPinchDistance > 0f)
            {
                // 3. Calcula la diferencia (delta) en la distancia entre el frame actual y el anterior.
                // Si los dedos se están separando, 'delta' será positivo.
                // Si los dedos se están juntando, 'delta' será negativo.
                float delta = currentDistance - previousPinchDistance;

                // 4. Llama a la función ApplyZoom para cambiar el tamaño de la cámara.
                // Multiplicamos 'delta' por la velocidad 'zoomSpeedTouch' para controlar la sensibilidad.
                // El signo negativo (-delta) es crucial:
                // - Un 'delta' positivo (dedos se separan) se convierte en un valor negativo,
                //   lo que reduce el 'orthographicSize' de la cámara, creando un **acercamiento (zoom in)**.
                // - Un 'delta' negativo (dedos se juntan) se convierte en un valor positivo,
                //   lo que aumenta el 'orthographicSize', creando un **alejamiento (zoom out)**.
                ApplyZoom(-delta * zoomSpeedTouch);
            }

            // 5. Guarda la distancia actual para que sea la 'distancia anterior' en el próximo frame.
            previousPinchDistance = currentDistance;
        }
        else
        {
            // 6. Si no hay dos dedos, reinicia la distancia anterior a 0.
            // Esto evita que se aplique un zoom incorrecto si se levanta un dedo.
            previousPinchDistance = 0f;
        }
    }

    // --- MOVIMIENTO DE LA CÁMARA (PAN) ---

    private void HandleDragPan()
    {
        // El pan solo es posible si se ha hecho zoom y canPan es true.
        if (!canPan)
        {
            lastDragPosition = null; // Reiniciar la posición de arrastre si no se puede hacer pan.
            return;
        }

        Vector2 currentPosition = dragAction.ReadValue<Vector2>();

        if (lastDragPosition.HasValue)
        {
            Vector2 delta = currentPosition - lastDragPosition.Value;
            Vector3 panMovement = new Vector3(-delta.x, 0f, -delta.y) * panSpeed * Time.deltaTime;
            Vector3 newPosition = transform.position + panMovement;

            // Limitar la posición dentro de los bordes definidos
            newPosition.x = Mathf.Clamp(newPosition.x, panLimitMin.x, panLimitMax.x);
            newPosition.z = Mathf.Clamp(newPosition.z, panLimitMin.y, panLimitMax.y);

            transform.position = newPosition;
        }

        lastDragPosition = currentPosition;
    }

    // --- APLICACIÓN DEL ZOOM ---

    /// <summary>
    /// Aplica el zoom cambiando el orthographicSize de la cámara, dentro de los límites definidos.
    /// </summary>
    /// <param name="delta">Cantidad de zoom a aplicar (positiva o negativa)</param>
    private void ApplyZoom(float delta)
    {
        float targetSize = mainCamera.orthographicSize + delta;
        float newSize = Mathf.Clamp(targetSize, minZoom, maxZoom);

        // Corregido: Si el zoom está regresando al punto más alejado, 
        // interpole la posición actual hacia la posición inicial para una transición suave.
        if (newSize >= maxZoom)
        {
            float t = Mathf.InverseLerp(mainCamera.orthographicSize, maxZoom, newSize);
            transform.position = Vector3.Lerp(transform.position, initialPosition, t);
            mainCamera.orthographicSize = maxZoom;
            canPan = false;
        }
        else
        {
            mainCamera.orthographicSize = newSize;
            canPan = true;
        }
    }
}