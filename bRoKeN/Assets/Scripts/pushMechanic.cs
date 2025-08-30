using UnityEngine;

public class PushMechanic : MonoBehaviour
{
    public float pushForce = 5f;        // Strength of the push
    public float pushDistance = 2f;    // Max distance to detect pushable objects
    public Transform playerCamera;     // Camera for raycasting (assign in Inspector)

    private CharacterController characterController;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.E)) // Check if push key is held
        {
            TryPush();
        }
    }

    void TryPush()
    {
        Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        RaycastHit hit;

        // Cast a ray to detect objects in front
        if (Physics.Raycast(ray, out hit, pushDistance))
        {
            if (hit.collider.CompareTag("Pushable")) // Object must have the tag "Pushable"
            {
                Rigidbody pushableRB = hit.collider.attachedRigidbody;

                if (pushableRB != null)
                {
                    // Calculate push direction (ignore Y-axis for realism)
                    Vector3 pushDirection = new Vector3(playerCamera.forward.x, 0, playerCamera.forward.z).normalized;

                    // Apply force to the object
                    pushableRB.AddForce(pushDirection * pushForce, ForceMode.Force);
                }
            }
        }
    }
}
