using UnityEngine;
using UnityEngine.AI;
using GOAP;

public class CatHitObjectAction : GOAPAction
{
    private DestructibleObject target;
    private NavMeshAgent agent;
    private bool destinationSet;
    private float attackRange = 1.5f;

    private void Awake()
    {
        AddPrecondition("DestroyObject", true);
        AddEffect("DestroyObject", true);
        Cost = 2f;
    }

    public override void ResetAction()
    {
        target = null;
        destinationSet = false;
        IsDone = false;
    }

    public override bool CheckProceduralPrecondition(GameObject actor)
    {
        // Buscar el objeto destructible más cercano
#if UNITY_2023_2_OR_NEWER
        var all = Object.FindObjectsByType<DestructibleObject>(FindObjectsSortMode.None);
#else
        var all = Object.FindObjectsOfType<DestructibleObject>();
#endif
        if (all.Length == 0) return false;

        float bestDist = Mathf.Infinity;
        DestructibleObject best = null;
        foreach (var d in all)
        {
            if (d == null) continue;
            float dist = Vector3.Distance(actor.transform.position, d.transform.position);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = d;
            }
        }

        if (best != null)
        {
            target = best;
            return true;
        }

        return false;
    }

    public override bool Perform(GameObject actor)
    {
        if (IsDone) return true;

        if (target == null)
        {
            IsDone = true;
            return false;
        }

        if (agent == null)
            agent = actor.GetComponent<NavMeshAgent>();

        if (agent != null && agent.isOnNavMesh)
        {
            if (!destinationSet)
            {
                agent.stoppingDistance = attackRange * 0.9f;
                agent.SetDestination(target.transform.position);
                destinationSet = true;
            }

            if (agent.pathPending) return true;

            if (agent.remainingDistance > attackRange)
                return true; // sigue caminando
        }

        // Si llega aquí, ya está en rango: atacar
        Debug.Log($"[CatHitObjectAction] {actor.name} ataca {target.name}");

        target.DestroyObject(); // tu método original
        IsDone = true;
        return true;
    }

    public override bool RequiresInRange() => false;
}
