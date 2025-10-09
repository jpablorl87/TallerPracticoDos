using UnityEngine;

public class SceneCurrencyChecker : MonoBehaviour
{
    private void Start()
    {
        var managers = FindObjectsByType<CurrencyManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Debug.Log($"[DEBUG] Encontradas {managers.Length} instancias de CurrencyManager en la escena.");
        foreach (var manager in managers)
        {
            Debug.Log($"[DEBUG] CurrencyManager en objeto: {manager.gameObject.name}", manager.gameObject);
        }
    }
}