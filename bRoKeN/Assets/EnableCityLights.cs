using UnityEngine;
using System.Collections;

public class EnableCityLightsWithFlicker : MonoBehaviour
{
    [Header("Wire State")]
    public WireGlowController wireController; // Reference to the WireGlowController
    public bool enableOnGlowing = true; // Whether to enable objects when the wire starts glowing

    [Header("City Lights")]
    public string cityLightsTag = "CityLights"; // Tag used to identify CityLights objects
    public float flickerDuration = 1.5f; // Total duration of the flicker effect
    public float flickerInterval = 0.1f; // Time between flickers
    public float lightOnIntensity = 1.9f; // Intensity when lights are turned on
    public float lightOffIntensity = 0f; // Intensity when lights are turned off

    private bool lightsEnabled = false; // Track if the lights are already enabled

    private void Awake()
    {
        DisableCityLightsObjects(); // Disable all CityLights objects on Awake
    }

    private void Update()
    {
        // Check if the wire is glowing and lights are not yet enabled
        if (wireController != null && wireController.isGlowing && !lightsEnabled && enableOnGlowing)
        {
            StartCoroutine(FlickerThenEnableCityLights());
        }
    }

    private IEnumerator FlickerThenEnableCityLights()
    {
        lightsEnabled = true; // Prevent multiple calls

        GameObject[] cityLights = GameObject.FindGameObjectsWithTag(cityLightsTag);
        float elapsedTime = 0f;

        while (elapsedTime < flickerDuration)
        {
            foreach (GameObject lightObject in cityLights)
            {
                Light lightComponent = lightObject.GetComponent<Light>();
                if (lightComponent != null)
                {
                    // Alternate between on and off intensities
                    lightComponent.intensity = (elapsedTime % (flickerInterval * 2) < flickerInterval) ? lightOffIntensity : lightOnIntensity;
                }
            }

            elapsedTime += flickerInterval;
            yield return new WaitForSeconds(flickerInterval);
        }

        // After flickering, enable the lights fully
        foreach (GameObject lightObject in cityLights)
        {
            Light lightComponent = lightObject.GetComponent<Light>();
            if (lightComponent != null)
            {
                lightComponent.intensity = lightOnIntensity; // Set to full intensity
                lightComponent.enabled = true; // Ensure the light is enabled
            }
        }

        Debug.Log("[EnableCityLights] All CityLights objects have been enabled with flicker effect.");
    }

    private void DisableCityLightsObjects()
    {
        GameObject[] cityLights = GameObject.FindGameObjectsWithTag(cityLightsTag);

        foreach (GameObject lightObject in cityLights)
        {
            Light lightComponent = lightObject.GetComponent<Light>();
            if (lightComponent != null)
            {
                lightComponent.intensity = lightOffIntensity; // Set to off intensity
                lightComponent.enabled = false; // Disable the light
            }
        }

        Debug.Log("[EnableCityLights] All CityLights objects have been disabled on Awake.");
    }
}
