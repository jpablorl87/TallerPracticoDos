using UnityEngine;
using System.Collections.Generic;
using System;


/// <summary>
/// Administra los items que el jugador ha comprado / desbloqueado.
/// Singleton para acceso global.
/// </summary>
public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    // Evento público que se lanza cuando se agrega un nuevo ítem al inventario
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
    /// Añade un objeto al inventario en memoria y lanza el evento.
    /// </summary>
    public void AddItem(GameObject prefab)
    {
        items.Add(prefab);

        // Lanza el evento para notificar que el inventario cambió
        OnInventoryUpdated?.Invoke();
    }

    /// <summary>
    /// Retorna todos los objetos comprados hasta ahora.
    /// </summary>
    public IReadOnlyList<GameObject> GetItems()
    {
        return items.AsReadOnly();
    }
}