using UnityEngine;

public class SpinObject : MonoBehaviour
{
    private bool spinning = false;
    public float spinSpeed = 100f;

    // Add dropdown to select rotation axis in Inspector
    public enum RotationAxis
    {
        X,  // Rotate around X axis (Vector3.right)
        Y,  // Rotate around Y axis (Vector3.up)
        Z   // Rotate around Z axis (Vector3.forward)
    }

    public RotationAxis rotationAxis = RotationAxis.Y;

    private Vector3 GetRotationAxis()
    {
        switch (rotationAxis)
        {
            case RotationAxis.X:
                return Vector3.right;
            case RotationAxis.Y:
                return Vector3.up;
            case RotationAxis.Z:
                return Vector3.forward;
            default:
                return Vector3.up;
        }
    }

    private void Update()
    {
        if (spinning)
        {
            transform.Rotate(GetRotationAxis(), spinSpeed * Time.deltaTime);
        }
    }

    public void StartSpinning()
    {
        spinning = true;
        Debug.Log($"[SpinObject] StartSpinning called on {gameObject.name}, spinning = {spinning}");
    }
}