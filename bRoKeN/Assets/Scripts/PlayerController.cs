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
    public float gravity = -9.81f;

    private CharacterController characterController;
    private Vector3 velocity;
    private bool isGrounded;

    [Header("Camera")]
    public float mouseSensitivity = 2;

    [Header("Jump & Crouch")]
    public float crouchSpeed = 2.5f;
    public float jumpHeight = 1f;
    public float crouchHeight = 0f;
    public float standHeight = 2f;
    public bool isCrouching = false;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
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

    void HandlePlayerMovement()
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        Vector3 move = transform.right * moveX + transform.forward * moveZ;
        float currentSpeed = isCrouching ? crouchSpeed : speed;
        characterController.Move(move * currentSpeed * Time.deltaTime);
    }

    void HandleCameraMovement()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        transform.Rotate(Vector3.up * mouseX);

        float verticalLookRotation = Camera.main.transform.localEulerAngles.x - mouseY;
        Camera.main.transform.localRotation = Quaternion.Euler(verticalLookRotation, 0, 0);
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

        // Lock cursor
        Cursor.lockState = CursorLockMode.None;
    }

    public void Retry()
    {
        // Restart the scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}