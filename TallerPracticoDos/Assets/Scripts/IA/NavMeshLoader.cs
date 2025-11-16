using UnityEngine;
using Unity.AI.Navigation;

[RequireComponent(typeof(NavMeshSurface))]
public class NavMeshLoader : MonoBehaviour
{
    private NavMeshSurface surface;

    private void Awake()
    {
        surface = GetComponent<NavMeshSurface>();
    }

    private void Start()
    {
        if (surface.navMeshData == null)
        {
            Debug.LogWarning("[NavMeshLoader] No hay NavMeshData asignado, construyendo en runtime...");
            surface.BuildNavMesh(); // reconstruye si no encuentra el asset (por ejemplo, en Android)
        }
        else
        {
            surface.AddData(); // fuerza a Unity a cargar la malla ya horneada
            Debug.Log("[NavMeshLoader] NavMeshData encontrado y cargado correctamente.");
        }
    }
}
