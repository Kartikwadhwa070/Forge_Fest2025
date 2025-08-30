using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class PressurePlate : MonoBehaviour
{
    [Header("Press Logic")]
    [Tooltip("Total mass required to press this plate. 0 = any rigidbody.")]
    public float requiredMass = 1f;

    [Tooltip("Which layers count as 'weight' on the plate.")]
    public LayerMask weightLayers = ~0;

    [Tooltip("Also count the player (CharacterController) as weight.")]
    public bool allowPlayerToPress = true;

    [Tooltip("Approx. mass counted for a player if allowed.")]
    public float playerVirtualMass = 80f;

    [Header("Glow / Audio (uses your WireGlowController)")]
    public WireGlowController glow;     // assign the same materials/audio as your wires
    [Tooltip("Play a short flicker before staying glowing when pressed.")]
    public bool flickerOnPress = true;

    [Header("Events")]
    public UnityEvent onPressed;
    public UnityEvent onReleased;

    // --- runtime ---
    public bool IsPressed { get; private set; }

    Collider triggerCol;
    readonly HashSet<Rigidbody> bodies = new HashSet<Rigidbody>();
    bool playerInside = false;

    [Header("Gating")]
    [Tooltip("If set, this plate ONLY works after the referenced Connect4 is solved.")]
    public Connect4Manager connect4Gate;

    [Tooltip("Require Connect 4 to be solved before this plate can be pressed.")]
    public bool requireConnect4Solved = false;

    bool GateActive => !requireConnect4Solved || (connect4Gate != null && connect4Gate.IsSolved);


    void Awake()
    {
        triggerCol = GetComponent<Collider>();
        triggerCol.isTrigger = true;

        if (glow == null) glow = GetComponentInChildren<WireGlowController>();
        if (glow != null) glow.SetNormalState();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!GateActive) return;
        TryAdd(other);
        Recompute();
    }

    void OnTriggerExit(Collider other)
    {
        if (!GateActive) return;
        TryRemove(other);
        Recompute();
    }

    void OnTriggerStay(Collider other)
    {
        if (!GateActive) return;
        // Handles cases where objects are added while asleep/teleported
        Recompute();
    }

    void TryAdd(Collider col)
    {
        // Player?
        if (allowPlayerToPress && col.GetComponent<CharacterController>() != null)
        {
            playerInside = true;
            return;
        }

        // Rigidbodies in allowed layers
        if (((1 << col.gameObject.layer) & weightLayers) != 0)
        {
            var rb = col.attachedRigidbody;
            if (rb != null && !rb.isKinematic)
                bodies.Add(rb);
        }
    }

    void TryRemove(Collider col)
    {
        if (allowPlayerToPress && col.GetComponent<CharacterController>() != null)
        {
            playerInside = false;
            return;
        }

        var rb = col.attachedRigidbody;
        if (rb != null) bodies.Remove(rb);
    }

    void Recompute()
    {
        if (!GateActive)
        {
            // If the gate turns OFF while something sits here, force a visual+state release.
            if (IsPressed)
            {
                IsPressed = false;
                if (glow != null) glow.SetNormalState(); // uses your WireGlowController
                onReleased?.Invoke();
            }
            return;
        }
        float total = 0f;

        if (playerInside) total += playerVirtualMass;

        foreach (var rb in bodies)
        {
            if (rb == null) continue;
            if (((1 << rb.gameObject.layer) & weightLayers) == 0) continue;
            if (rb.isKinematic) continue;
            total += Mathf.Max(0f, rb.mass);
        }

        bool shouldBePressed = (requiredMass <= 0f) ? (total > 0f) : (total >= requiredMass);
        if (shouldBePressed == IsPressed) return;

        IsPressed = shouldBePressed;
        if (IsPressed)
        {
            if (glow != null)
            {
                if (flickerOnPress) StartCoroutine(glow.FlickerThenGlow());
                else glow.SetGlowingState();
            }
            onPressed?.Invoke();
        }
        else
        {
            if (glow != null) glow.SetNormalState();
            onReleased?.Invoke();
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        var c = GetComponent<Collider>();
        if (c != null && c.isTrigger)
        {
            Gizmos.color = IsPressed ? new Color(0f, 1f, 0f, 0.25f) : new Color(1f, 0.5f, 0f, 0.25f);
            var box = c as BoxCollider;
            if (box)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(box.center, box.size);
            }
        }
    }
#endif
}
