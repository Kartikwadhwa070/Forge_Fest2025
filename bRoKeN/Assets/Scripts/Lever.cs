using UnityEngine;

public class Lever : MonoBehaviour
{
    public int leverID;
    private bool isOn = false;
    private LeverSystem leverSystem;

    private Quaternion offRotation;
    private Quaternion onRotation;
    public float rotationAngle = -45f;
    public float animationSpeed = 5f;

    private void Start()
    {
        leverSystem = FindObjectOfType<LeverSystem>();
        offRotation = transform.rotation;
        onRotation = Quaternion.Euler(rotationAngle, transform.eulerAngles.y, transform.eulerAngles.z);
    }

    public void ToggleLever()
    {
        isOn = !isOn;
        leverSystem.LeverActivated(leverID, isOn);
        StopAllCoroutines();
        StartCoroutine(AnimateLever(isOn ? onRotation : offRotation));
    }

    public void ResetLever()
    {
        if (isOn)
        {
            isOn = false;
            StopAllCoroutines();
            StartCoroutine(AnimateLever(offRotation));
        }
    }

    private System.Collections.IEnumerator AnimateLever(Quaternion targetRotation)
    {
        while (Quaternion.Angle(transform.rotation, targetRotation) > 0.1f)
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation,
                Time.deltaTime * animationSpeed);
            yield return null;
        }
        transform.rotation = targetRotation;
    }
}