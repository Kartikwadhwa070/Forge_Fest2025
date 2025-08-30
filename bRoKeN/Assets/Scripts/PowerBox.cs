using UnityEngine;

public class PowerBox : MonoBehaviour
{
    public Light indicatorLight;
    public LeverSystem leverSystem;
    public AudioSource deniedSound;
    public GameObject testButtonObject;
    public GameObject retryButtonObject;

    [Header("Light Settings")]
    public float lightIntensity = 5f;

    private void Start()
    {
        indicatorLight.enabled = false;
        SetButtonInteractivity(testButtonObject, false);
        Debug.Log("PowerBox initialized");
    }

    public void TestSequence()
    {
        Debug.Log("TestSequence called on PowerBox");
        if (leverSystem == null)
        {
            Debug.LogError("LeverSystem reference is missing!");
            return;
        }

        if (leverSystem.ValidateSequence())
        {
            Debug.Log("Correct sequence! Turning on light.");
            indicatorLight.enabled = true;
            indicatorLight.intensity = lightIntensity;
        }
        else
        {
            Debug.Log("Wrong sequence! Resetting levers.");
            if (deniedSound != null)
            {
                deniedSound.Play();
            }
            leverSystem.ResetLevers();
        }
    }

    public void RetrySequence()
    {
        Debug.Log("Retrying, resetting levers.");
        indicatorLight.enabled = false;
        leverSystem.ResetLevers();
        SetButtonInteractivity(testButtonObject, false);
    }

    public void EnableTestButton(bool enable)
    {
        SetButtonInteractivity(testButtonObject, enable);
    }

    private void SetButtonInteractivity(GameObject button, bool isEnabled)
    {
        Collider buttonCollider = button.GetComponent<Collider>();
        if (buttonCollider != null)
        {
            buttonCollider.enabled = isEnabled;
        }
    }
}