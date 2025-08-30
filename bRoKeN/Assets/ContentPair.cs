using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SequentialUI : MonoBehaviour
{
    [Header("Assign your UI elements in order (Image/Text/Image/Text...)")]
    public GameObject[] uiElements;

    [Header("Delay Settings")]
    public float delayBetween = 1f; // time between elements appearing
    public bool useTypewriter = true;
    public float typewriterSpeed = 0.05f; // for text only

    void Start()
    {
        // Hide everything at the start
        foreach (GameObject element in uiElements)
        {
            element.SetActive(false);
        }

        // Start the sequence
        StartCoroutine(ShowElementsSequentially());
    }

    IEnumerator ShowElementsSequentially()
    {
        foreach (GameObject element in uiElements)
        {
            element.SetActive(true);

            // If it's a text and typewriter is enabled
            Text text = element.GetComponent<Text>();
            if (useTypewriter && text != null)
            {
                yield return StartCoroutine(TypeText(text));
            }

            // Wait before showing next element
            yield return new WaitForSeconds(delayBetween);
        }
    }

    IEnumerator TypeText(Text textComponent)
    {
        string fullText = textComponent.text;
        textComponent.text = ""; // clear first

        foreach (char c in fullText)
        {
            textComponent.text += c;
            yield return new WaitForSeconds(typewriterSpeed);
        }
    }
}
