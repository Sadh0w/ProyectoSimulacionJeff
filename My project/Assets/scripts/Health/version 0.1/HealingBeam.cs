using UnityEngine;
using UnityEngine.InputSystem;

public class HealingBeam : MonoBehaviour
{
    [Header("Healing Settings")]
    [SerializeField] float healPerSecond = 10f;
    [SerializeField] float beamRange = 20f;

    [Header("Beam Visuals")]
    [SerializeField] LineRenderer beamRenderer;
    [SerializeField] ParticleSystem impactEffect; // puede ser un sistema ya instanciado en la escena
    [SerializeField] ParticleSystem castEffect;
    [SerializeField] AudioClip healSound;
    [SerializeField] float healSoundInterval = 0.3f;

    // estado interno
    Keyboard keyboard;
    Camera playerCamera;
    bool isFiring;
    float healSoundTimer;
    HealthSystem currentTarget;

    void Start()
    {
        keyboard = Keyboard.current;
        playerCamera = Camera.main;

        if (beamRenderer != null) beamRenderer.enabled = false;
        if (castEffect != null) castEffect.Stop();
        healSoundTimer = 0f;
    }

    void Update()
    {
        HandleInput();
        if (isFiring) DoFiring();
        if (healSoundTimer > 0f) healSoundTimer -= Time.deltaTime;
    }

    void HandleInput()
    {
        // Disparo mantenido con click izquierdo (Input System). Si no existe Mouse.current no hará nada.
        bool wantFire = Mouse.current != null && Mouse.current.leftButton.isPressed;

        if (wantFire && !isFiring) StartFiring();
        else if (!wantFire && isFiring) StopFiring();
    }

    void StartFiring()
    {
        isFiring = true;
        currentTarget = null;
        if (castEffect != null && !castEffect.isPlaying) castEffect.Play();
        Debug.Log("HealingBeam: start firing");
    }

    void StopFiring()
    {
        isFiring = false;
        currentTarget = null;
        if (beamRenderer != null) beamRenderer.enabled = false;
        if (impactEffect != null && impactEffect.isPlaying) impactEffect.Stop();
        if (castEffect != null && castEffect.isPlaying) castEffect.Stop();
        Debug.Log("HealingBeam: stop firing");
    }

    void DoFiring()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null) return;
        }

        // Buscar objetivo y calcular punto final del rayo
        Vector3 aimOrigin = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f)).origin;
        Vector3 aimDir = playerCamera.transform.forward;
        Ray ray = new Ray(aimOrigin, aimDir);
        RaycastHit hit;
        Vector3 endPoint = aimOrigin + aimDir * beamRange;

        // Raycast (detecta cualquier collider dentro del rango)
        if (Physics.Raycast(ray, out hit, beamRange))
        {
            var health = hit.collider.GetComponent<HealthSystem>();
            if (health != null && health.IsAlive)
            {
                currentTarget = health;
                endPoint = currentTarget.transform.position;
            }
            else
            {
                currentTarget = null;
                endPoint = hit.point;
            }
        }
        else
        {
            currentTarget = null;
        }

        // Visual
        UpdateBeamVisual(transform.position, endPoint);

        // Curación
        if (currentTarget != null && currentTarget.IsAlive)
        {
            float healThisFrame = healPerSecond * Time.deltaTime;
            currentTarget.Heal(healThisFrame);

            // Impact effect (si está referenciado como sistema en escena)
            if (impactEffect != null)
            {
                impactEffect.transform.position = currentTarget.transform.position;
                if (!impactEffect.isPlaying) impactEffect.Play();
            }

            // Sonido periódico
            if (healSound != null && healSoundTimer <= 0f)
            {
                AudioSource.PlayClipAtPoint(healSound, transform.position, 0.1f);
                healSoundTimer = healSoundInterval;
            }
        }
        else
        {
            if (impactEffect != null && impactEffect.isPlaying) impactEffect.Stop();
        }

#if UNITY_EDITOR
        Debug.DrawRay(aimOrigin, aimDir * beamRange, Color.green, 0.05f);
#endif
    }

    void UpdateBeamVisual(Vector3 start, Vector3 end)
    {
        if (beamRenderer == null) return;

        beamRenderer.enabled = true;
        beamRenderer.positionCount = 2;
        beamRenderer.SetPosition(0, start);
        beamRenderer.SetPosition(1, end);

        Color activeColor = (currentTarget != null) ? Color.green : new Color(1f, 1f, 1f, 0.3f);
        beamRenderer.startColor = activeColor;
        beamRenderer.endColor = activeColor;
        if (beamRenderer.material != null) beamRenderer.material.color = activeColor;
    }
}