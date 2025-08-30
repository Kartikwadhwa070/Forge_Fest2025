using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class StartButton : MonoBehaviour
{
    public GameObject intermediateCanvas; // Assign via Inspector
    private CanvasGroup canvasGroup;
    public float fadeDuration = 1f; // Duration of the fade-in effect

    void Start()
    {
        // Get the Button component and add an event listener
        GetComponent<Button>().onClick.AddListener(OnStartButtonPressed);

        // Get the CanvasGroup component from the intermediate canvas
        canvasGroup = intermediateCanvas.GetComponent<CanvasGroup>();

        // Ensure the intermediate canvas is initially inactive
        intermediateCanvas.SetActive(false);
    }

    void OnStartButtonPressed()
    {
        // Start the coroutine to handle the intermediate canvas and scene loading
        StartCoroutine(ShowIntermediateCanvasAndLoadScene());
    }

    IEnumerator ShowIntermediateCanvasAndLoadScene()
    {
        // Activate the intermediate canvas
        intermediateCanvas.SetActive(true);

        // Start with the canvas fully transparent
        canvasGroup.alpha = 0f;

        // Fade in the canvas over the specified duration
        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        canvasGroup.alpha = 1f; // Ensure the canvas is fully opaque

        // Wait for an additional 10 seconds
        yield return new WaitForSeconds(7f);

        // Deactivate the intermediate canvas
        //intermediateCanvas.SetActive(false);

        // Load the next scene
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        int nextSceneIndex = currentSceneIndex + 1;

        // Check if the next scene index is within the build settings
        if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextSceneIndex);
        }
        else
        {
            Debug.LogWarning("No next scene in Build Settings!");
        }
    }
}
