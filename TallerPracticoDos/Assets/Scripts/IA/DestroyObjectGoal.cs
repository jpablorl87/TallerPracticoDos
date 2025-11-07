using UnityEngine;
using GOAP;

public class DestroyObjectGoal : GOAPGoal
{
    private CatAI cat;

    private void Awake()
    {
        cat = GetComponent<CatAI>();
        GoalName = "DestroyObject";
        AddDesiredState("DestroyObject", true);
        Priority = 2f;
    }

    public override float GetPriority()
    {
        if (cat == null) return 0f;

        // Detecta objetos cercanos según el mismo LayerMask
        var objs = Physics.OverlapSphere(cat.transform.position, cat.detectionRadius, cat.interactableMask);
        return objs.Length > 0 ? 2f : 0.5f;
    }

    public override bool IsAchievable()
    {
        var objs = Physics.OverlapSphere(transform.position, cat.detectionRadius, cat.interactableMask);
        return objs.Length > 0;
    }
}
