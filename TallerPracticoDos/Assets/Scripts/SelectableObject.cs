using UnityEngine;
// Define el tipo de superficie donde se puede colocar el objeto.
public enum PlacementSurface
{
    FloorOnly,
    WallOnly,
    AnySurface
}

// Define qué eje del objeto (X, Y o Z) es relevante para el offset.
// Vertical (Piso): El offset es la altura (eje Y).
// Horizontal (Pared): El offset es la profundidad (eje Z).
public enum PlacementOrientation
{
    Vertical,
    Horizontal
}

/// <summary>
/// Marca que este objeto puede ser seleccionado...
/// </summary>
public class SelectableObject : MonoBehaviour
{
    [Header("Placement Rules")]
    [Tooltip("Define en qué tipo de superficie puede colocarse este objeto.")]
    public PlacementSurface allowedSurface = PlacementSurface.FloorOnly;

    [Tooltip("Define si el objeto se eleva por la altura (Vertical=Y) o se separa por la profundidad (Horizontal=Z).")]
    public PlacementOrientation placementOrientation = PlacementOrientation.Vertical;

    [Tooltip("Si es false, no se permitirá decoración en este objeto.")]
    public bool canBeDecorated = false;

    [Tooltip("Tags permitidos de decoraciones para este objeto.")]
    public string[] allowedDecorationTags;

    /// <summary>
    /// Retorna true si esta decoración con dicho tag está permitida aquí.
    /// </summary>
    public bool CanDecorate(string decorationTag)
    {
        if (!canBeDecorated) return false;

        foreach (string tag in allowedDecorationTags)
        {
            if (tag == decorationTag) return true;
        }
        return false;
    }
}