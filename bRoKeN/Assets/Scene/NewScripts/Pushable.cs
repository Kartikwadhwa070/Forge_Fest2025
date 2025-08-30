using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Pushable : MonoBehaviour
{
    [Header("Limits")]
    [Tooltip("Clamp the object's maximum linear speed when being pushed. <=0 disables clamping.")]
    public float maxLinearSpeed = 4.5f;

    [Tooltip("Extra drag while being pushed (helps it settle quickly).")]
    public float extraDragWhilePushed = 0.5f;

    [Tooltip("If true, object will never tip over (locks rotation X/Z).")]
    public bool preventTipping = true;

    Rigidbody rb;
    float baseDrag;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        baseDrag = rb.linearDamping;

        if (preventTipping)
        {
            var constraints = rb.constraints;
            constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            rb.constraints = constraints;
        }
    }

    void FixedUpdate()
    {
        // Light speed clamp
        if (maxLinearSpeed > 0f)
        {
            Vector3 v = rb.linearVelocity;
            Vector3 hv = new Vector3(v.x, 0f, v.z);
            if (hv.magnitude > maxLinearSpeed)
            {
                hv = hv.normalized * maxLinearSpeed;
                rb.linearVelocity = new Vector3(hv.x, v.y, hv.z);
            }
        }

        // Extra drag only when moving
        float moving = rb.linearVelocity.sqrMagnitude > 0.01f ? 1f : 0f;
        rb.linearDamping = baseDrag + extraDragWhilePushed * moving;
    }
}
