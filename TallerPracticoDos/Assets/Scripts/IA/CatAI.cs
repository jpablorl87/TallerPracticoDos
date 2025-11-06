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

        float decision = Random.value;

        if (decision < 0.4f)
            idleRoutine = StartCoroutine(IdleBehavior());
        else if (decision < 0.8f)
            StartCoroutine(WalkRandom());
        else
            TryImmediateAttack();
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

    public void TryImmediateAttack()
    {
        var destructibles = FindObjectsOfType<DestructibleObject>();
        if (destructibles.Length == 0)
        {
            StartCoroutine(WalkRandom());
            return;
        }

        GameObject closest = null;
        float bestDist = Mathf.Infinity;

        foreach (var d in destructibles)
        {
            float dist = Vector3.Distance(transform.position, d.transform.position);
            if (dist < bestDist)
            {
                bestDist = dist;
                closest = d.gameObject;
            }
        }

        if (closest != null)
        {
            Debug.Log($"[CatAI] {name} detecta {destructibles.Length} objetos y quiere atacar {closest.name}.");
            agent.SetGoal("DestroyObject", true);
        }
        else
        {
            StartCoroutine(IdleBehavior());
        }

        timeSinceLastAction = 0;
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
