using UnityEngine;
using TMPro;
using System.Collections;

public class SequentialUI : MonoBehaviour
{
    [Header("Assign your UI elements in order (Image, TMP Text, Image, TMP Text...)")]
    public GameObject[] uiElements;

    [Header("Timing Settings")]
    public float delayBetween = 1f;        // time between elements
    public float typewriterSpeed = 0.05f;  // speed for text typing

    void Start()
    {
        // Hide everything at the start
        foreach (GameObject element in uiElements)
        {
            element.SetActive(false);
        }

        StartCoroutine(ShowElementsSequentially());
    }

    IEnumerator ShowElementsSequentially()
    {
        foreach (GameObject element in uiElements)
        {
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
}
