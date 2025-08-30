using UnityEngine;
using System.Collections;

public class PhysicalButton : MonoBehaviour
{
    [Tooltip("The index of the column this button will trigger (0-based).")]
    public int columnIndex = 0;

    [Tooltip("Reference to your Connect4Manager script.")]
    public Connect4Manager connect4Manager;

    [Tooltip("How far the button moves when pressed.")]
    public float pressDepth = 0.1f;

    [Tooltip("Duration of the press animation (seconds).")]
    public float pressDuration = 0.1f;

    private Vector3 initialPosition;

    private void Start()
    {
        initialPosition = transform.localPosition;
        Debug.Log($"Button initialized at position: {initialPosition}");
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0)) // Left mouse button
        {
            Debug.Log("Mouse button clicked.");
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                Debug.Log($"Raycast hit: {hit.collider.gameObject.name}");
                if (hit.collider != null && hit.collider.gameObject == gameObject)
                {
                    Debug.Log("Raycast hit the button. Triggering button press.");
                    OnButtonPressed();
                }
                else
                {
                    Debug.Log("Raycast did not hit this button.");
                }
            }
            else
            {
                Debug.Log("Raycast did not hit anything.");
            }
        }
    }

    private void OnButtonPressed()
    {
        Debug.Log($"Button pressed! Column index: {columnIndex}");

        if (connect4Manager != null)
        {
            Debug.Log("Connect4Manager reference found. Calling OnColumnButtonPressed.");
            connect4Manager.OnColumnButtonPressed(columnIndex);
        }
        else
        {
            Debug.LogError("Connect4Manager reference is missing on PhysicalButton.");
        }

        StartCoroutine(AnimateButtonPress());
    }

    private IEnumerator AnimateButtonPress()
    {
        Vector3 pressedPosition = initialPosition - new Vector3(0, pressDepth, 0);

        Debug.Log("Starting button press animation.");
        float elapsedTime = 0;

        // Animate button press downward
        while (elapsedTime < pressDuration)
        {
            transform.localPosition = Vector3.Lerp(initialPosition, pressedPosition, elapsedTime / pressDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.localPosition = pressedPosition;
        Debug.Log("Button pressed down.");

        // Wait while button is pressed
        yield return new WaitForSeconds(0.1f);

        elapsedTime = 0;
        Debug.Log("Starting button release animation.");

        // Animate button release upward
        while (elapsedTime < pressDuration)
        {
            transform.localPosition = Vector3.Lerp(pressedPosition, initialPosition, elapsedTime / pressDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.localPosition = initialPosition;
        Debug.Log("Button returned to initial position.");
    }
}
