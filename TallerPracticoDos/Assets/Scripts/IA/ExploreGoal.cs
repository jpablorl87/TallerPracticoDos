using GOAP;
using UnityEngine;

public class ExploreGoal : GOAPGoal
{
    private void Awake()
    {
        GoalName = "Explore";
        Priority = 1f;
        AddDesiredState("isExploring", true);
    }

    public override float GetPriority()
    {
        // Si no está haciendo nada, aumenta prioridad
        return Priority;
    }
}
