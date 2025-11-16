using System.Collections;
using UnityEngine;
using GOAP;
using UnityEngine.AI;

[RequireComponent(typeof(GOAPAgent), typeof(NavMeshAgent))]
public class CatAI : MonoBehaviour
{
    [Header("Detection (usado por DestroyObjectGoal / acciones)")]
    [SerializeField] private float detectionRadius = 12f;
    [SerializeField] private LayerMask interactableMask = ~0;

    // --- Exposición segura para otros scripts (lectura solamente) ---
    public float DetectionRadius => detectionRadius;
    public LayerMask InteractableMask => interactableMask;

    [Header("Behavior Settings")]
    [SerializeField] private float maxIdleTime = 60f;     // tiempo máximo antes de forzar ataque
    [SerializeField] private float forcedAttackCooldown = 10f;
    [SerializeField] private float attackChance = 0.25f;          // Probabilidad de atacar cuando hay objetivo
    [SerializeField] private Vector2 calmDurationRange = new Vector2(4f, 8f); // Rango aleatorio de calma tras atacar

    // Calm state (métodos públicos Calm() / CalmCat(float) que otras partes pueden llamar)
    public bool IsCalm { get; private set; } = false;
    private Coroutine calmCoroutine;

    private GOAPAgent agent;
    private NavMeshAgent navMeshAgent;

    private float idleTimer;
    private float lastForcedAttackTime;

    private void Awake()
    {
        agent = GetComponent<GOAPAgent>();
        navMeshAgent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        if (agent == null)
        {
            Debug.LogError($"[CatAI] {name} no tiene GOAPAgent.");
            enabled = false;
            return;
        }

        Debug.Log($"[CatAI] {name} iniciado. detectionRadius={detectionRadius}");
        StartCoroutine(BehaviourLoop());
    }

    private IEnumerator BehaviourLoop()
    {
        while (true)
        {
            Collider[] found = Physics.OverlapSphere(transform.position, detectionRadius, interactableMask);
            bool hasTarget = found.Length > 0;

            // Log detallado de detección
            if (hasTarget)
            {
                string names = "";
                foreach (var c in found) names += c.gameObject.name + ", ";
                Debug.Log($"[CatAI] Detectados {found.Length} interactables cerca de {name}: {names}");
            }
            else
            {
                Debug.Log($"[CatAI] No se detectaron interactables cerca de {name} (radius={detectionRadius}).");
            }

            // Intento de actualizar world state en el GOAPAgent
            if (agent != null)
            {
                agent.SetWorldState("HasTarget", hasTarget);
                Debug.Log($"[CatAI] Llamó agent.SetWorldState('HasTarget', {hasTarget})");
            }
            else
            {
                Debug.LogWarning($"[CatAI] agent es NULL en {name} cuando intenta SetWorldState.");
            }

            // Si hay objetivo cercano y no estamos calmados, solicitar ataque
            if (hasTarget && !IsCalm)
            {
                Debug.Log($"[CatAI] hasTarget && !IsCalm -> llamando TryImmediateAttack en {name}.");
                TryImmediateAttack();
            }

            // Idle aleatorio
            float idleDuration = Random.Range(2f, 5f);
            Debug.Log($"[CatAI] {name} idle {idleDuration:F1}s.");
            float timer = 0f;
            while (timer < idleDuration)
            {
                // si estamos calmados no hacemos nada especial, solo esperamos
                timer += Time.deltaTime;
                idleTimer += Time.deltaTime;

                // Forzar ataque si lleva mucho tiempo sin atacar y no está calm
                if (!IsCalm && idleTimer > maxIdleTime && Time.time - lastForcedAttackTime > forcedAttackCooldown)
                {
                    Debug.Log($"[CatAI] {name} lleva >{maxIdleTime}s sin atacar, forzando intento.");
                    lastForcedAttackTime = Time.time;
                    TryImmediateAttack(force: true);
                    idleTimer = 0f;
                    // tras forzar un ataque continuamos el loop (el GOAPAgent buscará plan)
                    break;
                }

                yield return null;
            }

            // Pequeña probabilidad de caminar en vez de volver a idle
            if (!IsCalm && Random.value > 0.5f)
            {
                yield return StartCoroutine(WalkRandomRoutine());
            }
        }
    }

