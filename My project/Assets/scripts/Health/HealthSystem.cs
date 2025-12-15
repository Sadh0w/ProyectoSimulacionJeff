using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

public class HealthSystem : MonoBehaviour
{
    #region Campos serializados

    [Header("Health Settings")]
    #region Health Settings
    [SerializeField] float maxHealth = 100f;
    [SerializeField] float currentHealth;
    [SerializeField] bool isInvulnerable = false;
    [SerializeField] float invulnerabilityTime = 1f;
    #endregion

    [Header("Health Bar UI")]
    #region Health Bar UI
    [SerializeField] GameObject healthBarCanvas;
    [SerializeField] Slider healthSlider;
    [SerializeField] Image healthFillImage;
    [SerializeField] TextMeshProUGUI healthText;
    [SerializeField] bool showHealthBar = true;
    [SerializeField] bool showHealthText = true;
    #endregion

    [Header("Health Bar Colors")]
    #region Health Bar Colors
    [SerializeField] Color healthyColor = Color.green;
    [SerializeField] Color damagedColor = Color.yellow;
    [SerializeField] Color criticalColor = Color.red;
    #endregion

    [Header("Visual Effects")]
    #region Visual Effects
    [SerializeField] ParticleSystem healEffect;
    [SerializeField] ParticleSystem damageEffect;
    [SerializeField] ParticleSystem deathEffect;
    #endregion

    [Header("Health Events")]
    #region Health Events
    public UnityEvent<float> OnHealthChanged;
    public UnityEvent OnDeath;
    public UnityEvent OnHit;
    public UnityEvent OnHeal;
    #endregion

    #endregion

    #region Estado privado y propiedades
    float invulnerabilityTimer;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsAlive => currentHealth > 0;
    public bool IsInvulnerable => isInvulnerable;
    public float HealthPercentage => maxHealth > 0f ? currentHealth / maxHealth : 0f;
    #endregion

    #region Unity Callbacks
    void Start()
    {
        currentHealth = maxHealth;
        SetupHealthBar();
        NotifyHealthChanged();
    }

    void Update()
    {
        UpdateInvulnerability();
    }
    #endregion

    #region Control de salud (Daño / Curar / Muerte)
    void SetCurrentHealth(float newHealth)
    {
        newHealth = Mathf.Clamp(newHealth, 0f, maxHealth);
        if (Mathf.Approximately(newHealth, currentHealth)) return;

        float previous = currentHealth;
        currentHealth = newHealth;

        NotifyHealthChanged();
        RefreshHealthBar();

        if (currentHealth <= 0f && previous > 0f) Die();
    }

    public void TakeDamage(float damage)
    {
        if (isInvulnerable || !IsAlive || damage <= 0f) return;

        OnHit?.Invoke();
        SpawnEffect(damageEffect, 2f);
        Debug.Log($"{gameObject.name} recibió {damage} de daño. Vida: {Mathf.Max(currentHealth - damage, 0f)}/{maxHealth}");

        SetCurrentHealth(currentHealth - damage);

        if (damage > 0f) StartInvulnerability();
    }

    public void Heal(float healAmount)
    {
        if (!IsAlive || healAmount <= 0f) return;

        float before = currentHealth;
        SetCurrentHealth(currentHealth + healAmount);

        if (currentHealth > before)
        {
            OnHeal?.Invoke();
            SpawnEffect(healEffect, 2f);
            Debug.Log($"{gameObject.name} curado por {healAmount}. Vida: {currentHealth}/{maxHealth}");
        }
    }

    public void HealToPercentage(float targetPercentage)
    {
        if (!IsAlive) return;
        float t = Mathf.Clamp01(targetPercentage);
        SetCurrentHealth(maxHealth * t);
    }

    public void RestoreFullHealth()
    {
        if (!IsAlive) return;
        SetCurrentHealth(maxHealth);
        OnHeal?.Invoke();
    }

    public void Kill()
    {
        if (!IsAlive) return;
        SetCurrentHealth(0f);
    }
    #endregion

    #region UI (Inicialización / Refresco / Visibilidad)
    void SetupHealthBar()
    {
        if (healthBarCanvas != null) healthBarCanvas.SetActive(showHealthBar);

        if (healthSlider != null)
        {
            healthSlider.minValue = 0f;
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }

        RefreshHealthBar();
    }

    void NotifyHealthChanged()
    {
        OnHealthChanged?.Invoke(HealthPercentage);
    }

    void RefreshHealthBar()
    {
        if (!showHealthBar) return;

        if (healthSlider != null) healthSlider.value = currentHealth;

        if (healthFillImage != null)
        {
            float p = HealthPercentage;
            healthFillImage.color = p > 0.6f ? healthyColor : (p > 0.3f ? damagedColor : criticalColor);
        }

        if (healthText != null && showHealthText) healthText.text = $"{currentHealth:F0}/{maxHealth:F0}";
    }

    public void SetHealthBarVisible(bool visible)
    {
        showHealthBar = visible;
        if (healthBarCanvas != null) healthBarCanvas.SetActive(visible);
        RefreshHealthBar();
    }

    public void SetHealthTextVisible(bool visible)
    {
        showHealthText = visible;
        RefreshHealthBar();
    }
    #endregion

    #region Efectos visuales
    void SpawnEffect(ParticleSystem prefab, float destroyAfter)
    {
        if (prefab == null) return;
        var effect = Instantiate(prefab, transform.position, Quaternion.identity);
        effect.Play();
        Destroy(effect.gameObject, destroyAfter);
    }

    void Die()
    {
        Debug.Log($"{gameObject.name} ha muerto!");
        if (healthBarCanvas != null) healthBarCanvas.SetActive(false);
        SpawnEffect(deathEffect, 3f);
        OnDeath?.Invoke();
    }
    #endregion

    #region Invulnerabilidad
    public void StartInvulnerability()
    {
        isInvulnerable = true;
        invulnerabilityTimer = invulnerabilityTime;
    }

    void UpdateInvulnerability()
    {
        if (!isInvulnerable) return;
        invulnerabilityTimer -= Time.deltaTime;
        if (invulnerabilityTimer <= 0f) isInvulnerable = false;
    }
    #endregion

    #region Utilidades
    public void SetMaxHealth(float newMaxHealth, bool fillHealth = false)
    {
        if (newMaxHealth <= 0f) return;
        float pct = HealthPercentage;
        maxHealth = newMaxHealth;
        currentHealth = fillHealth ? maxHealth : maxHealth * pct;

        if (healthSlider != null) healthSlider.maxValue = maxHealth;
        NotifyHealthChanged();
        RefreshHealthBar();
    }

    public bool IsHealthBelowPercentage(float percentage) => HealthPercentage < percentage;
    public float GetMissingHealth() => maxHealth - currentHealth;
    #endregion
}