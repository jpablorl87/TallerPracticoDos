using UnityEngine;
using UnityEngine.AI;
using GOAP;

[RequireComponent(typeof(Animator), typeof(NavMeshAgent))]
public class CatHitObjectAction : GOAPAction
{
    [Header("Attack Settings")]
    [SerializeField] private float attackRange = 1.8f;
    [SerializeField] private float attackCooldown = 1.2f;
    [SerializeField] private float lookBeforeAttack = 1.8f;
    [SerializeField] private float attackAngleTolerance = 60f;
    [SerializeField] private int hitsToDestroy = 2;
    [SerializeField] private float detectionRadius = 12f;

    private DestructibleObject target;
    private NavMeshAgent agent;
    private Animator animator;

    private float lookTimer;
    private float lastAttackTime;
    private int localHits;

    private void Awake()
    {
        AddEffect("DestroyObject", true);
        Cost = 2f;
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
    }

    public override void ResetAction()
    {
        target = null;
        Target = null;
        IsDone = false;
        lookTimer = 0f;
        lastAttackTime = 0f;
        localHits = 0;
    }

    public override bool CheckProceduralPrecondition(GameObject actor)
    {
        var all = FindObjectsByType<DestructibleObject>(FindObjectsSortMode.None);
        if (all == null || all.Length == 0) return false;

        float bestDist = Mathf.Infinity;
        DestructibleObject best = null;

        foreach (var d in all)
        {
            if (d == null || d.IsDestroyed) continue;
            float dist = Vector3.Distance(actor.transform.position, d.transform.position);
            if (dist < bestDist && dist <= detectionRadius)
            {
                bestDist = dist;
                best = d;
            }
        }

        if (best == null) return false;
        target = best;
        Target = target.gameObject;
        //Debug.Log($"[CatHitObjectAction] {actor.name} asignó objetivo: {target.name} (dist={bestDist:F2})");
        return true;
    }

    public override bool Perform(GameObject actor)
    {
        if (target == null || target.IsDestroyed)
        {
            if (!CheckProceduralPrecondition(actor))
            {
                IsDone = true;
                return false;
            }
        }

        if (!agent.isOnNavMesh) return false;

        float distance = Vector3.Distance(actor.transform.position, target.transform.position);
        agent.stoppingDistance = attackRange;
        agent.SetDestination(target.transform.position);

        if (distance > attackRange)
        {
            animator?.SetTrigger("Walk");
            return true;
        }

        Vector3 dir = (target.transform.position - actor.transform.position);
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.01f)
        {
            Quaternion desired = Quaternion.LookRotation(dir);
            actor.transform.rotation = Quaternion.Slerp(actor.transform.rotation, desired, Time.deltaTime * 6f);
        }

        float angle = Vector3.Angle(actor.transform.forward, dir);
        if (angle > attackAngleTolerance)
        {
            animator?.SetTrigger("Idle");
            return true;
        }

        lookTimer += Time.deltaTime;
        if (lookTimer >= lookBeforeAttack && Time.time - lastAttackTime >= attackCooldown)
        {
            lastAttackTime = Time.time;
            lookTimer = 0f;
            localHits++;

            animator?.SetTrigger("Attack");
            target.TakeHit();

            //Debug.Log($"[CatHitObjectAction] {actor.name} golpeó {target.name} (hit {localHits})");

            if (target.IsDestroyed)
            {
                //Debug.Log($"[CatHitObjectAction] {actor.name} destruyó {target.name}");
                IsDone = true;

                var catAI = actor.GetComponent<CatAI>();
                if (catAI != null)
                    catAI.CalmCat(Random.Range(5f, 10f)); // pausa tras destrucción
            }

            if (localHits >= hitsToDestroy)
                IsDone = true;
        }

        return true;
    }

    public override bool RequiresInRange() => true;
}
