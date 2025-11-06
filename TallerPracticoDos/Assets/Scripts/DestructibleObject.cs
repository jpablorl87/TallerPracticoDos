using UnityEngine;

public class DestructibleObject : MonoBehaviour
{
    [Header("Vida del objeto")]
    [Tooltip("Cuánta vida tiene antes de ser destruido.")]
    public float health = 3f;

    [Header("Feedback visual opcional")]
    public ParticleSystem destructionEffect;
    public float destroyDelay = 0f;
    public AudioClip hitSound;
    public AudioClip destroySound;

    private AudioSource audioSource;
    private bool isDestroyed = false;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    /// <summary>
    /// Recibe daño del entorno o IA (por ejemplo, de CatAI).
    /// </summary>
    public void ApplyDamage(float damage)
    {
        if (isDestroyed) return;

        health -= damage;
        Debug.Log($"[DestructibleObject] {name} recibió {damage} de daño. Vida restante: {health}");

        if (hitSound != null && audioSource != null)
            audioSource.PlayOneShot(hitSound);

        if (health <= 0f)
            DestroyObject();
    }

    /// <summary>
    /// Maneja la destrucción del objeto (efecto + destrucción).
    /// </summary>
    public void DestroyObject()
    {
        if (isDestroyed) return;
        isDestroyed = true;

        if (destructionEffect != null)
            Instantiate(destructionEffect, transform.position, Quaternion.identity);

        if (destroySound != null && audioSource != null)
            audioSource.PlayOneShot(destroySound);

        Debug.Log($"[DestructibleObject] {name} destruido.");
        Destroy(gameObject, destroyDelay);
    }
}
