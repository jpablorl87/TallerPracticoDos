using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Administra los ítems que el jugador ha comprado o desbloqueado.
/// Singleton para acceso global y persistencia de la instancia entre escenas.
/// </summary>
public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    public event Action OnInventoryUpdated;

    private List<GameObject> items = new List<GameObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Añade un prefab al inventario y dispara evento para refrescar UI.
    /// </summary>
    public void AddItem(GameObject prefab)
    {
        items.Add(prefab);
        OnInventoryUpdated?.Invoke();
    }

    /// <summary>
    /// Retorna lista inmutable de ítems comprados.
    /// </summary>
    public IReadOnlyList<GameObject> GetItems()
    {
        return items.AsReadOnly();
    }
}
