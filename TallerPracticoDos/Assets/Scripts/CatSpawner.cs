using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class CatSpawner : MonoBehaviour
{
    [Header("Cat Prefabs")]
    [SerializeField] private List<GameObject> catPrefabs = new List<GameObject>();

    [Header("Dependencies")]
    [SerializeField] private PlayerMetrics playerMetrics;

    [Header("Spawn Settings")]
    [SerializeField] private int maxCats = 5;
    [SerializeField] private float checkInterval = 5f;
    [SerializeField] private float spawnRadius = 15f;
    [SerializeField] private Transform spawnCenter;

    private readonly List<GameObject> activeCats = new();
    private float timer;
    private bool isActive;

    private void Start()
    {
        playerMetrics ??= FindFirstObjectByType<PlayerMetrics>();
        spawnCenter ??= transform;
        timer = checkInterval;
    }

    private void Update()
    {
        if (!isActive) return;
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            ManageCatPopulation();
            timer = checkInterval;
        }

        activeCats.RemoveAll(c => c == null);
    }

    private void ManageCatPopulation()
    {
        if (catPrefabs.Count == 0) return;

        float skill = playerMetrics != null ? playerMetrics.GetPlayerSkillLevel() : 1f;
        int desiredCats = Mathf.Clamp(Mathf.RoundToInt(skill * 2f), 1, maxCats);
        int missing = desiredCats - activeCats.Count;

        for (int i = 0; i < missing; i++) TrySpawnCat();
    }

    private void TrySpawnCat()
    {
        if (activeCats.Count >= maxCats) return;

        Vector3 randomPos = spawnCenter.position + Random.insideUnitSphere * spawnRadius;
        randomPos.y = spawnCenter.position.y + 0.2f; // altura más baja

        if (NavMesh.SamplePosition(randomPos, out var hit, 4f, NavMesh.AllAreas))
        {
            GameObject prefab = catPrefabs[Random.Range(0, catPrefabs.Count)];
            GameObject cat = Instantiate(prefab, hit.position, Quaternion.identity);
            activeCats.Add(cat);
            Debug.Log($"[CatSpawner] Gato creado en {hit.position}");
        }
        else
        {
            Debug.LogWarning("[CatSpawner] No se encontró posición válida.");
        }
    }

    public void ActivateSpawner()
    {
        if (isActive) return;
        isActive = true;
        Debug.Log("[CatSpawner] Activado por primer objeto colocado.");
    }
}
