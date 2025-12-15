using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCamera : MonoBehaviour
{
    #region Configuración pública
    [Header("Camera Settings")]
    [SerializeField] Transform player;
    [SerializeField] float mouseSensitivity = 2f;
    [SerializeField] float distanceFromPlayer = 3f;
    [SerializeField] float height = 1.5f;
    [SerializeField] float verticalAngleLimit = 80f;

    [Header("Shoulder Offset")]
    [SerializeField] float shoulderOffset = 0.5f;
    [SerializeField] KeyCode shoulderSwitchKey = KeyCode.V;

    [Header("Crosshair")]
    [SerializeField] Texture2D crosshairTexture;
    [SerializeField] Vector2 crosshairSize = new Vector2(32, 32);
    [SerializeField] Color crosshairColor = Color.white;

    [Header("Smoothing")]
    [SerializeField] float positionSmoothTime = 0.1f;
    [SerializeField] float rotationSmoothTime = 0.05f;

    [Header("Options")]
    [SerializeField] bool lockCursor = true;
    [SerializeField] bool rotatePlayerWithCamera = true;
    #endregion

    #region Estado privado
    float rotationX = 0f; // pitch
    float rotationY = 0f; // yaw
    Vector3 cameraVelocity;
    float currentShoulderOffset;
    Mouse mouse;
    Camera mainCamera;

    // para suavizado de ángulos
    float currentYawVelocity;
    float currentPitchVelocity;
    #endregion

    #region Unity callbacks
    void Start()
    {
        mouse = Mouse.current;
        mainCamera = Camera.main;

        if (player != null)
        {
            rotationY = player.eulerAngles.y;
        }

        currentShoulderOffset = shoulderOffset;

        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void Update()
    {
        if (player == null)
        {
            Debug.LogWarning($"ThirdPersonCamera: 'player' no asignado en {name}");
            return;
        }

        ReadLookInput();
        HandleShoulderSwitch();
    }

    void LateUpdate()
    {
        if (player == null) return;
        UpdateCameraPosition();
    }
    #endregion

    #region Input y rotación
    void ReadLookInput()
    {
        Vector2 delta;

        // Preferir Input System si está disponible, fallback a Input.GetAxis
        if (mouse != null)
        {
            // IMPORTANT: mouse.delta ya es movimiento por frame -> NO multiplicar por Time.deltaTime
            delta = mouse.delta.ReadValue();
        }
        else
        {
            // Input.GetAxis devuelve un valor por frame también; no multipliques por Time.deltaTime aquí
            delta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        }

        // Aplicar sensibilidad (sin Time.deltaTime)
        float targetYaw = rotationY + delta.x * mouseSensitivity;
        float targetPitch = rotationX - delta.y * mouseSensitivity;
        targetPitch = Mathf.Clamp(targetPitch, -verticalAngleLimit, verticalAngleLimit);

        // Suavizado de ángulos (opcional). Si quieres respuesta más inmediata, reduce rotationSmoothTime a ~0.01 o 0f.
        rotationY = Mathf.SmoothDampAngle(rotationY, targetYaw, ref currentYawVelocity, rotationSmoothTime);
        rotationX = Mathf.SmoothDampAngle(rotationX, targetPitch, ref currentPitchVelocity, rotationSmoothTime);

        // Aplicar rotación de cámara
        transform.rotation = Quaternion.Euler(rotationX, rotationY, 0f);
    }

    void HandleShoulderSwitch()
    {
        // Soporta tanto Input System como Input clásico (KeyCode configurable)
        if ((Keyboard.current != null && Keyboard.current.vKey.wasPressedThisFrame) || Input.GetKeyDown(shoulderSwitchKey))
        {
            currentShoulderOffset = -currentShoulderOffset;
        }
    }
    #endregion

    #region Movimiento de cámara y apuntado
    void UpdateCameraPosition()
    {
        // posición objetivo detrás del jugador según la rotación actual de la cámara
        Vector3 cameraDirection = -transform.forward;
        Vector3 targetPosition = player.position + cameraDirection * distanceFromPlayer;
        targetPosition.y = player.position.y + height;
        targetPosition += transform.right * currentShoulderOffset;

        // suavizar posición
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref cameraVelocity, positionSmoothTime);

        // Opcional: rotar jugador para mirar en la misma dirección horizontal de la cámara
        if (rotatePlayerWithCamera)
        {
            RotatePlayerToCamera();
        }
    }

    void RotatePlayerToCamera()
    {
        Vector3 cameraDirection = transform.forward;
        cameraDirection.y = 0f;

        if (cameraDirection.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(cameraDirection.normalized);
            // rotación inmediata: se podría suavizar si se desea
            player.rotation = Quaternion.Slerp(player.rotation, targetRot, 1f);
        }
    }

    // Devuelve dirección donde la cámara apunta
    public Vector3 GetAimDirection() => transform.forward;

    // Devuelve punto de impacto o punto lejano si no choca
    public Vector3 GetAimPoint(float maxDistance = 100f)
    {
        Camera rayCam = mainCamera != null ? mainCamera : Camera.main;
        if (rayCam == null) return transform.position + transform.forward * maxDistance;

        Ray ray = new Ray(rayCam.transform.position, rayCam.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance))
            return hit.point;

        return ray.origin + ray.direction * maxDistance;
    }
    #endregion

    #region UI - Crosshair
    void OnGUI()
    {
        if (!lockCursor) return; // opcional: no dibujar si cursor desbloqueado

        if (crosshairTexture != null)
        {
            float x = (Screen.width - crosshairSize.x) / 2f;
            float y = (Screen.height - crosshairSize.y) / 2f;
            var prev = GUI.color;
            GUI.color = crosshairColor;
            GUI.DrawTexture(new Rect(x, y, crosshairSize.x, crosshairSize.y), crosshairTexture);
            GUI.color = prev;
        }
        else
        {
            DrawSimpleCrosshair();
        }
    }

    void DrawSimpleCrosshair()
    {
        float cx = Screen.width / 2f;
        float cy = Screen.height / 2f;
        float size = 10f;
        float thickness = 2f;

        var prev = GUI.color;
        GUI.color = crosshairColor;
        GUI.DrawTexture(new Rect(cx - size, cy - thickness / 2f, size * 2f, thickness), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(cx - thickness / 2f, cy - size, thickness, size * 2f), Texture2D.whiteTexture);
        GUI.color = prev;
    }
    #endregion
}