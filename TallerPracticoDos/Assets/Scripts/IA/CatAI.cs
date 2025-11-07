using System.Collections;
using UnityEngine;
using GOAP;

[RequireComponent(typeof(GOAPAgent))]
[RequireComponent(typeof(UnityEngine.AI.NavMeshAgent))]
public class CatAI : MonoBehaviour
{
    private GOAPAgent agent;
    private UnityEngine.AI.NavMeshAgent nav;
    private bool isIdle;
    private bool isCalm;

    [Header("Comportamiento general")]
    public float idleMin = 2f;
    public float idleMax = 5f;
    public float forcedActionInterval = 10f;
    public float exploreRadius = 6f;
    [Header("Detección y combate")]
    public LayerMask interactableMask;
    public float detectionRadius = 6f;

    private float lastAttackTime;
    private float timeSinceLastAction;
    private Coroutine idleRoutine;

    public bool IsCalm => isCalm;

    private void Awake()
    {
        agent = GetComponent<GOAPAgent>();
        nav = GetComponent<UnityEngine.AI.NavMeshAgent>();
    }

    private void Start()
    {
        Debug.Log($"[CatAI] {name} iniciado con {agent.AvailableActions.Count} acciones GOAP.");
        ElegirNuevaAccion();
    }

    private void Update()
    {
        if (isCalm) return;

        timeSinceLastAction += Time.deltaTime;

        if (!isIdle && timeSinceLastAction > forcedActionInterval)
        {
            Debug.Log($"[CatAI] {name} lleva mucho tiempo quieto, forzando acción...");
            ElegirNuevaAccion();
            timeSinceLastAction = 0;
        }

        if (!agent.HasPlan && !isIdle)
            ElegirNuevaAccion();
    }

    private void ElegirNuevaAccion()
    {
        if (idleRoutine != null)
        {
            StopCoroutine(idleRoutine);
            idleRoutine = null;
        }

        // Random entre 0 y 1
        float decision = Random.value;

        // --- Probabilidades ---
        // 0.0 - 0.3 >> Idle
        // 0.3 - 0.7 >> Walk
        // 0.7 - 1.0 >> Intentar ataque
        if (decision < 0.3f)
        {
            idleRoutine = StartCoroutine(IdleBehavior());
        }
        else if (decision < 0.7f)
        {
            StartCoroutine(WalkRandom());
        }
        else
        {
            TryImmediateAttack();
        }

        // --- Ataque forzado cada ~60s ---
        if (Time.time % 60f < 1f && Random.value < 0.6f)
        {
            Debug.Log($"[CatAI] {name} siente impulso agresivo espontáneo.");
            TryImmediateAttack();
        }

        // --- Ataque garantizado cada minuto ---
        // Si ha pasado más de 60 segundos desde el último ataque, forzar uno.
        if (Time.time - lastAttackTime > 60f)
        {
            Debug.Log($"[CatAI] {name} lleva más de un minuto sin atacar, forzando ataque.");
            TryImmediateAttack(forceAttack: true);
        }
    }



    private IEnumerator IdleBehavior()
    {
        isIdle = true;
        float wait = Random.Range(idleMin, idleMax);
        Debug.Log($"[CatAI] {name} está idle por {wait:F1}s.");
        yield return new WaitForSeconds(wait);
        isIdle = false;
        timeSinceLastAction = 0;
        ElegirNuevaAccion();
    }

    private IEnumerator WalkRandom()
    {
        if (nav == null || !nav.isOnNavMesh)
        {
            Debug.LogWarning($"[CatAI] {name} no puede moverse (sin NavMesh).");
            yield break;
        }

        Vector3 randomDirection = Random.insideUnitSphere * exploreRadius;
        randomDirection += transform.position;

        if (UnityEngine.AI.NavMesh.SamplePosition(randomDirection, out var hit, exploreRadius, UnityEngine.AI.NavMesh.AllAreas))
        {
            nav.SetDestination(hit.position);
            Debug.Log($"[CatAI] {name} camina hacia {hit.position}.");
        }

        yield return new WaitUntil(() => !nav.pathPending && nav.remainingDistance <= nav.stoppingDistance);
        Debug.Log($"[CatAI] {name} llegó a destino.");
        timeSinceLastAction = 0;
        ElegirNuevaAccion();
    }

    public void TryImmediateAttack(bool forceAttack = false)
    {
        if (isCalm) return;

        Collider[] nearbyObjects = Physics.OverlapSphere(transform.position, detectionRadius, interactableMask);
        if (nearbyObjects.Length == 0 && !forceAttack)
        {
            Debug.Log($"[CatAI] {name} no encontró objetivos en rango ({detectionRadius}m).");
            return;
        }

        GameObject target = nearbyObjects.Length > 0
            ? nearbyObjects[Random.Range(0, nearbyObjects.Length)].gameObject
            : null;

        if (target != null)
            Debug.Log($"[CatAI] {name} detecta {nearbyObjects.Length} objetos y quiere atacar {target.name}.");
        else
            Debug.Log($"[CatAI] {name} no tiene objetivo, pero atacará por impulso.");

        // Forzar meta de destrucción
        agent.SetGoal("DestroyObject", true);
        lastAttackTime = Time.time;

        // Ataque inmediato si se forza
        if (forceAttack && target != null)
        {
            var destructible = target.GetComponent<DestructibleObject>();
            if (destructible != null)
            {
                Debug.Log($"[CatAI] {name} ejecuta ataque directo a {target.name}.");
                destructible.DestroyObject();
            }
        }
    }

    public void CalmCat(float duration)
    {
        if (isCalm) return;
        StartCoroutine(CalmRoutine(duration));
    }

    private IEnumerator CalmRoutine(float duration)
    {
        isCalm = true;
        nav.isStopped = true;
        Debug.Log($"[CatAI] {name} calmado por {duration:F1}s.");
        yield return new WaitForSeconds(duration);
        nav.isStopped = false;
        isCalm = false;
        Debug.Log($"[CatAI] {name} vuelve a la acción.");
        ElegirNuevaAccion();
    }
}
