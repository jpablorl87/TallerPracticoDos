using UnityEngine;
using UnityEngine.UI;
using TMPro;  // Si necesitas TMP en algún subcomponente

/// <summary>
/// Clase base para enemigos del minijuego. Contiene lógica común.
/// </summary>
[RequireComponent(typeof(Button))]
public abstract class HuntEnemyBase : MonoBehaviour
{
    [SerializeField] private int coinsWorth = 1; // Cuántas monedas da al ser cazado.

    protected RectTransform rect;

    protected virtual void Awake()
    {
        rect = GetComponent<RectTransform>();
        Button btn = GetComponent<Button>();
        btn.onClick.AddListener(OnClicked);
    }

    protected virtual void OnDestroy()
    {
        Button btn = GetComponent<Button>();
        btn.onClick.RemoveListener(OnClicked);
    }

    private void OnClicked()
    {
        // Si el juego ya terminó, no sumar monedas
        if (!TheHuntGameManager.Instance.GameIsRunning)
            return;

        TheHuntGameManager.Instance.AddCoins(coinsWorth);

        // En lugar de destruir inmediatamente, podrías hacer pooling
        Destroy(gameObject);
    }

    protected abstract void Move();

    // Usamos Update aquí para mover, aunque podrías optimizar luego si usas corutinas o LeanTween
    protected virtual void Update()
    {
        if (TheHuntGameManager.Instance.GameIsRunning)
        {
            Move();
        }
    }
}