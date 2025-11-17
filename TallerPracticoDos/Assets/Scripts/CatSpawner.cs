using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

public class CatSpawner : MonoBehaviour
{
    [Header("Cat Prefabs")]
    [SerializeField] private List<GameObject> catPrefabs = new List<GameObject>();

    [Header("Dependencies")]
    public PlayerMetrics playerMetrics;

    [Header("Spawn Settings")]
    [SerializeField] private int maxCats = 5;
    [SerializeField] private float checkInterval = 5f;
    [SerializeField] private float spawnRadius = 15f;
    public Transform spawnCenter;

    private readonly List<GameObject> activeCats = new();
    private float timer;
    private bool isActive;

    private void Start()
    {
        playerMetrics ??= FindFirstObjectByType<PlayerMetrics>();
        spawnCenter ??= transform;
        timer = checkInterval;

        if (!gameObject.activeInHierarchy)
        {
           // Debug.LogWarning("[CatSpawner] Estaba desactivado, activando...");
            gameObject.SetActive(true);
        }

        var surface = FindFirstObjectByType<Unity.AI.Navigation.NavMeshSurface>();
        if (surface != null)
        {
            bool hasNavMesh = UnityEngine.AI.NavMesh.SamplePosition(spawnCenter.position, out var _, 2f, UnityEngine.AI.NavMesh.AllAreas);
            if (!hasNavMesh)
            {
                //Debug.LogWarning("[CatSpawner] No hay NavMesh detectado, reconstruyendo...");
                surface.BuildNavMesh();
            }
        }
        else
        {
            //Debug.LogError("[CatSpawner] No se encontró NavMeshSurface en la escena. Los gatos no podrán moverse.");
        }

        if (catPrefabs == null || catPrefabs.Count == 0)
        {
            //Debug.LogError("[CatSpawner] catPrefabs vacío o nulo. Verifica en el Inspector que el prefab del gato esté asignado.");
        }
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
        //Debug.Log("[CatSpawner] Intentando spawnear gato...");

        if (activeCats.Count >= maxCats)
        {
          //  Debug.Log("[CatSpawner] Límite máximo de gatos alcanzado.");
            return;
        }

        if (catPrefabs == null || catPrefabs.Count == 0)
        {
            //Debug.LogError("[CatSpawner] catPrefabs vacío o nulo. Verifica referencias en el Inspector.");
            return;
        }

        Vector3 randomPos = spawnCenter.position + Random.insideUnitSphere * spawnRadius;
        randomPos.y = spawnCenter.position.y + 0.2f; // altura base

        if (NavMesh.SamplePosition(randomPos, out var hit, 4f, NavMesh.AllAreas))
        {
            GameObject prefab = catPrefabs[Random.Range(0, catPrefabs.Count)];
            //Debug.Log($"[CatSpawner] Prefab seleccionado: {prefab?.name ?? "NULL"}");

            GameObject cat = Instantiate(prefab, hit.position, Quaternion.identity);
            cat.transform.position += Vector3.up * 0.1f; // evita quedar bajo el plano
            activeCats.Add(cat);

            //Debug.Log($"[CatSpawner] Gato creado en {hit.position}");
        }
        else
        {
            //Debug.LogWarning($"[CatSpawner] No se encontró posición válida cerca de {randomPos}");
        }
    }


    public void ActivateSpawner()
    {
        //Debug.Log("[CatSpawner] Activado por primer objeto colocado.");

        // Verificar si existe algún NavMeshSurface activo
        var surface = FindFirstObjectByType<Unity.AI.Navigation.NavMeshSurface>();
        if (surface == null)
        {
            //Debug.LogError("[CatSpawner] No se encontró NavMeshSurface en la escena. Los gatos no podrán moverse.");
        }
        else
        {
            // Intentar samplear posición para verificar si hay NavMesh
            if (!NavMesh.SamplePosition(spawnCenter.position, out var _, 2f, NavMesh.AllAreas))
            {
                //Debug.LogWarning("[CatSpawner] No se detecta NavMesh válido, reconstruyendo...");
                surface.BuildNavMesh();
            }
            else
            {
                //Debug.Log("[CatSpawner] NavMesh válido detectado.");
            }
        }

        // Asegurar que el spawner esté activo
        isActive = true;

        // Cancelar invocaciones previas para evitar duplicados
        CancelInvoke(nameof(TrySpawnCat));

        // Spawnear un gato inmediatamente
        TrySpawnCat();

        // Programar spawns periódicos usando checkInterval existente
        InvokeRepeating(nameof(TrySpawnCat), checkInterval, checkInterval);

        // Mostrar overlay de depuración
        //DebugOverlay.Log("Spawner activo", 3f);
    }

}
