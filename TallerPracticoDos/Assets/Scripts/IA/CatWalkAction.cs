using GOAP;
using UnityEngine;
using UnityEngine.AI;

public class CatWalkAction : GOAPAction
{
    private NavMeshAgent navAgent;
    private Vector3 targetPos;
    private bool destinationSet;
    private float stuckTimer;

    [SerializeField] private float wanderRadius = 8f;
    [SerializeField] private LayerMask destructibleMask;

    private void Awake()
    {
        // Esta acción prepara el terreno para atacar
        AddEffect("HasTarget", true);
        AddEffect("isExploring", true);
        Cost = 0.5f;
    }

    public override void ResetAction()
    {
        IsDone = false;
        destinationSet = false;
        stuckTimer = 0f;
    }

    public override bool CheckProceduralPrecondition(GameObject agent)
    {
        navAgent ??= agent.GetComponent<NavMeshAgent>();

        // Intentar moverse hacia un objeto destructible visible
        var destructibles = GameObject.FindObjectsByType<DestructibleObject>(FindObjectsSortMode.None);
        if (destructibles != null && destructibles.Length > 0)
        {
            var target = destructibles[Random.Range(0, destructibles.Length)];
            if (target != null)
            {
                targetPos = target.transform.position;
                return true;
            }
        }

        // Si no hay objetos, deambula aleatoriamente
        targetPos = RandomNavSphere(agent.transform.position, wanderRadius);
        return true;
    }

    public override bool Perform(GameObject agent)
    {
        if (navAgent == null) navAgent = agent.GetComponent<NavMeshAgent>();

        if (!destinationSet)
        {
            navAgent.SetDestination(targetPos);
            destinationSet = true;
            Debug.Log($"[CatWalkAction] {agent.name} camina hacia {targetPos}");
        }

        // Verificar si se atascó
        if (navAgent.velocity.sqrMagnitude < 0.01f)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer > 3f)
            {
                Debug.LogWarning($"[CatWalkAction] {agent.name} atascado, abortando camino.");
                IsDone = true;
                return false;
            }
        }
        else stuckTimer = 0f;

        if (!navAgent.pathPending && navAgent.remainingDistance <= navAgent.stoppingDistance)
        {
            IsDone = true;
            Debug.Log($"[CatWalkAction] {agent.name} llegó a destino.");
        }

        return true;
    }

    public override bool RequiresInRange() => false;

    private Vector3 RandomNavSphere(Vector3 origin, float dist)
    {
        Vector3 rand = Random.insideUnitSphere * dist + origin;
        return NavMesh.SamplePosition(rand, out var hit, dist, NavMesh.AllAreas)
            ? hit.position
            : origin;
    }
}
