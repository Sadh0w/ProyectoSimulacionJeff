using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Editor References")]
    [SerializeField] Transform camTransform;

    [Header("Movement Parameters")]
    [SerializeField] float speed = 10f;
    [SerializeField] float rotSpeed = 15f;

    [Header("Jump Parameters")]
    [SerializeField] float jumpForce = 8f;
    [SerializeField] Transform groundCheck;
    [SerializeField] float groundCheckRadius = 0.2f;
    [SerializeField] LayerMask groundLayer;

    // Variables internas
    Rigidbody playerRB;
    Vector2 moveInput;
    bool isGrounded;
    bool jumpPressed;

    // Referencia al teclado (Input System)
    private Keyboard keyboard;

    // Referencia al sistema de salud
    private HealthSystem healthSystem;

    private void Awake()
    {
        playerRB = GetComponent<Rigidbody>();
        healthSystem = GetComponent<HealthSystem>();

        if (camTransform == null && Camera.main != null) camTransform = Camera.main.transform;
        if (playerRB != null) playerRB.freezeRotation = true;

        // Inicializar teclado (puede ser null en plataformas sin device)
        keyboard = Keyboard.current;
    }

    void Update()
    {
        CheckIfGrounded();
        ReadKeyboardInput();
    }

    private void FixedUpdate()
    {
        HandleMovement();
        HandleRotation();
        HandleJump();
    }

    void ReadKeyboardInput()
    {
        moveInput = Vector2.zero;
        // no reinicializar aquí jumpPressed para no perder la señal entre Update y FixedUpdate

        // Si no hay dispositivo del Input System, hacemos fallback a Input.GetKey para pruebas
        bool useFallback = keyboard == null;

        // MOVIMIENTO WASD
        if ((!useFallback && (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed))
            || (useFallback && (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))))
            moveInput.y += 1f;

        if ((!useFallback && (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed))
            || (useFallback && (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))))
            moveInput.y -= 1f;

        if ((!useFallback && (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed))
            || (useFallback && (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))))
            moveInput.x -= 1f;

        if ((!useFallback && (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed))
            || (useFallback && (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))))
            moveInput.x += 1f;

        // SALTO - Tecla ESPACIO
        if ((!useFallback && keyboard.spaceKey.wasPressedThisFrame) || (useFallback && Input.GetKeyDown(KeyCode.Space)))
            jumpPressed = true;

        // TEST: Daño y curación con teclas (opcional - para testing)
        if ((!useFallback && keyboard.tKey.wasPressedThisFrame) || (useFallback && Input.GetKeyDown(KeyCode.T)))
        {
            healthSystem?.TakeDamage(10f);
        }
        if ((!useFallback && keyboard.yKey.wasPressedThisFrame) || (useFallback && Input.GetKeyDown(KeyCode.Y)))
        {
            healthSystem?.Heal(10f);
        }

        // Normalizar input diagonal
        if (moveInput.magnitude > 1f) moveInput.Normalize();
    }

    #region Movimiento y Salto

    void HandleMovement()
    {
        if (camTransform == null || playerRB == null) return;

        Vector3 cameraForward = camTransform.forward;
        Vector3 cameraRight = camTransform.right;
        cameraForward.y = 0;
        cameraRight.y = 0;
        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 moveDirection = (cameraForward * moveInput.y + cameraRight * moveInput.x).normalized;

        // Usar linearVelocity (recomendado por la versión de Unity del proyecto)
        Vector3 v = playerRB.linearVelocity;
        v.x = moveDirection.x * speed;
        v.z = moveDirection.z * speed;
        playerRB.linearVelocity = v;
    }

    void HandleRotation()
    {
        if (moveInput == Vector2.zero || playerRB == null) return;

        Vector3 moveDirection = new Vector3(playerRB.linearVelocity.x, 0, playerRB.linearVelocity.z);
        if (moveDirection == Vector3.zero) return;

        Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotSpeed * Time.fixedDeltaTime);
    }

    void HandleJump()
    {
        if (!jumpPressed || !isGrounded || playerRB == null) return;

        // Resetar la componente Y antes de aplicar impulso para saltos consistentes
        Vector3 v = playerRB.linearVelocity;
        v.y = 0f;
        playerRB.linearVelocity = v;

        playerRB.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        jumpPressed = false;
    }

    void CheckIfGrounded()
    {
        if (groundCheck == null)
        {
            isGrounded = false;
            return;
        }

        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);

        // Solo dibujar si groundCheck existe
        Debug.DrawRay(groundCheck.position, Vector3.down * groundCheckRadius, isGrounded ? Color.green : Color.red);
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }

    #endregion
}