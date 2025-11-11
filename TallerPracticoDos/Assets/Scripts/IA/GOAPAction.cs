using System.Collections.Generic;
using UnityEngine;

namespace GOAP
{
    public abstract class GOAPAction : MonoBehaviour
    {
        // Estado requerido antes de ejecutar esta acción
        public Dictionary<string, bool> Preconditions { get; private set; } = new();
        // Estado resultante tras ejecutar la acción
        public Dictionary<string, bool> Effects { get; private set; } = new();

        // Coste relativo para el planificador
        public float Cost = 1f;

        // Indica si ya se cumplió la acción
        public bool IsDone { get; protected set; }

        // Objetivo o destino (opcional)
        public GameObject Target { get; protected set; }

        // Asignar precondiciones o efectos
        protected void AddPrecondition(string key, bool value) => Preconditions[key] = value;
        protected void AddEffect(string key, bool value) => Effects[key] = value;

        // Sobreescribir en cada acción concreta
        public abstract bool CheckProceduralPrecondition(GameObject agent);
        public abstract bool Perform(GameObject agent);
        public abstract bool RequiresInRange();
        public abstract void ResetAction();

        // Helper: resetea Target y estado básico
        protected void ClearTarget()
        {
            Target = null;
            IsDone = false;
        }

        // Helper seguro para comprobar "Unity null" (también cubre objetos ya destruidos)
        protected static bool IsNullOrDestroyed(Object obj) => obj == null;
    }
}