    private IEnumerator WalkRandomRoutine()
    {
        Vector3 randomDestination = transform.position + Random.insideUnitSphere * 4f;
        randomDestination.y = transform.position.y;

        if (NavMesh.SamplePosition(randomDestination, out var hit, 4f, NavMesh.AllAreas))
        {
            if (navMeshAgent != null && navMeshAgent.isOnNavMesh)
            {
                navMeshAgent.SetDestination(hit.position);
                Debug.Log($"[CatAI] {name} caminando a {hit.position}.");

                while (navMeshAgent.pathPending || navMeshAgent.remainingDistance > navMeshAgent.stoppingDistance)
                    yield return null;
            }
            else
            {
                Debug.LogWarning($"[CatAI] navMeshAgent no disponible o no onNavMesh en {name}.");
            }
        }

        yield return null;
    }

    /// <summary>
    /// Forzar objetivo/ataque inmediato: pide al GOAPAgent que replantee con la meta DestroyObject.
    /// No realizará la solicitud si el gato está calmado (a menos que 'force' sea true).
    /// </summary>
    public void TryImmediateAttack(bool force = false)
    {
        if (IsCalm && !force)
        {
            Debug.Log($"[CatAI] {name} está calmado; no forzar ataque.");
            return;
        }

        if (agent == null)
        {
            Debug.LogError($"[CatAI] agent es NULL en TryImmediateAttack() de {name}.");
            return;
        }

        // logging adicional: listar acciones conocidas por el GOAPAgent (si está expuesto)
        try
        {
            var actions = agent.availableActions;
            Debug.Log($"[CatAI] GOAPAgent tiene {actions?.Count ?? 0} acciones registradas.");
            if (actions != null)
            {
                foreach (var a in actions)
                {
                    Debug.Log($"[CatAI] - acción registrada: {(a != null ? a.GetType().Name : "NULL")}");
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[CatAI] No se pudo leer availableActions del GOAPAgent: {ex.Message}");
        }

        // Probabilidad de atacar (comportamiento impredecible)
        if (!force && Random.value > attackChance)
        {
            Debug.Log($"[CatAI] {name} decidió no atacar esta vez (probabilidad).");
            return;
        }

        // actualizar el estado de mundo para indicar que hay objetivo (si es cierto se detectará en CheckProceduralPrecondition)
        agent.SetWorldState("HasTarget", true);
        agent.SetGoal("DestroyObject", true);

        Debug.Log($"[CatAI] {name} solicitó meta 'DestroyObject' (force={force}).");

        // aplicamos una calma corta después de solicitar la meta para evitar replanificaciones spam
        CalmCatForSeconds(Random.Range(calmDurationRange.x, calmDurationRange.y));
    }

    // --- Métodos de "calm" solicitados por tu código previo o herramientas de debugging --- 
    public void Calm() => CalmCat(3f);

    public void CalmCat(float seconds)
    {
        if (calmCoroutine != null) StopCoroutine(calmCoroutine);
        calmCoroutine = StartCoroutine(CalmRoutine(seconds));
    }

    private void CalmCatForSeconds(float seconds)
    {
        if (calmCoroutine != null) StopCoroutine(calmCoroutine);
        calmCoroutine = StartCoroutine(CalmRoutine(seconds));
    }

    private IEnumerator CalmRoutine(float seconds)
    {
        IsCalm = true;
        Debug.Log($"[CatAI] {name} calmado por {seconds:F1}s.");
        yield return new WaitForSeconds(seconds);
        IsCalm = false;
        Debug.Log($"[CatAI] {name} ya no está calmado.");
    }
}
