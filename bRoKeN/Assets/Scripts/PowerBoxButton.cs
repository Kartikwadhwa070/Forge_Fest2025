using UnityEngine;

public class PowerBoxButton : MonoBehaviour
{
    public enum ButtonType { Test, Retry }
    public ButtonType buttonType;
    public PowerBox powerBox;

    private Vector3 originalPosition;
    public float pressDepth = 0.05f;
    public float pressSpeed = 5f;
    private bool isPressed = false;

    private void Start()
    {
        originalPosition = transform.position;
    }

    private void OnMouseDown()
    {
        if (!isPressed)
        {
            isPressed = true;
            StartCoroutine(PressButton());
        }
    }

    private System.Collections.IEnumerator PressButton()
    {
        // Button press animation
        Vector3 pressedPosition = originalPosition - transform.forward * pressDepth;
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * pressSpeed;
            transform.position = Vector3.Lerp(originalPosition, pressedPosition, t);
            yield return null;
        }

        // Trigger appropriate action
        if (buttonType == ButtonType.Test)
        {
            powerBox.TestSequence();
        }
        else if (buttonType == ButtonType.Retry)
        {
            powerBox.RetrySequence();
        }

        // Button release animation
        t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * pressSpeed;
            transform.position = Vector3.Lerp(pressedPosition, originalPosition, t);
            yield return null;
        }

        isPressed = false;
    }
}