using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class WallAttachment : MonoBehaviour
{
    [System.Serializable]
    public class AttachPoint
    {
        public Transform point;
        public int requiredGearID; // Only gears with this ID can attach here
    }

    [Header("Attach Points")]
    public AttachPoint[] attachPoints;
    private int attachedObjects = 0;

    [Header("Wall Animation and Audio")]
    public Animator wallAnimator; // Reference to the wall's Animator component
    public string animationTrigger = "AllGearsPlaced"; // Name of the animation trigger
    public AudioSource audioSource; // Reference to the AudioSource component
    public AudioClip completionSound; // The audio clip to play when all gears are attached
    public AudioClip loopMusic; // The audio clip that will loop after the puzzle is completed

    [Header("Wire Control")]
    public WireGlowController requiredWire; // The wire that must be glowing to enable functionality
    public List<WireGlowController> wiresToFlicker; // List of wires to flicker and glow

    private bool functionalityEnabled = false; // Flag to control script functionality

    private void Start()
    {
        if (attachPoints == null || attachPoints.Length == 0)
        {
            Debug.LogError("[WallAttachment] No attach points assigned!");
        }

        // Ensure the Animator and AudioSource are assigned
        if (wallAnimator == null)
        {
            Debug.LogError("[WallAttachment] Animator not assigned!");
        }
        if (audioSource == null)
        {
            Debug.LogError("[WallAttachment] AudioSource not assigned!");
        }
        if (completionSound == null)
        {
            Debug.LogError("[WallAttachment] Completion sound not assigned!");
        }
        if (loopMusic == null)
        {
            Debug.LogError("[WallAttachment] Loop music not assigned!");
        }

        // Freeze functionality until the required wire is glowing
        if (requiredWire != null && !requiredWire.isGlowing)
        {
            StartCoroutine(WaitForRequiredWire());
        }
        else
        {
            functionalityEnabled = true; // Enable functionality immediately if no required wire is set or it's already glowing
        }
    }

    public void AttachObject(PickableObject pickable)
    {
        if (!functionalityEnabled)
        {
            Debug.LogWarning("[WallAttachment] Functionality is currently disabled. Waiting for the required wire to turn on.");
            return;
        }

        if (pickable == null)
        {
            Debug.LogError("[WallAttachment] PickableObject is null!");
            return;
        }

        foreach (AttachPoint attachPoint in attachPoints)
        {
            if (attachPoint.point.childCount == 0 && pickable.gearID == attachPoint.requiredGearID)
            {
                Debug.Log($"[WallAttachment] Attaching {pickable.gameObject.name} to {attachPoint.point.name}");

                // Enable spinning
                SpinObject spinner = pickable.GetComponent<SpinObject>();
                if (spinner != null) spinner.enabled = true;

                pickable.PlaceObject(attachPoint.point);
                attachedObjects++;

                if (attachedObjects == attachPoints.Length)
                {
                    Debug.Log("[WallAttachment] All objects attached!");
                    PlayCompletionAnimationAndSound(); // Play animation and sound when all gears are attached
                }
                return;
            }
        }

        Debug.Log($"[WallAttachment] {pickable.gameObject.name} does not match any attach point!");
    }

    private void PlayCompletionAnimationAndSound()
    {
        // Trigger animation
        if (wallAnimator != null)
        {
            wallAnimator.SetTrigger(animationTrigger);
        }

        // Play completion sound
        if (audioSource != null && completionSound != null)
        {
            audioSource.PlayOneShot(completionSound);
        }

        // Set the loop music to play continuously after the puzzle is completed
        if (audioSource != null && loopMusic != null)
        {
            audioSource.clip = loopMusic;
            audioSource.loop = true;
            audioSource.Play();
        }

        // Trigger wire flicker and glow
        TriggerWireFlickerAndGlow();
    }

    private void TriggerWireFlickerAndGlow()
    {
        if (wiresToFlicker != null && wiresToFlicker.Count > 0)
        {
            foreach (WireGlowController wire in wiresToFlicker)
            {
                if (wire != null)
                {
                    StartCoroutine(wire.FlickerThenGlow());
                }
            }
        }
        else
        {
            Debug.LogWarning("[WallAttachment] No wires assigned to flicker and glow!");
        }
    }

    private IEnumerator WaitForRequiredWire()
    {
        Debug.Log("[WallAttachment] Waiting for the required wire to be turned on...");
        while (requiredWire != null && !requiredWire.isGlowing)
        {
            yield return null; // Wait until the required wire is glowing
        }
        Debug.Log("[WallAttachment] Required wire is glowing! Functionality enabled.");
        functionalityEnabled = true;
    }
}
