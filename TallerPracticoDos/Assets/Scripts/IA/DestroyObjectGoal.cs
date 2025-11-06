using UnityEngine;
using GOAP;

public class DestroyObjectGoal : GOAPGoal
{
    private CatAI cat;

    private void Awake()
    {
        cat = GetComponent<CatAI>();
        GoalName = "DestroyObject";

        // Se agrega la condición deseada
        AddDesiredState("DestroyObject", true);

        Priority = 2f;
    }

    public override float GetPriority()
    {
        if (cat == null) return 0f;

        // Si el gato detecta objetos cerca, sube prioridad
        var objs = Physics.OverlapSphere(cat.transform.position, 6f, LayerMask.GetMask("Interactable"));
        return objs.Length > 0 ? 3f : 0.5f;
    }

    public override bool IsAchievable()
    {
        // Usa el método moderno, sin obsoletos
        var destructibles = Object.FindObjectsByType<DestructibleObject>(FindObjectsSortMode.None);
        return destructibles.Length > 0;
    }
}
