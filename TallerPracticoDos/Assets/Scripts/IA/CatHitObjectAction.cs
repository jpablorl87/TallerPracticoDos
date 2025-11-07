using UnityEngine;
using UnityEngine.AI;
using GOAP;

public class CatHitObjectAction : GOAPAction
{
    private DestructibleObject target;
    private NavMeshAgent agent;
    private float attackRange = 1.6f;
    private float attackCooldown = 1.5f;
    private float lastAttackTime;

    private void Awake()
    {
        AddPrecondition("DestroyObject", true);
        AddEffect("DestroyObject", true);
        Cost = 2f;
    }

    public override void ResetAction()
    {
        target = null;
        IsDone = false;
        lastAttackTime = 0f;
    }

    public override bool CheckProceduralPrecondition(GameObject actor)
    {
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
            Target = target.gameObject;
            return true;
        }

        return false;
    }

    public override bool Perform(GameObject actor)
    {
        if (IsDone) return true;
        if (target == null)
        {
            Debug.LogWarning($"[CatHitObjectAction] {actor.name} no tiene objetivo válido.");
            IsDone = true;
            return false;
        }

        agent ??= actor.GetComponent<NavMeshAgent>();

        // Moverse hacia el objetivo
        if (agent != null && agent.isOnNavMesh)
        {
            agent.stoppingDistance = attackRange;
            agent.SetDestination(target.transform.position);

            if (agent.pathPending) return true;

            float dist = Vector3.Distance(actor.transform.position, target.transform.position);

            // --- Atacar si está en rango ---
            if (dist <= attackRange)
            {
                if (Time.time - lastAttackTime >= attackCooldown)
                {
                    lastAttackTime = Time.time;

                    Debug.Log($"[CatHitObjectAction] {actor.name} ataca {target.name} (distancia {dist:F1})");

                    // --- Aquí está el ataque real ---
                    target.ApplyDamage(1f);

                    // Si el objeto fue destruido, marcamos la acción como completada
                    if (target == null || target.Equals(null))
                    {
                        Debug.Log($"[CatHitObjectAction] {actor.name} destruyó {target?.name ?? "objeto"}.");
                        IsDone = true;
                        return true;
                    }
                }
            }
        }

        // Si el objetivo fue destruido por otro gato
        if (target == null || target.Equals(null))
        {
            IsDone = true;
        }

        return true;
    }

    public override bool RequiresInRange() => true;
}
