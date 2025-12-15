using UnityEngine;
using UnityEngine.InputSystem;

public class JeffWaterCannon : MonoBehaviour
{
    #region Ajustes serializables
    [Header("Water Cannon Settings")]
    [SerializeField] float healAmount = 25f;
    [SerializeField] float damageAmount = 20f;
    [SerializeField] float beamRange = 20f;
    [SerializeField] float castTime = 0.5f;
    [SerializeField] float cooldown = 2f;

    [Header("Beam Visuals")]
    [SerializeField] LineRenderer beamRenderer;
    [SerializeField] ParticleSystem impactEffect;
    [SerializeField] ParticleSystem castEffect;

    [Header("Arc Settings")]
    [SerializeField] float arcHeight = 3f;
    [SerializeField] int arcPoints = 20;
    #endregion

    #region Estado e input
    private Keyboard keyboard;
    private float cooldownTimer;
    private bool isCasting;
    private float castTimer;
    private HealthSystem currentTarget;
    private Camera playerCamera;
    #endregion

    #region Unity Callbacks
    void Start()
    {
        keyboard = Keyboard.current;
        playerCamera = Camera.main;

        if (beamRenderer != null)
            beamRenderer.enabled = false;
    }

    void Update()
    {
        UpdateCooldown();
        HandleInput();
        UpdateCasting();
    }
    #endregion

    #region Input y comienzo de casteo
    void HandleInput()
    {
        if (cooldownTimer > 0 || isCasting) return;

        // Click izquierdo para lanzar rayo o tecla Q
        if (Mouse.current.leftButton.wasPressedThisFrame || keyboard.qKey.wasPressedThisFrame)
        {
            TryStartCasting();
        }
    }

    void TryStartCasting()
    {
        // Usar el centro de la pantalla (crosshair)
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, beamRange))
        {
            // Obtener HealthSystem si existe y está vivo
            HealthSystem target = hit.collider.GetComponent<HealthSystem>();
            currentTarget = (target != null && target.IsAlive) ? target : null;

            // Iniciar casteo siempre (aunque no haya objetivo válido)
            isCasting = true;
            castTimer = 0f;

            if (castEffect != null)
                castEffect.Play();

            Debug.Log("Lanzando chorro de agua...");
        }
    }
    #endregion

    #region Lógica de casteo y completado
    void UpdateCasting()
    {
        if (!isCasting) return;

        castTimer += Time.deltaTime;

        // Mostrar arco del rayo durante el casteo
        if (beamRenderer != null)
        {
            Vector3 targetPosition = currentTarget != null ? currentTarget.transform.position : GetAimPoint();
            DrawBeamArc(transform.position, targetPosition, castTimer / castTime);
        }

        if (castTimer >= castTime)
        {
            CompleteCast();
        }
    }

    void CompleteCast()
    {
        // Aplicar efecto al objetivo si lo hay
        if (currentTarget != null)
        {
            if (currentTarget.CompareTag("Ally"))
            {
                currentTarget.Heal(healAmount);
                Debug.Log($"¡Aliado curado! +{healAmount} de vida");
            }
            else if (currentTarget.CompareTag("Enemy"))
            {
                currentTarget.TakeDamage(damageAmount);
                Debug.Log($"¡Enemigo dañado! -{damageAmount} de vida");
            }
        }
        else
        {
            Debug.Log("Chorro de agua lanzado (sin objetivo)");
        }

        // Efecto de impacto
        Vector3 impactPoint = currentTarget != null ? currentTarget.transform.position : GetAimPoint();
        if (impactEffect != null)
            Instantiate(impactEffect, impactPoint, Quaternion.identity);

        // Reset estado visual y timers
        isCasting = false;
        cooldownTimer = cooldown;
        currentTarget = null;

        if (beamRenderer != null)
            beamRenderer.enabled = false;

        if (castEffect != null)
            castEffect.Stop();
    }
    #endregion

    #region Visual: arco del rayo
    Vector3 GetAimPoint()
    {
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        return ray.origin + ray.direction * beamRange;
    }

    void DrawBeamArc(Vector3 start, Vector3 end, float progress)
    {
        if (beamRenderer == null) return;

        beamRenderer.enabled = true;
        beamRenderer.positionCount = arcPoints;

        Vector3 currentEnd = Vector3.Lerp(start, end, progress);

        for (int i = 0; i < arcPoints; i++)
        {
            float t = i / (float)(arcPoints - 1);
            Vector3 point = CalculateArcPoint(start, currentEnd, t);
            beamRenderer.SetPosition(i, point);
        }
    }

    Vector3 CalculateArcPoint(Vector3 start, Vector3 end, float t)
    {
        Vector3 linear = Vector3.Lerp(start, end, t);
        float arc = arcHeight * Mathf.Sin(t * Mathf.PI);
        return linear + Vector3.up * arc;
    }
    #endregion

    #region Cooldown y utilidades
    void UpdateCooldown()
    {
        if (cooldownTimer > 0)
        {
            cooldownTimer -= Time.deltaTime;
        }
    }
    #endregion
}