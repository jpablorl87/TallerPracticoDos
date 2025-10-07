using UnityEngine;
using System.Collections;

/// <summary>
/// Se encarga de spawnear enemigos de forma aleatoria dentro de un área visible de la UI.
/// Esta clase usa un RectTransform como referencia para posicionar correctamente a los enemigos en pantalla.
/// Cuando el juego termina, deja de spawnear enemigos.
/// </summary>
public class HuntEnemySpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("Área en la que los enemigos pueden aparecer (RectTransform UI).")]
    [SerializeField] private RectTransform spawnArea;
    [Tooltip("Tiempo entre la aparición de enemigos.")]
    [SerializeField] private float spawnInterval = 0.5f;

    [Header("Enemy Prefabs")]
    [Tooltip("Prefabs de enemigos posibles: cucaracha, mosca, ratón, etc.")]
    [SerializeField] private GameObject[] enemyPrefabs;

    private bool spawning = false;

    private void Start()
    {
        StartSpawning();
    }

    /// <summary>
    /// Inicia el spawn periódico de enemigos.
    /// </summary>
    public void StartSpawning()
    {
        if (!spawning)
        {
            spawning = true;
            StartCoroutine(SpawnRoutine());
        }
    }

    /// <summary>
    /// Detiene el spawn de enemigos.
    /// </summary>
    public void StopSpawning()
    {
        spawning = false;
    }

    /// <summary>
    /// Bucle que ejecuta el spawn repetidamente cada cierto intervalo,
    /// siempre y cuando “spawning” siga siendo verdadero.
    /// </summary>
    private IEnumerator SpawnRoutine()
    {
        while (spawning)
        {
            yield return new WaitForSeconds(spawnInterval);
            SpawnEnemy();
        }
    }

    /// <summary>
    /// Instancia un enemigo en una posición aleatoria dentro del área visible del RectTransform.
    /// </summary>
    private void SpawnEnemy()
    {
        if (enemyPrefabs.Length == 0 || spawnArea == null)
            return;

        // Selecciona un prefab aleatorio
        GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];

        // Instancia como hijo de spawnArea (UI)
        GameObject enemyInstance = Instantiate(prefab, spawnArea);

        // Calcula posición aleatoria dentro del área visible (centrado)
        float x = Random.Range(-spawnArea.rect.width / 2f, spawnArea.rect.width / 2f);
        float y = Random.Range(-spawnArea.rect.height / 2f, spawnArea.rect.height / 2f);

        RectTransform rt = enemyInstance.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(x, y);
    }
}