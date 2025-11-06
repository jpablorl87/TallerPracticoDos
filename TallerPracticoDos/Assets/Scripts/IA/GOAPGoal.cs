using System.Collections.Generic;
using UnityEngine;

namespace GOAP
{
    public abstract class GOAPGoal : MonoBehaviour
    {
        public string GoalName;
        public float Priority = 1f;
        public Dictionary<string, bool> DesiredState { get; private set; } = new();

        protected void AddDesiredState(string key, bool value) => DesiredState[key] = value;

        // Se puede actualizar dinámicamente (ej: si el jugador es “bueno”, cambiar prioridades)
        public virtual float GetPriority() => Priority;

        // Permitimos que derivadas sobreescriban la comprobación de alcanzabilidad
        public virtual bool IsAchievable()
        {
#if UNITY_2023_2_OR_NEWER
            var found = UnityEngine.Object.FindObjectsByType<DestructibleObject>(FindObjectsSortMode.None);
            return found != null && found.Length > 0;
#else
            // Fallback si tu Unity es anterior (evita error de compilación si no existe FindObjectsByType)
            var foundLegacy = Object.FindObjectsOfType<DestructibleObject>();
            return foundLegacy != null && foundLegacy.Length > 0;
#endif
        }
    }
}
