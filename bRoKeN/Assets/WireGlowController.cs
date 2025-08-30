using UnityEngine;
using System.Collections;

public class WireGlowController : MonoBehaviour
{
    [Header("Materials")]
    public Material normalMaterial;  // Assign the normal material
    public Material glowingMaterial; // Assign the glowing material
    public bool isGlowing = false;
    private Renderer wireRenderer;   // Renderer of the wire

    [Header("Flicker Settings")]
    public float flickerDuration = 1f; // How long the flicker effect lasts
    public float flickerInterval = 0.1f; // Time between each flicker

    [Header("Audio Settings")]
    public AudioSource audioSource;      // Reference to the AudioSource component
    public AudioClip toggleOnSound;      // Sound to play when lights toggle on

    private void Awake()
    {
        wireRenderer = GetComponent<Renderer>();
        SetNormalState(); // Default to normal state

        // Ensure AudioSource is set up
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private void Update()
    {
        //if (Input.GetKeyDown(KeyCode.E))
        //{
        //    if (isGlowing)
        //    {
        //        StopAllCoroutines(); // Stop any ongoing flicker
        //        SetNormalState();
        //    }
        //    else
        //    {
        //        StartCoroutine(FlickerThenGlow());
        //    }
        //}
    }

    // Coroutine for the flickering effect
    public IEnumerator FlickerThenGlow()
    {
        float elapsedTime = 0f;
        PlayToggleOnSound();
        while (elapsedTime < flickerDuration)
        {
            // Toggle between glowing and normal
            wireRenderer.material = (elapsedTime % (flickerInterval * 2) < flickerInterval)
                ? glowingMaterial
                : normalMaterial;

            elapsedTime += flickerInterval;
            yield return new WaitForSeconds(flickerInterval);
        }

        // After flickering, set to glowing state and play sound
        SetGlowingState();
        
    }

    // Call this to set the wire to its normal state
    public void SetNormalState()
    {
        wireRenderer.material = normalMaterial;
        isGlowing = false;
    }

    // Call this to make the wire glow
    public void SetGlowingState()
    {
        wireRenderer.material = glowingMaterial;
        isGlowing = true;
    }

    // Play the toggle-on sound effect
    private void PlayToggleOnSound()
    {
        if (audioSource != null && toggleOnSound != null)
        {
            audioSource.clip = toggleOnSound;
            audioSource.Play();
        }
    }
}
