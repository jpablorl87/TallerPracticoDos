using UnityEngine;
using UnityEngine.AI;

public static class NavmeshUtility
{
    public static void MarkAsObstacle(GameObject obj)
    {
        if (obj == null) return;

        var obstacle = obj.GetComponent<NavMeshObstacle>();
        if (obstacle == null)
            obstacle = obj.AddComponent<NavMeshObstacle>();

        obstacle.carving = true; // permite actualizar navmesh dinámicamente
        obstacle.shape = NavMeshObstacleShape.Box;
        obstacle.size = obj.GetComponent<Collider>() != null ?
            obj.GetComponent<Collider>().bounds.size : Vector3.one;
    }
}
