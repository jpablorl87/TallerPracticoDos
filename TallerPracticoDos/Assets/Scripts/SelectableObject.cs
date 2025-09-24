using UnityEngine;

/// <summary>
/// Este script se encarga de definir las propiedades de un objeto que puede ser
/// seleccionado en la escena. Principalmente, gestiona si un objeto puede ser
/// decorado y con qué tipo de decoraciones.
/// </summary>
public class SelectableObject : MonoBehaviour
{
    // --- PROPIEDADES PÚBLICAS EN EL INSPECTOR ---

    [Tooltip("Indica si este objeto puede ser decorado. Si es falso, la lógica de decoración no se aplicará.")]
    public bool canBeDecorated = false;

    [Tooltip("Un array de tags que definen qué tipo de decoraciones están permitidas en este objeto.")]
    // Los tags permitidos, como "BallDecoration" o "LightDecoration", se configuran
    // directamente en el Inspector de Unity.
    public string[] allowedDecorationTags;

    // --- MÉTODOS PÚBLICOS ---

    /// <summary>
    /// Verifica si una decoración específica, identificada por su tag, está permitida en este objeto.
    /// </summary>
    /// <param name="decorationTag">El tag de la decoración que se intenta colocar.</param>
    /// <returns>True si la decoración está permitida, de lo contrario, False.</returns>
    public bool CanDecorate(string decorationTag)
    {
        // Primer paso: Si la bandera 'canBeDecorated' es falsa, no se permite ninguna decoración.
        // Esto es una optimización para evitar el bucle si no es necesario.
        if (!canBeDecorated) return false;

        // Segundo paso: Recorre todos los tags permitidos para verificar si el tag de la
        // decoración que se intenta colocar coincide con alguno de ellos.
        foreach (string tag in allowedDecorationTags)
        {
            // Compara el tag de la decoración con el tag permitido.
            if (tag == decorationTag)
            {
                // Si hay una coincidencia, retorna true inmediatamente.
                return true;
            }
        }

        // Tercer paso: Si el bucle termina sin encontrar una coincidencia,
        // significa que la decoración no está permitida en este objeto.
        return false;
    }
}