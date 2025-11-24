using GOAP;
using UnityEngine;

public class CatIdleAction : GOAPAction
{
    private float idleTime;
    private float timer;

    private void Awake()
    {
        // Idle no debe declarar que produce 'isExploring' — lo dejamos para CatWalkAction
        Cost = 2f; // más costoso para que el planner prefiera caminar
    }

    public override void ResetAction()
    {
        IsDone = false;
        timer = 0f;
        idleTime = Random.Range(2f, 5f);
    }

    public override bool CheckProceduralPrecondition(GameObject agent) => true;

    public override bool Perform(GameObject agent)
    {
        timer += Time.deltaTime;
        if (timer >= idleTime)
        {
            IsDone = true;
            //Debug.Log($"[CatIdleAction] {agent.name} terminó idle ({idleTime:F1}s).");
        }

        return true;
    }

    public override bool RequiresInRange() => false;
}
