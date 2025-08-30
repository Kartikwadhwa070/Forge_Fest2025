using UnityEngine;
using System.Collections;          // needed for StartCoroutine
using UnityEngine.Events;

public class PressurePlateGroup : MonoBehaviour
{
    [Header("Plates to Monitor")]
    public PressurePlate[] plates;

    [Header("Behavior")]
    [Tooltip("If true, fire the solve actions only once until ResetGroup() is called.")]
    public bool latchWhenSolved = true;

    private bool solved;

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

    // (Optional) still expose events if you want extra hooks in the editor
    [Header("Optional Events")]
    public UnityEvent onAllPressed;   // fires when all plates are pressed (after built-in actions)
    public UnityEvent onAnyReleased;  // fires when any plate releases (when not latched)

    void OnEnable()
    {
        // Polling keeps setup simple (no manual event wiring)
        InvokeRepeating(nameof(CheckState), 0.1f, 0.1f);

        ApplyGateState(); // disable all plate colliders until Connect 4 is solved

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
        // Keep plate colliders synced with the Connect4 gate
        ApplyGateState();

        // If Connect 4 hasn’t been solved yet, ignore plate logic entirely
        if (requireConnect4Solved && connect4Gate != null && !connect4Gate.IsSolved)
            return; // uses Connect4Manager.IsSolved :contentReference[oaicite:0]{index=0}

        if (plates == null || plates.Length == 0) return;

        bool allPressed = true;
        for (int i = 0; i < plates.Length; i++)
        {
            var p = plates[i];
            if (p == null || !p.IsPressed)
            {
                allPressed = false;
                break;
            }
        }

        if (allPressed)
        {
            if (!solved)
            {
                solved = true;

                // ---- BUILT-IN ACTIONS ----
                // 1) Unlock Simon
                if (simonSays != null && gateSimonUntilSolved)
                    simonSays.SetInteractionEnabled(true);

                // 2) Player feedback: wires flicker then glow
                if (wiresToTurnOn != null)
                {
                    foreach (var w in wiresToTurnOn)
                    {
                        if (!w) continue;
                        if (flickerOnActivate) StartCoroutine(w.FlickerThenGlow()); // WireGlowController coroutine
                        else w.SetGlowingState();
                    }
                }
                // --------------------------

                // Optional editor hooks
                onAllPressed?.Invoke();
            }

            if (!latchWhenSolved)
                solved = false; // allow repeated firing if desired
        }
        else
        {
            if (solved && !latchWhenSolved)
                onAnyReleased?.Invoke();

            if (!latchWhenSolved)
                solved = false;
        }
    }

    void ApplyGateState()
    {
        bool locked = requireConnect4Solved && connect4Gate != null && !connect4Gate.IsSolved;
        if (plates == null) return;

        foreach (var p in plates)
        {
            if (!p) continue;

            // Hard-disable interaction on every plate while locked
            var col = p.GetComponent<Collider>();
            if (col) col.enabled = !locked;

            // Reset glow for clear “off” visuals
            if (locked && forceResetGlowWhileLocked && p.glow)
                p.glow.SetNormalState(); // WireGlowController normal/glow swap :contentReference[oaicite:1]{index=1}
        }
    }

    public void ResetGroup()
    {
        solved = false;

        // Reset plate visuals
        foreach (var p in plates)
            if (p && p.glow) p.glow.SetNormalState();

        // Relock Simon on reset if gating is on
        if (gateSimonUntilSolved && simonSays != null)
            simonSays.SetInteractionEnabled(false);

        // Re-apply Connect4 gate after reset
        ApplyGateState();
    }
}
