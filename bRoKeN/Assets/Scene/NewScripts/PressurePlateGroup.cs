using UnityEngine;
using UnityEngine.Events;

public class PressurePlateGroup : MonoBehaviour
{
    [Header("Plates to Monitor")]
    public PressurePlate[] plates;

    [Header("Events")]
    public UnityEvent onAllPressed;   // fires when all plates are pressed
    public UnityEvent onAnyReleased;  // fires when any plate releases (optional)

    [Header("Behavior")]
    [Tooltip("If true, onAllPressed fires only once until ResetGroup() is called.")]
    public bool latchWhenSolved = true;

    bool solved;

    [Header("Simon Says Activation")]
    [Tooltip("Simon Says puzzle to enable once ALL plates are pressed.")]
    public SimonSays simonSays;

    [Tooltip("Wires to flicker+glow when Simon unlocks (player feedback).")]
    public WireGlowController[] wiresToTurnOn;

    [Tooltip("Keep Simon un-interactable until plates puzzle is solved.")]
    public bool gateSimonUntilSolved = true;

    [Tooltip("Flicker wires before glowing when unlocking Simon.")]
    public bool flickerOnActivate = true;

    [Header("Connect4 Gate")]
    [Tooltip("If assigned and required, ALL plates stay disabled until this Connect4 is solved.")]
    public Connect4Manager connect4Gate;

    [Tooltip("If true, group ignores input and disables all plate colliders until Connect4 is solved.")]
    public bool requireConnect4Solved = true;

    [Tooltip("If true, when locked, reset all plate glows to normal for visual clarity.")]
    public bool forceResetGlowWhileLocked = true;



    void OnEnable()
    {
        // Cheap polling keeps setup simple (no manual event wiring)
        InvokeRepeating(nameof(CheckState), 0.1f, 0.1f);
        ApplyGateState(); // NEW

        // Lock Simon at start if gating is on
        if (gateSimonUntilSolved && simonSays != null)
            simonSays.SetInteractionEnabled(false);
    }

    void OnDisable()
    {
        CancelInvoke(nameof(CheckState));
    }

    void CheckState()
    {
        // NEW: keep plate colliders synced with gate state
        ApplyGateState();

        // If locked, do nothing else this tick
        if (requireConnect4Solved && connect4Gate != null && !connect4Gate.IsSolved)
            return;

        if (plates == null || plates.Length == 0) return;

        bool all = true;
        for (int i = 0; i < plates.Length; i++)
        {
            var p = plates[i];
            if (p == null || !p.IsPressed)
            {
                all = false;
                break;
            }
        }

        if (all)
        {
            if (!solved)
            {
                solved = true;
                onAllPressed?.Invoke();
            }
            if (!latchWhenSolved) solved = false;
        }
        else
        {
            if (solved && !latchWhenSolved) onAnyReleased?.Invoke();
            if (!latchWhenSolved) solved = false;
        }
    }
    void ApplyGateState()
    {
        bool locked = requireConnect4Solved && connect4Gate != null && !connect4Gate.IsSolved; // ← uses Connect4Manager.IsSolved
        if (plates == null) return;

        foreach (var p in plates)
        {
            if (!p) continue;
            var col = p.GetComponent<Collider>();
            if (col) col.enabled = !locked;              // hard-disable interaction

            if (locked && forceResetGlowWhileLocked && p.glow)
                p.glow.SetNormalState();                 // make it obvious they’re off
        }
    }



    public void ResetGroup()
    {
        solved = false;

        // Optionally, reset glow on plates
        foreach (var p in plates)
            if (p && p.glow) p.glow.SetNormalState();

        // Relock Simon on reset if gating is on
        if (gateSimonUntilSolved && simonSays != null)
            simonSays.SetInteractionEnabled(false);
        ApplyGateState(); // NEW: re-apply gate after reset
    }
}
