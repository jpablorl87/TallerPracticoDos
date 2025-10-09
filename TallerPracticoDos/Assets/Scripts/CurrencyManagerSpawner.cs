using UnityEngine;

public class CurrencyManagerSpawner : MonoBehaviour
{
    [Tooltip("Prefab que contiene el script CurrencyManager")]
    [SerializeField] private GameObject currencyManagerPrefab;

    private void Awake()
    {
        if (CurrencyManager.Instance == null)
        {
            Instantiate(currencyManagerPrefab);
        }
    }
}
