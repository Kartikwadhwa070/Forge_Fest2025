using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    public float interactionDistance = 3f;
    public Transform holdPoint;
    private PickableObject heldObject;

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // Left-click to interact
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit, interactionDistance))
            {
                // Check for a wall to place the object
                if (heldObject != null)
                {
                    WallAttachment wall = hit.collider.GetComponent<WallAttachment>();
                    if (wall != null)
                    {
                        wall.AttachObject(heldObject);
                        heldObject = null; // Object is placed
                        return;
                    }
                }
                // Check for a lever interaction
                Lever lever = hit.collider.GetComponent<Lever>();
                if (lever != null)
                {
                    lever.ToggleLever();
                    return; // Stop checking other interactions
                }
            }
        }
        if (Input.GetMouseButtonDown(1)) // Right-click to pick up or drop
        {
            if (heldObject == null) // If not holding anything, try to pick up
            {
                TryPickUp();
            }
            else // If holding something, drop it
            {
                DropObject();
            }
        }
    }

    void TryPickUp()
    {
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit, interactionDistance))
        {
            PickableObject pickable = hit.collider.GetComponent<PickableObject>();
            if (pickable != null)
            {
                heldObject = pickable;
                pickable.PickUpObject(holdPoint);
            }
        }
    }

    void DropObject()
    {
        if (heldObject != null)
        {
            heldObject.DropObject();
            heldObject = null;
        }
    }
}