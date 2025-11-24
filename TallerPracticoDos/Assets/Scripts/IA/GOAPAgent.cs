using System.Collections.Generic;
using UnityEngine;

namespace GOAP
{
    [RequireComponent(typeof(Transform))]
    public class GOAPAgent : MonoBehaviour
    {
        public List<GOAPAction> availableActions = new List<GOAPAction>();
        private Queue<GOAPAction> currentActions;
        private GOAPAction currentAction;

        private GOAPPlanner planner = new GOAPPlanner();

        private Dictionary<string, bool> worldState = new Dictionary<string, bool>();
        private Dictionary<string, bool> goal = new Dictionary<string, bool>();

        private void Awake()
        {
            availableActions.Clear();
            var acts = GetComponents<GOAPAction>();
            foreach (var a in acts)
            {
                if (a == null) continue;
                availableActions.Add(a);
                //Debug.Log($"[GOAPAgent] {name} registró acción: {a.GetType().Name}");
            }
        }

        private void Update()
        {
            if (goal == null || goal.Count == 0)
                return;

            if (currentActions == null || (currentActions.Count == 0 && currentAction == null))
            {
                Replan();
                return;
            }

            if (currentAction == null && currentActions.Count > 0)
            {
                currentAction = currentActions.Dequeue();
                currentAction.ResetAction();
                //Debug.Log($"[GOAPAgent] {name} ejecutando acción: {currentAction.GetType().Name}");

                if (currentAction.RequiresInRange() && currentAction.Target == null)
                {
                    if (!currentAction.CheckProceduralPrecondition(gameObject))
                    {
                        currentAction = null;
                        Replan();
                        return;
                    }
                }
            }

            if (currentAction == null) return;

            bool result = currentAction.Perform(gameObject);

            if (!result)
            {
                currentAction = null;
                Replan();
                return;
            }

            if (currentAction.IsDone)
            {
                currentAction.ResetAction();
                currentAction = null;

                if (currentActions != null && currentActions.Count > 0)
                    currentAction = currentActions.Dequeue();
                else
                {
                   // Debug.Log($"[GOAPAgent] {name}: plan completado.");
                    SetGoal("DestroyObject", false); // ?? desactiva meta tras completarla
                }
            }
        }

        public void Replan()
        {
            currentActions = planner.Plan(
                new List<GOAPAction>(availableActions),
                new Dictionary<string, bool>(worldState),
                new Dictionary<string, bool>(goal)
            );

            if (currentActions == null || currentActions.Count == 0)
            {
                currentAction = null;
                return;
            }

            currentAction = currentActions.Dequeue();
        }

        public void SetGoal(string goalName, bool value)
        {
            if (goal.ContainsKey(goalName)) goal[goalName] = value;
            else goal.Add(goalName, value);

            //Debug.Log($"[GOAPAgent] {name} SetGoal('{goalName}', {value})");
            Replan();
        }

        public void SetWorldState(string key, bool value)
        {
            if (worldState.ContainsKey(key)) worldState[key] = value;
            else worldState.Add(key, value);
        }
    }
}
