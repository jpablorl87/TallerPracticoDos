using UnityEngine;

/// <summary>
/// Mide el desempeño del jugador y calcula un valor de "habilidad dinámica"
/// que afecta el comportamiento global del juego.
/// </summary>
public class PlayerMetrics : MonoBehaviour
{
    [Header("Performance Data")]
    [Tooltip("Promedio de rendimiento general (0 a 1).")]
    [Range(0f, 1f)] public float performanceScore = 0.5f;

    [Tooltip("Qué tan rápido se ajusta el rendimiento al promedio.")]
    [Range(0.01f, 1f)] public float smoothing = 0.3f;

    [Tooltip("Velocidad con la que baja el rendimiento si no hay actividad.")]
    public float decayRate = 0.005f;

    private float lastUpdateTime;

    private void Update()
    {
        // Si el jugador no ha jugado por un rato, el rendimiento baja lentamente
        if (Time.time - lastUpdateTime > 5f)
        {
            performanceScore = Mathf.MoveTowards(performanceScore, 0.4f, decayRate * Time.deltaTime);
        }
    }

    /// <summary>
    /// Actualiza la métrica de rendimiento según las monedas obtenidas en un minijuego.
    /// </summary>
    public void RegisterGameSession(float coinsEarned)
    {
        lastUpdateTime = Time.time;
        float normalizedCoins = Mathf.Clamp01(coinsEarned / 100f);
        performanceScore = Mathf.Lerp(performanceScore, normalizedCoins, smoothing);
        Debug.Log($"[PlayerMetrics] Nueva puntuación: {performanceScore:F2}");
    }

    /// <summary>
    /// Devuelve un multiplicador que representa la habilidad del jugador (0.5 a 3).
    /// </summary>
    public float GetPlayerSkillLevel()
    {
        return Mathf.Clamp(0.5f + performanceScore * 2f, 0.5f, 3f);
    }
}
