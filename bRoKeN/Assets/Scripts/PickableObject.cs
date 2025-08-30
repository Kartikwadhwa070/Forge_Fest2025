using UnityEngine;

public class PickableObject : MonoBehaviour
{
    public int gearID; // Unique ID for each gear
    private bool isHeld = false;
    private Transform player;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Transform originalParent;

    private void Start()
    {
        SpinObject spinner = GetComponent<SpinObject>();
        if (spinner != null) spinner.enabled = false;
        // Store original position and rotation
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        originalParent = transform.parent;
    }

    private void Update()
    {
        if (isHeld && player != null)
        {
            transform.position = player.position + player.forward ;
        }
    }

    public void PickUpObject(Transform playerTransform)
    {
        isHeld = true;
        player = playerTransform;
        GetComponent<Rigidbody>().isKinematic = true;
        SpinObject spinner = GetComponent<SpinObject>();
        if (spinner != null) spinner.enabled = false;
    }

    public void PlaceObject(Transform attachPoint)
    {
        if (attachPoint == null)
        {
            Debug.LogError("AttachPoint is null!");
            return;
        }
        isHeld = false;
        player = null;
        transform.position = attachPoint.position;
        transform.rotation = attachPoint.rotation;
        transform.SetParent(attachPoint);
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
        SpinObject spinner = GetComponent<SpinObject>();
        if (spinner != null)
        {
            spinner.enabled = true;
            spinner.StartSpinning();
        }
    }

    public void DropObject()
    {
        isHeld = false;
        player = null;

        // Reset the object's parent
        transform.SetParent(originalParent);

        // Enable physics
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            // Optional: Add a small force to make the drop feel more natural
            rb.AddForce(Vector3.down * 0.5f, ForceMode.Impulse);
        }

        // Disable spinning if it was spinning
        SpinObject spinner = GetComponent<SpinObject>();
        if (spinner != null)
        {
            spinner.enabled = false;
        }
    }
}