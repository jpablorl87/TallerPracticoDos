using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GOAP
{
    public class GOAPPlanner
    {
        public Queue<GOAPAction> Plan(List<GOAPAction> availableActions, Dictionary<string, bool> worldState, Dictionary<string, bool> goal)
        {
            var usableActions = availableActions.Where(a => a != null).ToList();
            var leaves = new List<Node>();
            var start = new Node(null, 0f, new Dictionary<string, bool>(worldState), null);

            // --- NUEVO: si la meta ya está satisfecha por el estado inicial,
            // intentar devolver una acción "fallback" que tenga efectos compatibles ---
            bool alreadySatisfied = GoalAchieved(goal, start.State);
            if (alreadySatisfied)
            {
                // buscar acciones cuyos Effects cumplan la meta
                var candidates = usableActions
                    .Where(a => a.Effects != null && a.Effects.Any(e => goal.ContainsKey(e.Key) && goal[e.Key] == e.Value))
                    .OrderBy(a => a.Cost)
                    .ToList();

                if (candidates.Count > 0)
                {
                    // devolver la acción de menor coste como plan simple
                    var single = new List<GOAPAction> { candidates.First() };
                    return new Queue<GOAPAction>(single);
                }

                // si no hay candidatos, no abortamos aún: continuamos con BuildGraph,
                // porque puede que exista una secuencia de acciones útiles (pero raro).
            }

            bool success = BuildGraph(start, leaves, usableActions, goal);
            if (!success)
            {
                // Si no se encontró plan normal, intentar fallback general (acciones sin precondiciones)
                // para no dejar al agente inactivo.
                var fallback = usableActions.Where(a => a.Preconditions == null || a.Preconditions.Count == 0).OrderBy(a => a.Cost).ToList();
                if (fallback.Count > 0)
                {
                    return new Queue<GOAPAction>(new[] { fallback.First() });
                }

                return null;
            }

            var cheapest = leaves.OrderBy(l => l.Cost).FirstOrDefault();
            var result = new List<GOAPAction>();

            Node n = cheapest;
            while (n != null && n.Action != null)
            {
                result.Insert(0, n.Action);
                n = n.Parent;
            }

            return new Queue<GOAPAction>(result);
        }

        private bool BuildGraph(Node parent, List<Node> leaves, List<GOAPAction> usableActions, Dictionary<string, bool> goal)
        {
            bool foundPath = false;
            foreach (var action in usableActions)
            {
                if (InState(action.Preconditions, parent.State))
                {
                    var currentState = new Dictionary<string, bool>(parent.State);
                    foreach (var effect in action.Effects)
                        currentState[effect.Key] = effect.Value;

                    var node = new Node(parent, parent.Cost + action.Cost, currentState, action);

                    if (GoalAchieved(goal, currentState))
                    {
                        leaves.Add(node);
                        foundPath = true;
                    }
                    else
                    {
                        var subset = usableActions.Where(a => a != action).ToList();
                        if (BuildGraph(node, leaves, subset, goal))
                            foundPath = true;
                    }
                }
            }
            return foundPath;
        }

        private bool InState(Dictionary<string, bool> test, Dictionary<string, bool> state)
            => test.All(kv => state.ContainsKey(kv.Key) && state[kv.Key] == kv.Value);

        private bool GoalAchieved(Dictionary<string, bool> goal, Dictionary<string, bool> state)
            => goal.All(kv => state.ContainsKey(kv.Key) && state[kv.Key] == kv.Value);

        private class Node
        {
            public Node Parent;
            public float Cost;
            public Dictionary<string, bool> State;
            public GOAPAction Action;
            public Node(Node parent, float cost, Dictionary<string, bool> state, GOAPAction action)
            {
                Parent = parent;
                Cost = cost;
                State = state;
                Action = action;
            }
        }
    }
}
