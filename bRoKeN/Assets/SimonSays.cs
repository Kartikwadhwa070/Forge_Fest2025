using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;           // NEW: for VideoPlayer
using UnityEngine.SceneManagement; // NEW: for optional return to MainMenu

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

    // -------------------------------
    // NEW: Optional cutscene on success
    // -------------------------------
    [Header("Cutscene (Optional)")]
    [Tooltip("If true, play a cutscene after the Simon Says puzzle is solved.")]
    public bool playCutsceneOnSuccess = false;

    [Tooltip("VideoPlayer to play when the puzzle is solved.")]
    public VideoPlayer winVideo;

    [Tooltip("Canvas that contains the VideoPlayer and optional prompt UI.")]
    public GameObject endCanvas;

    [Tooltip("UI prompt shown after the video ends (e.g., 'Press P to return').")]
    public GameObject promptMessage;

    private bool promptActive = false;
    // -------------------------------

    private List<int> sequence = new List<int>();
    private List<int> playerInput = new List<int>();
    private bool isShowingSequence = false;
    private bool waitingForInput = false;
    private bool gameStarted = false;
    private Vector3[] originalButtonPositions = new Vector3[4];
    private Vector3 originalStartButtonPosition;
    private bool[] buttonPressed = new bool[4];
    private bool startButtonPressed = false;

    // Light fix: remember original colors so we can restore them after failure flashes
    private Color[] originalLightColors = new Color[4];

    private Camera playerCamera;

    [Header("Gating")]
    [Tooltip("If false, clicks are ignored and Start cannot be pressed.")]
    public bool interactionEnabled = false;

    public void SetInteractionEnabled(bool enabled) => interactionEnabled = enabled;

    void Start()
    {
        // match your Connect4 pattern: ensure cutscene UI starts hidden
        if (endCanvas) endCanvas.SetActive(false);
        if (promptMessage) promptMessage.SetActive(false);

        // Get the main camera
        playerCamera = Camera.main;
        if (playerCamera == null) playerCamera = FindObjectOfType<Camera>();

        // Store button/home positions
        for (int i = 0; i < buttons.Length; i++)
            if (buttons[i] != null) originalButtonPositions[i] = buttons[i].localPosition;

        if (startButton != null)
            originalStartButtonPosition = startButton.localPosition;

        // Cache original light colors & turn them off
        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i] != null)
            {
                originalLightColors[i] = lights[i].color;
                lights[i].enabled = false;
            }
        }
    }

    void OnDisable()
    {
        // safety: unhook video callback if we had one
        if (winVideo != null)
            winVideo.loopPointReached -= OnVideoFinished;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (!interactionEnabled) return;
            HandleMouseClick();
        }

        // Return animations
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

        if (startButton != null && !startButtonPressed)
        {
            startButton.localPosition = Vector3.Lerp(
                startButton.localPosition,
                originalStartButtonPosition,
                Time.deltaTime * buttonReturnSpeed
            );
        }

        // Optional: same UX as Connect4 — after video ends, let player press P
        if (promptActive && Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log("P pressed. Loading Main Menu...");
            SceneManager.LoadScene("MainMenu");
        }
    }

    void HandleMouseClick()
    {
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, buttonLayerMask))
        {
            // Start button?
            if (startButton != null && (hit.collider.transform == startButton || hit.collider.transform.parent == startButton))
            {
                if (!gameStarted && !IsGameActive())
                {
                    PressStartButton();
                }
                return;
            }

            // Game buttons?
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
        if (!interactionEnabled) return;

        StartCoroutine(AnimateStartButtonPress());

        if (audioSource != null && startButtonSound != null)
            audioSource.PlayOneShot(startButtonSound);

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
        if (!waitingForInput || buttonIndex < 0 || buttonIndex >= buttons.Length) return;

        StartCoroutine(AnimateButtonPress(buttonIndex));

        if (audioSource != null && buttonSounds[buttonIndex] != null)
            audioSource.PlayOneShot(buttonSounds[buttonIndex]);

        playerInput.Add(buttonIndex);

        if (playerInput[playerInput.Count - 1] != sequence[playerInput.Count - 1])
        {
            StartCoroutine(HandleFailure());
            return;
        }

        if (playerInput.Count >= sequence.Count)
            StartCoroutine(HandleSuccess());
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

        for (int i = 0; i < 4; i++)
            sequence.Add(Random.Range(0, 4));

        StartCoroutine(ShowSequence());
    }

    IEnumerator ShowSequence()
    {
        isShowingSequence = true;
        waitingForInput = false;

        yield return new WaitForSeconds(1f);

        for (int i = 0; i < sequence.Count; i++)
        {
            int lightIndex = sequence[i];

            if (lights[lightIndex] != null) lights[lightIndex].enabled = true;
            if (audioSource != null && buttonSounds[lightIndex] != null)
                audioSource.PlayOneShot(buttonSounds[lightIndex]);

            yield return new WaitForSeconds(lightDuration);

            if (lights[lightIndex] != null) lights[lightIndex].enabled = false;

            yield return new WaitForSeconds(sequenceDelay);
        }

        isShowingSequence = false;
        waitingForInput = true;
        playerInput.Clear();
    }

    IEnumerator HandleSuccess()
    {
        waitingForInput = false;

        if (audioSource != null && successSound != null)
            audioSource.PlayOneShot(successSound);

        // flash lights
        for (int flash = 0; flash < 3; flash++)
        {
            for (int i = 0; i < lights.Length; i++)
                if (lights[i] != null) lights[i].enabled = true;

            yield return new WaitForSeconds(0.2f);

            for (int i = 0; i < lights.Length; i++)
                if (lights[i] != null) lights[i].enabled = false;

            yield return new WaitForSeconds(0.2f);
        }

        OnSequenceCompleted();
    }

    IEnumerator HandleFailure()
    {
        waitingForInput = false;

        if (audioSource != null && failSound != null)
            audioSource.PlayOneShot(failSound);

        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i] != null)
            {
                lights[i].enabled = true;
                lights[i].color = Color.red;
            }
        }

        yield return new WaitForSeconds(0.5f);

        // restore original colors and turn off
        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i] != null)
            {
                lights[i].color = originalLightColors[i];
                lights[i].enabled = false;
            }
        }

        yield return new WaitForSeconds(0.5f);

        StartCoroutine(ShowSequence());
    }

    // Called when the player successfully completes the sequence
    public void OnSequenceCompleted()
    {
        Debug.Log("Simon Says sequence completed successfully!");
        gameStarted = false; // Reset so start button can be used again

        // NEW: optional cutscene trigger
        if (playCutsceneOnSuccess)
            StartWinCutscene();
    }

    // Public method to restart the game
    public void RestartGame()
    {
        StopAllCoroutines();
        gameStarted = false;

        for (int i = 0; i < lights.Length; i++)
            if (lights[i] != null) lights[i].enabled = false;

        Debug.Log("Simon Says game reset. Press start button to begin again.");
    }

    public bool IsGameActive() => waitingForInput || isShowingSequence;

    // -------------------------------
    // NEW: Cutscene helpers (mirror Connect4)
    // -------------------------------
    private void StartWinCutscene()
    {
        if (winVideo != null)
        {
            if (endCanvas != null) endCanvas.SetActive(true);
            if (promptMessage != null) promptMessage.SetActive(false);
            promptActive = false;

            winVideo.loopPointReached -= OnVideoFinished; // avoid double-subscribe
            winVideo.loopPointReached += OnVideoFinished;
            winVideo.Play();
        }
        else
        {
            Debug.LogWarning("Play Cutscene On Success is enabled, but no VideoPlayer is assigned.");
        }
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        vp.loopPointReached -= OnVideoFinished;

        if (endCanvas != null) endCanvas.SetActive(false);
        if (promptMessage != null) promptMessage.SetActive(true);
        promptActive = true;

        Debug.Log("SimonSays win video finished. Press P to go back to the Main Menu to play again.");
    }
}
