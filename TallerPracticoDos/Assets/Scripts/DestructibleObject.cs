using UnityEngine;

public class DestructibleObject : MonoBehaviour
{
    [Header("Health / hits")]
    [SerializeField] private int maxHits = 3;
    private int currentHits = 0;
    public bool IsDestroyed { get; private set; }

    [Header("Effects")]
    [SerializeField] private ParticleSystem hitEffect;      // humo, chispas (instanciado en cada hit)
    [SerializeField] private ParticleSystem destroyEffect;  // efecto final (explosión)

    [Header("Optional")]
    [SerializeField] private bool destroyGameObject = true; // si se destruye el GO

    private void Awake()
    {
        IsDestroyed = false;
        currentHits = 0;
        Debug.Log($"[DestructibleObject] Awake: {name} maxHits={maxHits}");
    }

    // Llamada desde CatHitObjectAction: incremento por golpe.
    public void TakeHit()
    {
        if (IsDestroyed)
        {
            Debug.Log($"[DestructibleObject] TakeHit llamado en {name} pero ya está destruido.");
            return;
        }

        currentHits++;
        Debug.Log($"[DestructibleObject] {name} recibió golpe ({currentHits}/{maxHits})");

        if (hitEffect != null)
        {
            Instantiate(hitEffect, transform.position, Quaternion.identity);
        }

        if (currentHits >= maxHits)
        {
            DestroyObject();
        }
    }

    // Compatibilidad si en algún otro lado se esperaba ApplyDamage(float)
    public void ApplyDamage(float amount)
    {
        // cada unit => un golpe (mantener sencillo)
        if (IsDestroyed)
        {
            Debug.Log($"[DestructibleObject] ApplyDamage llamado en {name} pero ya está destruido.");
            return;
        }

        int hits = Mathf.Max(1, Mathf.RoundToInt(amount));
        currentHits += hits;

        Debug.Log($"[DestructibleObject] {name} ApplyDamage +{hits} => {currentHits}/{maxHits}");

        if (hitEffect != null)
            Instantiate(hitEffect, transform.position, Quaternion.identity);

        if (currentHits >= maxHits)
        {
            DestroyObject();
        }
    }

    public void DestroyObject()
    {
        if (IsDestroyed) return;
        IsDestroyed = true;

        if (destroyEffect != null)
            Instantiate(destroyEffect, transform.position, Quaternion.identity);

        Debug.Log($"[DestructibleObject] {name} destruido. (pos={transform.position})");

        // Delay pequeño opcional para permitir que efecto se vea (si se desea)
        if (destroyGameObject)
            Destroy(gameObject);
    }
}
