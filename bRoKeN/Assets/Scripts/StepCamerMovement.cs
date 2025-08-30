using UnityEngine;

public class StepCameraMovement : MonoBehaviour
{
    [Header("Step Movement Settings")]
    public float stepHeight = 0.02f;        // Maximum vertical movement
    public float stepLength = 0.01f;        // Maximum horizontal movement
    public float stepSpeed = 12f;           // Speed of the step cycle
    public float recoverySpeed = 15f;       // How quickly the head returns to neutral position
    public float stepDuration = 0.3f;       // How long each individual step takes

    [Header("References")]
    public Transform cameraTransform;

    private float stepCycle = 0f;           // Tracks the step animation progress
    private float lastStepTime = 0f;        // Time since last step
    private Vector3 originalPosition;        // Starting camera position
    private Vector3 currentOffset;          // Current position offset
    private bool isLeftFoot = false;        // Tracks which foot is stepping

    void Start()
    {
        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;

        originalPosition = cameraTransform.localPosition;
        currentOffset = Vector3.zero;
    }

    void Update()
    {
        bool isMoving = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) ||
                        Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D);

        if (isMoving)
        {
            // Update step cycle
            stepCycle += Time.deltaTime * stepSpeed;

            // Check if it's time for a new step
            if (Time.time - lastStepTime >= stepDuration)
            {
                lastStepTime = Time.time;
                isLeftFoot = !isLeftFoot;
            }

            // Calculate step progress (0 to 1)
            float stepProgress = ((Time.time - lastStepTime) / stepDuration);
            stepProgress = Mathf.Clamp01(stepProgress);

            // Create more natural stepping motion using multiple curves
            float verticalOffset = StepCurve(stepProgress) * stepHeight;
            float horizontalOffset = SwayMotion(stepProgress) * stepLength * (isLeftFoot ? 1 : -1);

            // Apply movement
            currentOffset = Vector3.Lerp(currentOffset,
                new Vector3(horizontalOffset, verticalOffset, 0),
                Time.deltaTime * stepSpeed);
        }
        else
        {
            // Gradually return to neutral position when not moving
            currentOffset = Vector3.Lerp(currentOffset, Vector3.zero,
                Time.deltaTime * recoverySpeed);
            stepCycle = 0f;
            lastStepTime = 0f;
        }

        // Apply final position
        cameraTransform.localPosition = originalPosition + currentOffset;
    }

    private float StepCurve(float t)
    {
        // Creates a quick up motion followed by a slower down motion
        if (t < 0.5f)
            return Mathf.Sin(t * Mathf.PI) * 0.5f;
        else
            return Mathf.Sin(t * Mathf.PI) * 0.5f * (1 - t);
    }

    private float SwayMotion(float t)
    {
        // Creates a gentle side-to-side sway
        return Mathf.Sin(t * Mathf.PI * 2) * 0.5f;
    }
}