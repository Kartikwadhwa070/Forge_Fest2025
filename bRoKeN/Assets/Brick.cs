using UnityEngine;

public class Brick : MonoBehaviour
{
    [Header("Positions")]
    [SerializeField] private Vector3 pressedOffset = new Vector3(0, 0, -0.1f);
    [SerializeField] private Vector3 originalLocalPosition;

    [Header("Dependencies")]
    public WireGlowController requiredWire; // Reference to the wire that needs to glow
    public BrickPuzzleManager puzzleManager; // Assign in Inspector or via script

    private bool isPressed = false;

    private void Awake()
    {
        // Store the brick's starting local position so we can return to it.
        originalLocalPosition = transform.localPosition;
    }

    // This method gets called when the player interacts (clicks or uses) this brick.
    public void Interact()
    {
        if (requiredWire != null && !requiredWire.isGlowing)
        {
            Debug.Log("The wire is not glowing. This brick cannot be interacted with.");
            return; // Do nothing if the required wire is not glowing
        }

        Debug.Log("Interacted with the brick.");
        ToggleBrickState();
    }

    private void ToggleBrickState()
    {
        if (!isPressed)
        {
            // Move it inward by pressedOffset
            transform.localPosition = originalLocalPosition + pressedOffset;
            isPressed = true;
        }
        else
        {
            // Move it back to original position
            transform.localPosition = originalLocalPosition;
            isPressed = false;
        }

        // Let the puzzle manager know something changed
        if (puzzleManager != null)
        {
            puzzleManager.CheckPuzzleState();
        }
    }

    // Optional: Provide a way for the manager to check if this brick is pressed.
    public bool IsPressed()
    {
        return isPressed;
    }
}
