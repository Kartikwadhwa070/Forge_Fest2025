using UnityEngine;

public class BrickInteraction : MonoBehaviour
{
    public float raycastDistance = 5f; // Maximum interaction distance

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // Left mouse button click
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition); // Cast a ray from the camera
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, raycastDistance))
            {
                Brick brick = hit.collider.GetComponent<Brick>();
                if (brick != null)
                {
                    brick.Interact(); // Call the Interact() method
                }
            }
        }
    }
}
