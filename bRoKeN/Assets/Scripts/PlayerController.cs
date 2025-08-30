using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    [Header("Player Health")]
    public float currentHealth;
    float maxHealth = 100f;
    private bool isDead = false;
    public Transform startPosition;

    [Header("UI")]
    public GameObject respawnScreen; // Reference to the respawn screen GameObject

    [Header("Movement and Gravity")]
    public float speed = 5f;
    public float sprintSpeed = 8f;
    public float gravity = -9.81f;
    private CharacterController characterController;
    private Vector3 velocity;
    private bool isGrounded;
    private CharacterPusher pusher;

    [Header("Sprint Settings")]
    public float sprintFOVIncrease = 5f; // How much to increase FOV when sprinting
    public float fovTransitionSpeed = 8f; // How fast FOV changes

    [Header("Camera")]
    public float mouseSensitivity = 2;
    private Camera playerCamera;
    private float originalFOV;
    private bool isSprinting = false;

    [Header("Jump & Crouch")]
    public float crouchSpeed = 2.5f;
    public float jumpHeight = 1f;
    public float crouchHeight = 0f;
    public float standHeight = 2f;
    public bool isCrouching = false;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        pusher = GetComponent<CharacterPusher>();

        // Get camera reference and store original FOV
        playerCamera = Camera.main;
        if (playerCamera == null)
            playerCamera = FindObjectOfType<Camera>();

        if (playerCamera != null)
            originalFOV = playerCamera.fieldOfView;

        if (characterController == null)
        {
            Debug.LogError("CharacterController component is missing on this GameObject.");
        }

        Cursor.lockState = CursorLockMode.Locked;
        currentHealth = maxHealth;

        // Ensure respawn screen is hidden at the start
        if (respawnScreen != null)
        {
            respawnScreen.SetActive(false);
        }
    }

    void Update()
    {
        if (characterController == null || isDead) return;

        isGrounded = characterController.isGrounded;

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        HandleCameraMovement();
        HandlePlayerMovement();
        HandleSprint();

        velocity.y += gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            HandleJump();
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            HandleCrouch();
        }
    }

    void HandleSprint()
    {
        // Check if player is holding shift and moving
        bool wantsToSprint = Input.GetKey(KeyCode.LeftShift) && !isCrouching &&
                            (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) ||
                             Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D));

        isSprinting = wantsToSprint;

        // Handle FOV change for sprint effect
        if (playerCamera != null)
        {
            float targetFOV = isSprinting ? originalFOV + sprintFOVIncrease : originalFOV;
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV,
                                                  Time.deltaTime * fovTransitionSpeed);
        }
    }

    void HandlePlayerMovement()
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");
        Vector3 move = transform.right * moveX + transform.forward * moveZ;

        // Determine current speed based on state
        float currentSpeed = speed;
        if (isCrouching)
            currentSpeed = crouchSpeed;
        else if (isSprinting)
            currentSpeed = sprintSpeed;

        if (pusher != null)
            pusher.SetMoveVector(move * currentSpeed);

        characterController.Move(move * currentSpeed * Time.deltaTime);
    }

    void HandleCameraMovement()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        float verticalLookRotation = playerCamera.transform.localEulerAngles.x - mouseY;
        playerCamera.transform.localRotation = Quaternion.Euler(verticalLookRotation, 0, 0);
    }

    public void HandleJump()
    {
        if (isGrounded && !isCrouching)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    public void HandleCrouch()
    {
        isCrouching = !isCrouching;
        characterController.height = isCrouching ? crouchHeight : standHeight;
        characterController.radius = isCrouching ? 0.2f : 0.5f;
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        if (currentHealth <= 0 && !isDead)
        {
            Die();
        }
    }

    void Die()
    {
        isDead = true;
        // Show respawn screen
        if (respawnScreen != null)
        {
            respawnScreen.SetActive(true);
        }
        // Unlock cursor
        Cursor.lockState = CursorLockMode.None;
    }

    public void Retry()
    {
        // Restart the scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // Public properties for other scripts to check player state
    public bool IsSprinting() => isSprinting;
    public bool IsCrouching() => isCrouching;
    public bool IsGrounded() => isGrounded;
}