using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class LeverSystem : MonoBehaviour
{
    public PowerBox powerBox;
    public Animator trainAnimator; // Reference to the Animator component
    public AudioSource audioSource; // Reference to the AudioSource component

    private Dictionary<int, bool> leverStates = new Dictionary<int, bool>();

    [Header("Correct Lever Sequence")]
    [Tooltip("Set the correct sequence of lever numbers that need to be activated")]
    [SerializeField]
    private int[] correctSequence = new int[] { 1, 3, 2, 4 }; // Set this in Inspector

    [Header("Wire Dependency")]
    [Tooltip("The wire that must be glowing to activate the lever system")]
    public WireGlowController requiredWire; // Reference to the wire that controls functionality

    [Header("Wires to Glow on Completion")]
    [Tooltip("List of wires that start glowing when the sequence is correct")]
    public List<WireGlowController> wiresToGlow; // Wires to flicker and glow when the train is enabled

    private bool systemEnabled = false; // Tracks if the system is active

    private void Start()
    {
        if (requiredWire != null && !requiredWire.isGlowing)
        {
            Debug.Log("[LeverSystem] Waiting for the required wire to glow...");
            StartCoroutine(WaitForRequiredWire());
        }
        else
        {
            systemEnabled = true; // Enable functionality if the wire is already glowing
        }
    }

    public void LeverActivated(int leverID, bool isOn)
    {
        if (!systemEnabled)
        {
            Debug.LogWarning("[LeverSystem] System is currently disabled. Waiting for the required wire to turn on.");
            return;
        }

        leverStates[leverID] = isOn;

        if (isOn)
        {
            CheckProgress();
        }
    }

    private void CheckProgress()
    {
        if (!systemEnabled) return;

        List<int> currentSequence = new List<int>();
        foreach (var lever in leverStates)
        {
            if (lever.Value)  // if lever is on
            {
                currentSequence.Add(lever.Key);
            }
        }

        // Enable test button only when the same number of levers are activated
        powerBox.EnableTestButton(currentSequence.Count == correctSequence.Length);
    }

    public bool ValidateSequence()
    {
        if (!systemEnabled)
        {
            Debug.LogWarning("[LeverSystem] System is currently disabled. Waiting for the required wire to turn on.");
            return false;
        }

        List<int> currentSequence = new List<int>();
        foreach (var lever in leverStates)
        {
            if (lever.Value)
            {
                currentSequence.Add(lever.Key);
            }
        }

        if (currentSequence.Count != correctSequence.Length)
        {
            return false;
        }

        for (int i = 0; i < correctSequence.Length; i++)
        {
            if (currentSequence[i] != correctSequence[i])
            {
                return false;
            }
        }

        // If the sequence is correct, trigger the train animation, play audio, and start wire flicker
        if (trainAnimator != null)
        {
            trainAnimator.SetTrigger("Train");
        }

        if (audioSource != null)
        {
            audioSource.Play(); // Play the audio clip
        }

        // Trigger wire flickering and glowing
        TriggerWireFlickerAndGlow();

        return true;
    }

    public void ResetLevers()
    {
        if (!systemEnabled)
        {
            Debug.LogWarning("[LeverSystem] System is currently disabled. Waiting for the required wire to turn on.");
            return;
        }

        leverStates.Clear();

        // Find and reset all lever objects
        Lever[] allLevers = FindObjectsOfType<Lever>();
        foreach (Lever lever in allLevers)
        {
            lever.ResetLever();
        }
    }

    private IEnumerator WaitForRequiredWire()
    {
        // Wait until the required wire is glowing
        while (requiredWire != null && !requiredWire.isGlowing)
        {
            yield return null; // Wait until the wire becomes glowing
        }

        Debug.Log("[LeverSystem] Required wire is glowing! System functionality enabled.");
        systemEnabled = true; // Enable the system
    }

    private void TriggerWireFlickerAndGlow()
    {
        if (wiresToGlow == null || wiresToGlow.Count == 0)
        {
            Debug.LogWarning("[LeverSystem] No wires assigned to glow on completion.");
            return;
        }

        foreach (WireGlowController wire in wiresToGlow)
        {
            if (wire != null)
            {
                StartCoroutine(wire.FlickerThenGlow());
                Debug.Log($"[LeverSystem] Flickering and glowing wire: {wire.name}");
            }
        }
    }
}
