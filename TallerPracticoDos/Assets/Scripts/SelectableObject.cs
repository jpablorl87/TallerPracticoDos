using UnityEngine;

/// <summary>
/// Marca que este objeto puede ser seleccionado, decorado o usado
/// como base para decoraciones.
/// </summary>
public class SelectableObject : MonoBehaviour
{
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