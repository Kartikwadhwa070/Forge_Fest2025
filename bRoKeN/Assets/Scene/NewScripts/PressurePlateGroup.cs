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


    void OnEnable()
    {
        // Cheap polling keeps setup simple (no manual event wiring)
        InvokeRepeating(nameof(CheckState), 0.1f, 0.1f);

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

                // 🔓 Unlock Simon
                if (simonSays != null && gateSimonUntilSolved)
                    simonSays.SetInteractionEnabled(true);

                // ✨ Player feedback: wires flicker then glow
                if (wiresToTurnOn != null)
                {
                    foreach (var w in wiresToTurnOn)
                    {
                        if (!w) continue;
                        if (flickerOnActivate) StartCoroutine(w.FlickerThenGlow()); // visual+sfx
                        else w.SetGlowingState();                                  // instant glow
                    }
                }

                onAllPressed?.Invoke();
            }
            if (!latchWhenSolved) solved = false; // allow repeated firing if desired
        }
        else
        {
            if (solved && !latchWhenSolved) onAnyReleased?.Invoke();
            if (!latchWhenSolved) solved = false;
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
    }
}
