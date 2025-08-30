using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement; // <— needed for scene load

public class SequentialUI : MonoBehaviour
{
    [Header("Assign your UI elements in order (Image, TMP Text, Image, TMP Text...)")]
    public GameObject[] uiElements;

    [Header("Timing Settings")]
    public float delayBetween = 1f;        // time between elements
    public float typewriterSpeed = 0.05f;  // speed for text typing

    [Header("Scene Handoff")]
    [Tooltip("If true, automatically load the next scene in Build Settings once the sequence finishes.")]
    public bool loadNextSceneOnFinish = true;
    [Tooltip("Small pause after the last element finishes before loading the next scene.")]
    public float afterFinishDelay = 0.25f;
    [Tooltip("Optional: if there is no 'next' scene in build, this name will be loaded instead (leave blank to do nothing).")]
    public string fallbackSceneName = "";

    void Start()
    {
        // Hide everything at the start
        foreach (GameObject element in uiElements)
            if (element) element.SetActive(false);

        StartCoroutine(ShowElementsSequentially());
    }

    IEnumerator ShowElementsSequentially()
    {
        foreach (GameObject element in uiElements)
        {
            if (!element) continue;

            element.SetActive(true);

            // If this element has a TMP text component → typewriter effect
            TextMeshProUGUI tmpText = element.GetComponent<TextMeshProUGUI>();
            if (tmpText != null)
            {
                yield return StartCoroutine(TypeText(tmpText));
            }
            else
            {
                // Wait a bit before showing the next element (for images etc.)
                yield return new WaitForSeconds(delayBetween);
            }
        }

        // Whole sequence finished → handoff to the next scene
        if (loadNextSceneOnFinish)
        {
            yield return new WaitForSeconds(afterFinishDelay);
            LoadNextScene();
        }
    }

    IEnumerator TypeText(TextMeshProUGUI tmpText)
    {
        string fullText = tmpText.text;
        tmpText.text = ""; // clear text before typing

        foreach (char c in fullText)
        {
            tmpText.text += c;
            yield return new WaitForSeconds(typewriterSpeed);
        }

        // wait after finishing text before next element
        yield return new WaitForSeconds(delayBetween);
    }

    void LoadNextScene()
    {
        int current = SceneManager.GetActiveScene().buildIndex;
        int next = current + 1;

        // If there IS a next scene in Build Settings, load it; otherwise try the fallback.
        if (next < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(next);
        }
        else if (!string.IsNullOrEmpty(fallbackSceneName))
        {
            SceneManager.LoadScene(fallbackSceneName);
        }
        else
        {
            Debug.LogWarning("[SequentialUI] No next scene in Build Settings, and no fallbackSceneName set.");
        }
    }
}
