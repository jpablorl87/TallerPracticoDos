using UnityEngine;

/// <summary>
/// Este script se encarga de definir las propiedades de un objeto que puede ser
/// seleccionado en la escena. Principalmente, gestiona si un objeto puede ser
/// decorado y con qu� tipo de decoraciones.
/// </summary>
public class SelectableObject : MonoBehaviour
{
    // --- PROPIEDADES P�BLICAS EN EL INSPECTOR ---

    [Tooltip("Indica si este objeto puede ser decorado. Si es falso, la l�gica de decoraci�n no se aplicar�.")]
    public bool canBeDecorated = false;

    [Tooltip("Un array de tags que definen qu� tipo de decoraciones est�n permitidas en este objeto.")]
    // Los tags permitidos, como "BallDecoration" o "LightDecoration", se configuran
    // directamente en el Inspector de Unity.
    public string[] allowedDecorationTags;

    // --- M�TODOS P�BLICOS ---

    /// <summary>
    /// Verifica si una decoraci�n espec�fica, identificada por su tag, est� permitida en este objeto.
    /// </summary>
    /// <param name="decorationTag">El tag de la decoraci�n que se intenta colocar.</param>
    /// <returns>True si la decoraci�n est� permitida, de lo contrario, False.</returns>
    public bool CanDecorate(string decorationTag)
    {
        // Primer paso: Si la bandera 'canBeDecorated' es falsa, no se permite ninguna decoraci�n.
        // Esto es una optimizaci�n para evitar el bucle si no es necesario.
        if (!canBeDecorated) return false;

        // Segundo paso: Recorre todos los tags permitidos para verificar si el tag de la
        // decoraci�n que se intenta colocar coincide con alguno de ellos.
        foreach (string tag in allowedDecorationTags)
        {
            // Compara el tag de la decoraci�n con el tag permitido.
            if (tag == decorationTag)
            {
                // Si hay una coincidencia, retorna true inmediatamente.
                return true;
            }
        }

        // Tercer paso: Si el bucle termina sin encontrar una coincidencia,
        // significa que la decoraci�n no est� permitida en este objeto.
        return false;
    }
}