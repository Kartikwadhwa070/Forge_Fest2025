using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;           // Required for VideoPlayer
using UnityEngine.SceneManagement; // Required for scene management

public class Connect4Manager : MonoBehaviour
{
    // Board dimensions (typically 7 columns and 6 rows)
    public int numColumns = 7;
    public int numRows = 6;

    // Disc types (Empty = 0, Player = 1, Computer = 2)
    public enum DiscType { Empty = 0, Player = 1, Computer = 2 };

    // A serializable class representing one column of board slots.
    [System.Serializable]
    public class Column
    {
        public Transform[] slots;
    }

    // Array of columns ï¿½ assign these in the Inspector.
    public Column[] columns;

    // Disc prefabs for the player and computer. Assign your 3D disc models here.
    public GameObject playerDiscPrefab;
    public GameObject computerDiscPrefab;

    // Range for the computerï¿½s move delay (in seconds).
    public float minDelay = 1.0f;
    public float maxDelay = 2.0f;

    // ------------------------------------------------------------------
    // Wire control
    // ------------------------------------------------------------------
    [Header("Wire Control")]
    [Tooltip("Wire that must be glowing to allow gameplay (gate). Optional.")]
    public WireGlowController requiredWire;

    [Tooltip("DEPRECATED: use 'Next Puzzle Wires' below. Kept for back-compat.")]
    public List<WireGlowController> wiresToFlicker;

    [Header("Next Puzzle Wiring")]
    [Tooltip("Wire(s) that will flicker+glow to show the NEXT puzzle has been activated.")]
    public List<WireGlowController> nextPuzzleWires = new List<WireGlowController>();

    // ------------------------------------------------------------------
    // On-solve actions
    // ------------------------------------------------------------------
    [Header("On Solve Actions")]
    [Tooltip("If true, play the win cutscene when the player solves the puzzle.")]
    public bool playCutsceneOnSolve = false;

    [Tooltip("If true, visibly activate the next puzzle by flickering/glowing its wires.")]
    public bool activateNextPuzzleOnSolve = true;

    // ------------------------------------------------------------------
    // Debug / QA
    // ------------------------------------------------------------------
    [Header("Debug / QA")]
    [Tooltip("If true, treat this puzzle as already solved when the scene starts (skips gameplay).")]
    public bool markSolvedOnStart = false; // NEW

    // Audio clips for various events.
    [Header("Audio")]
    [Tooltip("Sound played when the player places a disc.")]
    public AudioClip playerPlaceSound;
    [Tooltip("Sound played when the computer places a disc.")]
    public AudioClip computerPlaceSound;
    [Tooltip("Sound played when the puzzle resets.")]
    public AudioClip resetSound;
    [Tooltip("Sound played when the player wins.")]
    public AudioClip playerWinSound;
    [Tooltip("Sound played when the computer wins.")]
    public AudioClip computerWinSound;

    // Reference to an AudioSource component.
    private AudioSource audioSource;

    // VideoPlayer to play a video when the puzzle is solved (player wins).
    [Header("Video Player (optional)")]
    public VideoPlayer winVideo; // Assign your VideoPlayer component here via the Inspector

    // Canvas that holds the VideoPlayer & the prompt.
    public GameObject endCanvas;
    public GameObject promptMessage;

    // Flag used to indicate that the win video has finished and the prompt is active.
    private bool promptActive = false;

    // Internal board state
    private int[,] board;

    // Keep track of instantiated disc GameObjects so we can clear them when resetting.
    private List<GameObject> activeDiscs = new List<GameObject>();

    // Flag to disable player input while the computer is moving or after a win.
    private bool inputEnabled = true;

    // NEW: prevent double triggers
    private bool puzzleSolved = false; // NEW

    // Awake is called before Start.
    void Awake()
    {
        // Ensure that the end canvas and prompt are disabled as soon as this object awakes.
        if (endCanvas != null) endCanvas.SetActive(false);
        if (promptMessage != null) promptMessage.SetActive(false);
    }

    void Start()
    {
        board = new int[numColumns, numRows];
        InitializeBoard();
        Debug.Log("Connect4Manager started.");
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            Debug.LogWarning("No AudioSource found on Connect4Manager object. Please add one.");
        }

