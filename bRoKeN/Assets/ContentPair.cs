using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogueSequenceList : MonoBehaviour
{
    [System.Serializable]
    public class DialogueElement
    {
        public Sprite image;          // The image to show
        [TextArea] public string text; // The text to type out
    }

    [Header("Setup")]
    public GameObject imagePrefab;   // Prefab with an Image
    public GameObject textPrefab;    // Prefab with a TMP Text
    public Transform parent;         // Parent container (e.g., Vertical Layout Group)
    public float typewriterSpeed = 0.05f;

    [Header("Dialogue Elements")]
    public List<DialogueElement> sequence = new List<DialogueElement>();

    private void Start()
    {
        StartCoroutine(PlaySequence());
    }

    IEnumerator PlaySequence()
    {
        foreach (var element in sequence)
        {
            // --- Create Image ---
            GameObject newImageObj = Instantiate(imagePrefab, parent);
            Image img = newImageObj.GetComponent<Image>();
            img.sprite = element.image;
            newImageObj.SetActive(true);

            // --- Create Text ---
            GameObject newTextObj = Instantiate(textPrefab, parent);
            TMP_Text tmp = newTextObj.GetComponent<TMP_Text>();
            tmp.text = ""; // start empty

            // --- Typewriter effect ---
            foreach (char c in element.text)
            {
                tmp.text += c;
                yield return new WaitForSeconds(typewriterSpeed);
            }

            // Wait for space before continuing (optional)
            yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
        }
    }
}
