using GOAP;
using UnityEngine;
using UnityEngine.AI;

public class CatWalkAction : GOAPAction
{
    private NavMeshAgent navAgent;
    private Vector3 targetPos;
    private bool destinationSet;
    private float wanderRadius = 8f;
    private float stuckTimer;

    private void Awake()
    {
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
        targetPos = RandomNavSphere(agent.transform.position, wanderRadius);
        return true;
    }

    public override bool Perform(GameObject agent)
    {
        if (navAgent == null) navAgent = agent.GetComponent<NavMeshAgent>();

        if (!destinationSet)
        {
            if (NavMesh.SamplePosition(RandomNavSphere(agent.transform.position, wanderRadius),
                out var hit, wanderRadius, NavMesh.AllAreas))
            {
                targetPos = hit.position;
                navAgent.SetDestination(targetPos);
                destinationSet = true;
                Debug.Log($"[CatWalkAction] {agent.name} camina hacia {targetPos}");
            }
            else
            {
                IsDone = true;
                return false;
            }
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