        // Freeze gameplay until the required wire is turned on.
        if (requiredWire != null && !requiredWire.isGlowing)
        {
            inputEnabled = false;
            StartCoroutine(WaitForRequiredWire());
        }

        // NEW: QA toggle to auto-solve on start
        if (markSolvedOnStart)
        {
            Debug.Log("[Connect4] markSolvedOnStart is enabled — auto-solving.");
            HandlePuzzleSolved(playWinAudio: false); // skip loud win audio on boot
        }
    }

    void Update()
    {
        // After the video ends and the prompt is active, wait for the user to press P.
        if (promptActive && Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log("P pressed. Loading Main Menu...");
            SceneManager.LoadScene("MainMenu");
        }
    }

    void OnDisable()
    {
        // Safety: unhook video callback if we had one
        if (winVideo != null)
        {
            winVideo.loopPointReached -= OnVideoFinished;
        }
    }

    void InitializeBoard()
    {
        for (int col = 0; col < numColumns; col++)
        {
            for (int row = 0; row < numRows; row++)
            {
                board[col, row] = (int)DiscType.Empty;
            }
        }

        foreach (GameObject disc in activeDiscs)
        {
            Destroy(disc);
        }
        activeDiscs.Clear();

        // Only allow input if not solved yet
        inputEnabled = !puzzleSolved; // NEW

        if (audioSource != null && resetSound != null)
        {
            audioSource.PlayOneShot(resetSound);
        }
    }

    int InsertDisc(int column, DiscType discType)
    {
        if (column < 0 || column >= numColumns)
        {
            Debug.LogError("Invalid column index: " + column);
            return -1;
        }

        for (int row = 0; row < numRows; row++)
        {
            if (board[column, row] == (int)DiscType.Empty)
            {
                board[column, row] = (int)discType;
                Transform slotTransform = columns[column].slots[row];
                GameObject discPrefab = (discType == DiscType.Player) ? playerDiscPrefab : computerDiscPrefab;
                GameObject discInstance = Instantiate(discPrefab, slotTransform.position, slotTransform.rotation);
                activeDiscs.Add(discInstance);

                if (audioSource != null)
                {
                    if (discType == DiscType.Player && playerPlaceSound != null)
                        audioSource.PlayOneShot(playerPlaceSound);
                    else if (discType == DiscType.Computer && computerPlaceSound != null)
                        audioSource.PlayOneShot(computerPlaceSound);
                }
                return row;
            }
        }
        return -1; // Column is full
    }

    bool IsColumnFull(int column)
    {
        return board[column, numRows - 1] != (int)DiscType.Empty;
    }

    public void OnColumnButtonPressed(int column)
    {
        if (!inputEnabled || puzzleSolved) // NEW guard
            return;

        int row = InsertDisc(column, DiscType.Player);
        if (row != -1)
        {
            if (CheckWin(column, row, (int)DiscType.Player))
            {
                Debug.Log("Player won!");
                HandlePuzzleSolved(playWinAudio: true); // NEW centralized flow
                return;
            }

            // If player filled the selected column, do your existing reset
            if (IsColumnFull(column))
            {
                Debug.Log("Column " + column + " is full. Resetting board.");
                StartCoroutine(ResetBoardAfterDelay());
                return;
            }

            inputEnabled = false;
            StartCoroutine(ComputerMoveCoroutine());
        }
        else
        {
            Debug.Log("Column " + column + " is already full. Please choose a different column.");
        }
    }

    IEnumerator ComputerMoveCoroutine()
    {
        float delay = Random.Range(minDelay, maxDelay);
        yield return new WaitForSeconds(delay);

        List<int> validColumns = new List<int>();
        for (int col = 0; col < numColumns; col++)
        {
            if (!IsColumnFull(col))
                validColumns.Add(col);
        }

        if (validColumns.Count > 0)
        {
            int chosenColumn = validColumns[Random.Range(0, validColumns.Count)];
            int row = InsertDisc(chosenColumn, DiscType.Computer);
            if (row != -1)
            {
                if (CheckWin(chosenColumn, row, (int)DiscType.Computer))
                {
                    Debug.Log("Computer won!");
                    if (audioSource != null && computerWinSound != null)
                        audioSource.PlayOneShot(computerWinSound);
                    yield return new WaitForSeconds(0.5f);
                    StartCoroutine(ResetBoardAfterDelay());
                    yield break;
                }

                if (IsColumnFull(chosenColumn))
                {
                    yield return new WaitForSeconds(0.5f);
                    StartCoroutine(ResetBoardAfterDelay());
                    yield break;
                }
            }
        }
        if (!puzzleSolved) // NEW: don't re-enable input if already solved via some other path
            inputEnabled = true;
    }

    IEnumerator ResetBoardAfterDelay()
    {
        yield return new WaitForSeconds(1f);
        InitializeBoard();
    }

    bool CheckWin(int column, int row, int discType)
    {
        int[][] directions = new int[][] {
            new int[] { 1, 0 },
            new int[] { 0, 1 },
            new int[] { 1, 1 },
            new int[] { 1, -1 }
        };

        foreach (var dir in directions)
        {
            int dx = dir[0];
            int dy = dir[1];
            int count = 1;

            int c = column + dx;
            int r = row + dy;
            while (c >= 0 && c < numColumns && r >= 0 && r < numRows && board[c, r] == discType)
            {
                count++;
                c += dx;
                r += dy;
            }

            c = column - dx;
            r = row - dy;
            while (c >= 0 && c < numColumns && r >= 0 && r < numRows && board[c, r] == discType)
            {
                count++;
                c -= dx;
                r -= dy;
            }

            if (count >= 4)
                return true;
        }
        return false;
    }

    // -----------------------------
    // NEW: central on-solve flow
    // -----------------------------
    private void HandlePuzzleSolved(bool playWinAudio)
    {
        if (puzzleSolved) return;
        puzzleSolved = true;

        if (playWinAudio && audioSource != null && playerWinSound != null)
            audioSource.PlayOneShot(playerWinSound);

        if (activateNextPuzzleOnSolve)
            ActivateNextPuzzle();

        if (playCutsceneOnSolve)
            StartWinCutscene();

        inputEnabled = false; // freeze gameplay when solved
    }

    // -----------------------------
    // visibly activate next puzzle
    // -----------------------------
    private void ActivateNextPuzzle()
    {
        // Prefer the new list; fall back to the legacy one if empty.
        List<WireGlowController> list = (nextPuzzleWires != null && nextPuzzleWires.Count > 0)
            ? nextPuzzleWires
            : wiresToFlicker;

        if (list != null && list.Count > 0)
        {
            foreach (var wire in list)
            {
                if (wire != null)
                    StartCoroutine(wire.FlickerThenGlow()); // from WireGlowController
            }
        }
        else
        {
            Debug.LogWarning("No wires assigned for next puzzle activation!");
        }
    }

    // -----------------------------
    // optional win cutscene
    // -----------------------------
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
            Debug.LogWarning("Play Cutscene On Solve is enabled, but no VideoPlayer is assigned.");
        }
    }

    private IEnumerator WaitForRequiredWire()
    {
        Debug.Log("Waiting for the required wire to be turned on...");
        while (requiredWire != null && !requiredWire.isGlowing)
        {
            yield return null; // Wait until the required wire is glowing.
        }
        Debug.Log("Required wire is glowing! Gameplay unlocked.");

        // NEW: Do not re-enable input if the puzzle is already marked solved
        if (!puzzleSolved)
            inputEnabled = true;
    }

    // This method is called when the win video finishes playing.
    private void OnVideoFinished(VideoPlayer vp)
    {
        vp.loopPointReached -= OnVideoFinished;

        if (endCanvas != null) endCanvas.SetActive(false);

        if (promptMessage != null) promptMessage.SetActive(true);
        promptActive = true;

        Debug.Log("Win video finished. Press P to go back to the Main Menu to play again.");
    }

#if UNITY_EDITOR
    // Handy dev utility from the component's context menu
    [ContextMenu("DEV: Mark Solved Now")]
    private void Dev_MarkSolvedNow()
    {
        HandlePuzzleSolved(playWinAudio: false);
    }
#endif
}
