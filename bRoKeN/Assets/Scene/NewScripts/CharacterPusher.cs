using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class CharacterPusher : MonoBehaviour
{
    [Header("General")]
    [Tooltip("Layers that can be pushed (set a 'Pushable' layer and assign your crates).")]
    public LayerMask pushableLayers = ~0; // everything by default

    [Tooltip("Only push while the CharacterController is grounded.")]
    public bool requireGrounded = true;

    [Tooltip("Minimum player move speed before pushing starts (meters/second).")]
    public float minMoveSpeed = 0.2f;

    [Header("Push Force")]
    [Tooltip("Base push strength applied along the player's horizontal move direction.")]
    public float pushStrength = 6f;

    [Tooltip("Scale push by the player's current speed.")]
    public bool scaleWithPlayerSpeed = true;

    [Tooltip("Maximum mass of rigidbodies you can push. Set <= 0 to ignore mass limit.")]
    public float maxPushMass = 150f;

    [Tooltip("Do not add vertical force (prevents tipping when walking up to objects).")]
    public bool zeroOutVertical = true;

    [Header("Contact Settings")]
    [Tooltip("Only push when hitting near the player's forward direction (degrees). 0 = straight ahead, 180 = any direction.")]
    [Range(0f, 180f)] public float maxContactAngleFromForward = 70f;

    [Tooltip("Ignore pushing if the hit normal is too steep (e.g., walls you 'scrape' along).")]
    [Range(0f, 90f)] public float maxSurfaceSlope = 80f;

    [Header("Stability")]
    [Tooltip("Cap the added velocity per FixedUpdate to avoid jitter/explosions.")]
    public float maxVelocityChangePerHit = 3.5f;

    CharacterController cc;
    Vector3 lastMoveDir;   // set this from your own movement script if you want; else we infer it
    float lastSpeed;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
    }

    /// <summary>
    /// Optionally call this from your movement script each frame
    /// to provide a more accurate move direction & speed.
    /// </summary>
    public void SetMoveVector(Vector3 worldMove)
    {
        lastSpeed = worldMove.magnitude;
        lastMoveDir = (lastSpeed > 0.0001f) ? worldMove.normalized : Vector3.zero;
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // Only interact with rigidbodies
        var rb = hit.rigidbody;
        if (rb == null || rb.isKinematic) return;

        // Layer check
        if (((1 << rb.gameObject.layer) & pushableLayers) == 0) return;

        // Grounded?
        if (requireGrounded && !cc.isGrounded) return;

        // Decide move direction: prefer externally provided, else infer from character velocity
        Vector3 horizontalVel = new Vector3(cc.velocity.x, 0f, cc.velocity.z);
        if (horizontalVel.sqrMagnitude > 0.0001f)
        {
            lastMoveDir = horizontalVel.normalized;
            lastSpeed = horizontalVel.magnitude;
        }

        // Need to be moving
        if (lastSpeed < minMoveSpeed) return;

        // Angle gating: only push things roughly in front of the player
        float angle = Vector3.Angle(transform.forward, hit.point - transform.position);
        if (angle > maxContactAngleFromForward) return;

        // Ignore super-steep contact surfaces (prevents weird side pushes)
        float surfaceAngle = Vector3.Angle(hit.normal, Vector3.up); // 0 = floor, 90 = wall
        if (surfaceAngle > maxSurfaceSlope) return;

        // Respect mass cap (if any)
        if (maxPushMass > 0f && rb.mass > maxPushMass) return;

        // Build push direction (horizontal only if desired)
        Vector3 pushDir = lastMoveDir;
        if (zeroOutVertical) pushDir.y = 0f;
        pushDir = pushDir.normalized;

        // Strength
        float strength = pushStrength * (scaleWithPlayerSpeed ? Mathf.Clamp01(lastSpeed) : 1f);

        // Convert to a velocity change (ForceMode.VelocityChange is mass-independent)
        Vector3 deltaV = pushDir * strength;

        // Clamp to avoid spikes on high-FPS collision spam
        if (deltaV.magnitude > maxVelocityChangePerHit)
            deltaV = deltaV.normalized * maxVelocityChangePerHit;

        // Apply in FixedUpdate context (safe if called here—Unity queues it until physics step)
        rb.AddForce(deltaV, ForceMode.VelocityChange);

        // Optional: nudge angular velocity a bit for nicer crate rotation
        // rb.AddTorque(Vector3.up * 0.2f, ForceMode.VelocityChange);
    }
}
