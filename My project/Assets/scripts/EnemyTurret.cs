using UnityEngine;

public class EnemyTurret : MonoBehaviour
{
    [Header("Shooting Settings")]
    [SerializeField] float fireRate = 1f;
    [SerializeField] float damage = 10f;
    [SerializeField] float detectionRange = 15f;
    [SerializeField] LayerMask allyLayer;

    [Header("Visual Effects")]
    [SerializeField] ParticleSystem muzzleFlash;
    [SerializeField] LineRenderer laserRenderer;
    [SerializeField] Transform firePoint;

    private float fireTimer;
    private HealthSystem currentTarget;

    void Update()
    {
        FindTarget();

        if (currentTarget != null && currentTarget.IsAlive)
        {
            // Apuntar al objetivo
            Vector3 direction = (currentTarget.transform.position - transform.position).normalized;
            direction.y = 0;
            transform.rotation = Quaternion.LookRotation(direction);

            // Disparar
            fireTimer += Time.deltaTime;
            if (fireTimer >= fireRate)
            {
                Shoot();
                fireTimer = 0f;
            }
        }
    }

    void FindTarget()
    {
        // Buscar aliados en rango
        Collider[] alliesInRange = Physics.OverlapSphere(transform.position, detectionRange, allyLayer);

        HealthSystem closestAlly = null; // CAMBIADO: HealthSystem
        float closestDistance = Mathf.Infinity;

        foreach (Collider collider in alliesInRange)
        {
            // CAMBIADO: Buscar HealthSystem en lugar de AllyHealth
            HealthSystem ally = collider.GetComponent<HealthSystem>();
            if (ally != null && ally.IsAlive)
            {
                float distance = Vector3.Distance(transform.position, ally.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestAlly = ally;
                }
            }
        }

        currentTarget = closestAlly;

        // DEBUG: Mostrar información del target
        if (currentTarget != null)
        {
            Debug.Log($"EnemyTurret: Target encontrado - {currentTarget.gameObject.name} (Vida: {currentTarget.CurrentHealth})");
        }
    }

    #region Disparos

    void Shoot()
    {
        if (currentTarget == null) return;

        // Aplicar daño al objetivo
        currentTarget.TakeDamage(damage);

        // Efectos visuales
        if (muzzleFlash != null)
            muzzleFlash.Play();

        if (laserRenderer != null)
            StartCoroutine(ShowLaserBeam());

        Debug.Log($"Torreta disparó a {currentTarget.gameObject.name}. Daño: {damage}");
    }

    System.Collections.IEnumerator ShowLaserBeam()
    {
        if (firePoint != null && currentTarget != null)
        {
            laserRenderer.SetPosition(0, firePoint.position);
            laserRenderer.SetPosition(1, currentTarget.transform.position);
            laserRenderer.enabled = true;

            yield return new WaitForSeconds(0.1f);

            laserRenderer.enabled = false;
        }
    }

    void OnDrawGizmosSelected()
    {
        // Mostrar rango de detección
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Mostrar línea al objetivo actual
        if (currentTarget != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, currentTarget.transform.position);
        }
    }

    #endregion
}