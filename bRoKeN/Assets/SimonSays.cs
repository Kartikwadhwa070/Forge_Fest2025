using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimonSays : MonoBehaviour
{
    [Header("Lights")]
    public Light[] lights = new Light[4];

    [Header("Buttons")]
    public Transform[] buttons = new Transform[4];

    [Header("Start Button")]
    public Transform startButton;
    public float startButtonPressDepth = 0.05f;

    [Header("Settings")]
    public float lightDuration = 1f;
    public float sequenceDelay = 0.5f;
    public float buttonPressDepth = 0.1f;
    public float buttonReturnSpeed = 5f;
    public LayerMask buttonLayerMask = -1;

    [Header("Audio (Optional)")]
    public AudioSource audioSource;
    public AudioClip[] buttonSounds = new AudioClip[4];
    public AudioClip successSound;
    public AudioClip failSound;
    public AudioClip startButtonSound;

    private List<int> sequence = new List<int>();
    private List<int> playerInput = new List<int>();
    private bool isShowingSequence = false;
    private bool waitingForInput = false;
    private bool gameStarted = false;
    private Vector3[] originalButtonPositions = new Vector3[4];
    private Vector3 originalStartButtonPosition;
    private bool[] buttonPressed = new bool[4];
    private bool startButtonPressed = false;

    private Camera playerCamera;

    void Start()
    {
        // Get the main camera (assuming it's the player's camera)
        playerCamera = Camera.main;
        if (playerCamera == null)
            playerCamera = FindObjectOfType<Camera>();

        // Store original button positions
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null)
                originalButtonPositions[i] = buttons[i].localPosition;
        }

        // Store original start button position
        if (startButton != null)
            originalStartButtonPosition = startButton.localPosition;

        // Turn off all lights initially
        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i] != null)
                lights[i].enabled = false;
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            HandleMouseClick();
        }

        // Handle button return animations for game buttons
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null && !buttonPressed[i])
            {
                buttons[i].localPosition = Vector3.Lerp(
                    buttons[i].localPosition,
                    originalButtonPositions[i],
                    Time.deltaTime * buttonReturnSpeed
                );
            }
        }

        // Handle start button return animation
        if (startButton != null && !startButtonPressed)
        {
            startButton.localPosition = Vector3.Lerp(
                startButton.localPosition,
                originalStartButtonPosition,
                Time.deltaTime * buttonReturnSpeed
            );
        }
    }

    void HandleMouseClick()
    {
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, buttonLayerMask))
        {
            // Check if start button was hit
            if (startButton != null && (hit.collider.transform == startButton || hit.collider.transform.parent == startButton))
            {
                if (!gameStarted && !IsGameActive())
                {
                    PressStartButton();
                }
                return;
            }

            // Check which game button was hit (only if game is active and waiting for input)
            if (waitingForInput)
            {
                for (int i = 0; i < buttons.Length; i++)
                {
                    if (buttons[i] != null && (hit.collider.transform == buttons[i] || hit.collider.transform.parent == buttons[i]))
                    {
                        PressButton(i);
                        break;
                    }
                }
            }
        }
    }

    void PressStartButton()
    {
        Debug.Log("Start button pressed! Beginning Simon Says...");

        // Animate start button press
        StartCoroutine(AnimateStartButtonPress());

        // Play start button sound if available
        if (audioSource != null && startButtonSound != null)
        {
            audioSource.PlayOneShot(startButtonSound);
        }

        // Start the game
        StartNewGame();
    }

    IEnumerator AnimateStartButtonPress()
    {
        if (startButton == null) yield break;

        startButtonPressed = true;

        Vector3 pressedPosition = originalStartButtonPosition + startButton.forward * -startButtonPressDepth;
        startButton.localPosition = pressedPosition;

        yield return new WaitForSeconds(0.15f);

        startButtonPressed = false;
    }

    void PressButton(int buttonIndex)
    {
        if (!waitingForInput || buttonIndex < 0 || buttonIndex >= buttons.Length)
            return;

        // Animate button press
        StartCoroutine(AnimateButtonPress(buttonIndex));

        // Play sound if available
        if (audioSource != null && buttonSounds[buttonIndex] != null)
        {
            audioSource.PlayOneShot(buttonSounds[buttonIndex]);
        }

        // Add to player input
        playerInput.Add(buttonIndex);

        // Check if this input is correct so far
        if (playerInput[playerInput.Count - 1] != sequence[playerInput.Count - 1])
        {
            // Wrong input - restart
            StartCoroutine(HandleFailure());
            return;
        }

        // Check if sequence is complete
        if (playerInput.Count >= sequence.Count)
        {
            // Success!
            StartCoroutine(HandleSuccess());
        }
    }

    IEnumerator AnimateButtonPress(int buttonIndex)
    {
        if (buttons[buttonIndex] == null) yield break;

        buttonPressed[buttonIndex] = true;

        Vector3 pressedPosition = originalButtonPositions[buttonIndex] + buttons[buttonIndex].forward * -buttonPressDepth;
        buttons[buttonIndex].localPosition = pressedPosition;

        yield return new WaitForSeconds(0.1f);

        buttonPressed[buttonIndex] = false;
    }

    public void StartNewGame()
    {
        // Don't start if already running
        if (IsGameActive())
        {
            Debug.Log("Simon Says is already running!");
            return;
        }

        sequence.Clear();
        playerInput.Clear();
        waitingForInput = false;
        gameStarted = true;

        Debug.Log("Starting Simon Says sequence...");

        // Generate a random sequence of 4
        for (int i = 0; i < 4; i++)
        {
            sequence.Add(Random.Range(0, 4));
        }

        StartCoroutine(ShowSequence());
    }

    IEnumerator ShowSequence()
    {
        isShowingSequence = true;
        waitingForInput = false;

        yield return new WaitForSeconds(1f); // Initial delay

        for (int i = 0; i < sequence.Count; i++)
        {
            int lightIndex = sequence[i];

            // Turn on light
            if (lights[lightIndex] != null)
                lights[lightIndex].enabled = true;

            // Play sound if available
            if (audioSource != null && buttonSounds[lightIndex] != null)
            {
                audioSource.PlayOneShot(buttonSounds[lightIndex]);
            }

            yield return new WaitForSeconds(lightDuration);

            // Turn off light
            if (lights[lightIndex] != null)
                lights[lightIndex].enabled = false;

            yield return new WaitForSeconds(sequenceDelay);
        }

        isShowingSequence = false;
        waitingForInput = true;
        playerInput.Clear();
    }

    IEnumerator HandleSuccess()
    {
        waitingForInput = false;

        // Play success sound
        if (audioSource != null && successSound != null)
        {
            audioSource.PlayOneShot(successSound);
        }

        // Flash all lights
        for (int flash = 0; flash < 3; flash++)
        {
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i] != null)
                    lights[i].enabled = true;
            }

            yield return new WaitForSeconds(0.2f);

            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i] != null)
                    lights[i].enabled = false;
            }

            yield return new WaitForSeconds(0.2f);
        }

        // Call success function
        OnSequenceCompleted();
    }

    IEnumerator HandleFailure()
    {
        waitingForInput = false;

        // Play fail sound
        if (audioSource != null && failSound != null)
        {
            audioSource.PlayOneShot(failSound);
        }

        // Flash all lights red (if you want to change color temporarily)
        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i] != null)
            {
                lights[i].enabled = true;
                lights[i].color = Color.red;
            }
        }

        yield return new WaitForSeconds(0.5f);

        // Reset light colors and turn off
        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i] != null)
            {
                lights[i].color = Color.white;
                lights[i].enabled = false;
            }
        }

        yield return new WaitForSeconds(0.5f);

        // Show sequence again
        StartCoroutine(ShowSequence());
    }

    // This function is called when the player successfully completes the sequence
    public void OnSequenceCompleted()
    {
        Debug.Log("Simon Says sequence completed successfully!");
        gameStarted = false; // Reset so start button can be used again

        // Add your custom success logic here
        // For example:
        // - Open a door
        // - Give the player an item
        // - Trigger the next puzzle
        // - Call another script's function
    }

    // Public method to restart the game
    public void RestartGame()
    {
        StopAllCoroutines();
        gameStarted = false;

        // Turn off all lights
        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i] != null)
                lights[i].enabled = false;
        }

        Debug.Log("Simon Says game reset. Press start button to begin again.");
    }

    // Public method to check if the game is currently active
    public bool IsGameActive()
    {
        return waitingForInput || isShowingSequence;
    }
}