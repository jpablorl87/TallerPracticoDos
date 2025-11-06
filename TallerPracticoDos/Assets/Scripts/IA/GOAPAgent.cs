using System.Collections.Generic;
using UnityEngine;

namespace GOAP
{
    [RequireComponent(typeof(Animator))]
    public class GOAPAgent : MonoBehaviour
    {
        protected List<GOAPAction> availableActions = new();
        protected Queue<GOAPAction> currentActions = new();
        protected GOAPPlanner planner = new();
        protected GOAPGoal currentGoal;
        protected Animator animator;
        public bool HasPlan => currentActions.Count > 0;
        public IReadOnlyList<GOAPAction> AvailableActions => availableActions.AsReadOnly();

        private float replanTimer;
        private float replanInterval = 5f;
        private float replanRandomness = 0.3f;

        protected virtual void Start()
        {
            animator = GetComponent<Animator>();
            availableActions.AddRange(GetComponents<GOAPAction>());
            Debug.Log($"[GOAPAgent] {name} availableActions: {availableActions.Count}");
            replanTimer = Random.Range(2f, 4f);
        }

        protected virtual void Update()
        {
            replanTimer -= Time.deltaTime;

            if (currentActions.Count == 0 || replanTimer <= 0f)
                Replan();

            if (currentActions.Count > 0)
            {
                var action = currentActions.Peek();

                if (action.IsDone)
                {
                    currentActions.Dequeue();
                    Debug.Log($"[GOAPAgent] {name} acción completada: {action.GetType().Name}");
                    Replan();
                    return;
                }

                bool success = action.Perform(gameObject);
                if (!success)
                {
                    Debug.LogWarning($"[GOAPAgent] {name} acción falló ({action.GetType().Name}), replanificando...");
                    currentActions.Clear();
                    Replan();
                }
            }
        }

        /// <summary>
        /// Establece una meta dinámica (sin usar new GOAPGoal, que no es válido en MonoBehaviours)
        /// </summary>
        public void SetGoal(string goalName, bool active)
        {
            // Busca en este GameObject una GOAPGoal con ese nombre y úsala
            var goals = GetComponents<GOAPGoal>();
            var found = System.Array.Find(goals, g => g != null && g.GoalName == goalName);
            if (found != null)
            {
                currentGoal = found;
                Debug.Log($"[GOAPAgent] {name} estableció meta dinámica '{goalName}'.");
                Replan();
                return;
            }

            // Si no existe una GOAPGoal con ese nombre, no intentes crear un MonoBehaviour con new.
            Debug.LogWarning($"[GOAPAgent] {name} no encontró una GOAPGoal llamada '{goalName}'. Asegúrate de que exista como componente.");
        }

        private void Replan()
        {
            replanTimer = replanInterval + Random.Range(-replanInterval * replanRandomness, replanInterval * replanRandomness);

            if (currentGoal == null)
            {
                currentGoal = ChooseGoal();
                if (currentGoal == null)
                {
                    Debug.Log($"[GOAPAgent] {name} no tiene metas disponibles.");
                    return;
                }
            }

            var worldState = GetWorldState();
            Debug.Log($"[GOAPAgent] {name} (re)planificando meta: {currentGoal.GoalName}");

            var plan = planner.Plan(availableActions, worldState, currentGoal.DesiredState);
            if (plan != null && plan.Count > 0)
            {
                currentActions = plan;
                Debug.Log($"[GOAPAgent] {name} nuevo plan con {plan.Count} acciones ({currentGoal.GoalName}).");
            }
            else
            {
                Debug.Log($"[GOAPAgent] {name} no pudo crear plan, intentará nuevamente pronto.");
                currentActions.Clear();
            }
        }

        protected virtual GOAPGoal ChooseGoal() => null;
        protected virtual Dictionary<string, bool> GetWorldState() => new();
    }
}
