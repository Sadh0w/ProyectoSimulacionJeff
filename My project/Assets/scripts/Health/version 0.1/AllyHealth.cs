using UnityEngine;
using UnityEngine.Events;

public class AllyHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] float maxHealth = 100f;
    [SerializeField] float currentHealth;

    [Header("Visual Feedback")]
    [SerializeField] Renderer allyRenderer;
    [SerializeField] Material healthyMaterial;
    [SerializeField] Material damagedMaterial;
    [SerializeField] ParticleSystem healEffect;
    [SerializeField] ParticleSystem damageEffect;

    [Header("Events")]
    public UnityEvent<float> OnHealthChanged; // Health percentage
    public UnityEvent OnAllyHealed;
    public UnityEvent OnAllyDamaged;
    public UnityEvent OnAllyDied;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsAlive => currentHealth > 0;
    public float HealthPercentage => currentHealth / maxHealth;

    void Start()
    {
        currentHealth = maxHealth;
        UpdateVisuals();
    }

    public void TakeDamage(float damage)
    {
        if (!IsAlive) return;

        currentHealth = Mathf.Max(currentHealth - damage, 0);

        OnHealthChanged?.Invoke(HealthPercentage);
        OnAllyDamaged?.Invoke();

        // Efecto visual de daño
        if (damageEffect != null)
            Instantiate(damageEffect, transform.position, Quaternion.identity);

        UpdateVisuals();
        Debug.Log($"Aliado recibió {damage} de daño. Vida: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(float healAmount)
    {
        if (!IsAlive) return;

        float previousHealth = currentHealth;
        currentHealth = Mathf.Min(currentHealth + healAmount, maxHealth);

        OnHealthChanged?.Invoke(HealthPercentage);

        if (currentHealth > previousHealth)
        {
            OnAllyHealed?.Invoke();

            // Efecto visual de curación
            if (healEffect != null)
                Instantiate(healEffect, transform.position, Quaternion.identity);

            Debug.Log($"Aliado curado por {healAmount}. Vida: {currentHealth}");
        }

        UpdateVisuals();
    }

    void UpdateVisuals()
    {
        if (allyRenderer != null && healthyMaterial != null && damagedMaterial != null)
        {
            // Cambiar material según la salud
            if (HealthPercentage > 0.5f)
            {
                allyRenderer.material = healthyMaterial;
            }
            else if (HealthPercentage > 0.2f)
            {
                // Interpolar entre healthy y damaged
                allyRenderer.material.Lerp(healthyMaterial, damagedMaterial, 1 - (HealthPercentage - 0.2f) / 0.3f);
            }
            else
            {
                allyRenderer.material = damagedMaterial;
            }
        }
    }

    void Die()
    {
        OnAllyDied?.Invoke();
        Debug.Log("Aliado ha muerto!");

        // Aquí puedes agregar animación de muerte, desactivar colisiones, etc.
    }

    public void RestoreHealth()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(1f);
        UpdateVisuals();
    }
}