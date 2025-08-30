using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class MessageTrigger : MonoBehaviour
{
    public GameObject messageBox; // Assign the UI message box
    private TextMeshProUGUI messageText; // Text component

    public string message = "You stepped on the trigger!"; // Customizable message
    public float fadeDuration = 0.5f; // Fade-in time
    public TMP_FontAsset customFont;
    public float displayTime = 3f; // Time before fading out
    private bool hasTriggered = false; // Ensure it only works once

    private CanvasGroup canvasGroup;

    private void Start()
    {
        // Get UI elements
        messageText = messageBox.GetComponentInChildren<TextMeshProUGUI>();
        canvasGroup = messageBox.GetComponent<CanvasGroup>();

        // Ensure message box is invisible at start
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0;
            messageBox.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!hasTriggered && other.CompareTag("Player"))
        {
            hasTriggered = true; // Disable further triggers
            messageBox.SetActive(true);
            messageText.text = message;
            StartCoroutine(ShowMessage());
        }
    }

    private IEnumerator ShowMessage()
    {
        // Fade in
        yield return StartCoroutine(FadeCanvas(0, 1, fadeDuration));

        // Wait for display time
        yield return new WaitForSeconds(displayTime);

        // Fade out
        yield return StartCoroutine(FadeCanvas(1, 0, fadeDuration));

        // Disable message box after fading out
        messageBox.SetActive(false);
    }

    private IEnumerator FadeCanvas(float startAlpha, float endAlpha, float duration)
    {
        float elapsedTime = 0;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / duration);
            yield return null;
        }
        canvasGroup.alpha = endAlpha;
    }
}
