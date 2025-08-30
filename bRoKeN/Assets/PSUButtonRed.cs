using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class PSUButtonRed : MonoBehaviour
{
    [Header("Button Settings")]
    public Transform buttonTransform; // The transform of the button part to move
    public Vector3 pressOffset = new Vector3(0, -0.1f, 0); // Offset for the pressed state
    public float pressSpeed = 0.2f; // Speed of the button press animation

    [Header("Wire Control")]
    public List<WireGlowController> wires; // List of wires to toggle

    [Header("Audio")]
    public AudioSource audioSource; // Audio source to play sound
    public AudioClip pressSound; // Sound for button press

    private Vector3 originalPosition; // Initial position of the button
    private bool isPressed = false; // Tracks whether the button is pressed

    private void Awake()
    {
        // Save the original position of the button
        if (buttonTransform == null)
        {
            buttonTransform = transform;
        }
        originalPosition = buttonTransform.localPosition;

        // Ensure the audio source exists
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private void OnMouseDown()
    {
        if (!isPressed)
        {
            StartCoroutine(PressButton());
        }
    }

    public IEnumerator PressButton()
    {
        isPressed = true;

        // Play press sound
        if (audioSource != null && pressSound != null)
        {
            audioSource.PlayOneShot(pressSound);
        }

        // Move button down (pressed)
        Vector3 targetPosition = originalPosition + pressOffset;
        while (Vector3.Distance(buttonTransform.localPosition, targetPosition) > 0.01f)
        {
            buttonTransform.localPosition = Vector3.MoveTowards(buttonTransform.localPosition, targetPosition, pressSpeed * Time.deltaTime);
            yield return null;
        }

        // Toggle wires
        ToggleWires();

        // Wait briefly before releasing the button
        yield return new WaitForSeconds(0.2f);

        // Move button back to original position
        while (Vector3.Distance(buttonTransform.localPosition, originalPosition) > 0.01f)
        {
            buttonTransform.localPosition = Vector3.MoveTowards(buttonTransform.localPosition, originalPosition, pressSpeed * Time.deltaTime);
            yield return null;
        }

        isPressed = false;
    }

    private void ToggleWires()
    {
        foreach (WireGlowController wire in wires)
        {
            if (wire != null)
            {
                if (wire.isGlowing)
                {
                    wire.SetNormalState();
                }
                else
                {
                    StartCoroutine(wire.FlickerThenGlow());
                    //wire.SetGlowingState();
                }
            }
        }
    }
}
