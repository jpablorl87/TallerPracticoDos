using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    private PlayerControls controls;

    [Header("Pause Menu")]
    [SerializeField] private GameObject pauseMenuUI;

    public bool IsPaused { get; private set; } = false;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        controls = new PlayerControls();
        controls.UI.Pause.performed += ctx => TogglePause();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnEnable()
    {
        if (controls != null)
            controls.Enable();
    }

    private void OnDisable()
    {
        if (controls != null)
            controls.Disable();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Detectar menú aunque esté INACTIVO
        var menu = FindAnyObjectByType<PauseMenuUI>(FindObjectsInactive.Include);

        if (menu != null)
            pauseMenuUI = menu.gameObject;
    }

    public void TogglePause()
    {
        if (pauseMenuUI == null) return;

        if (IsPaused) Resume();
        else Pause();
    }

    public void Pause()
    {
        Time.timeScale = 0f;
        IsPaused = true;
        pauseMenuUI.SetActive(true);
    }

    public void Resume()
    {
        Time.timeScale = 1f;
        IsPaused = false;
        pauseMenuUI.SetActive(false);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
