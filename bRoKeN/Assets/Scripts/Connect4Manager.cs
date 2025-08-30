using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    // Array of columns – assign these in the Inspector.
    public Column[] columns;

    // Disc prefabs for the player and computer. Assign your 3D disc models here.
    public GameObject playerDiscPrefab;
    public GameObject computerDiscPrefab;

    // Range for the computer's move delay (in seconds).
    public float minDelay = 1.0f;
    public float maxDelay = 2.0f;

    // Wire control for freezing gameplay
    [Header("Wire Control")]
    public WireGlowController requiredWire; // The wire that must be turned on to start the game
    public List<WireGlowController> wiresToFlicker; // List of wires to flicker on player win

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

    // Mirror system integration
    [Header("Mirror System Integration")]
    public MirrorChainController mirrorChainController; // Reference to the mirror system
    public bool triggerMirrorsOnWin = true; // Whether to trigger mirrors when player wins

    // Internal board state where board[col, row] stores:
    // 0 = empty, 1 = player disc, 2 = computer disc.
    private int[,] board;

    // Keep track of instantiated disc GameObjects so we can clear them when resetting.
    private List<GameObject> activeDiscs = new List<GameObject>();

    // Flag to disable player input while the computer is moving or after a win.
    private bool inputEnabled = true;

    // Inspector testing controls
    [Header("Inspector Testing Controls")]
    [Tooltip("Complete Connect4 puzzle instantly (for testing mirror puzzle)")]
    public bool completeConnect4Puzzle = false;
    [Tooltip("Reset the Connect4 board")]
    public bool resetBoard = false;

    void Start()
    {
        board = new int[numColumns, numRows];

        // Initialize audio source first
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            Debug.LogWarning("No AudioSource found on Connect4Manager object. Please add one.");
        }

        // Setup mirror system callbacks BEFORE initializing board
        SetupMirrorCallbacks();

        // Initialize the board (this will reset mirror system too)
        InitializeBoard();

        Debug.Log("Connect4Manager started.");

        // Freeze gameplay until the required wire is turned on.
        if (requiredWire != null && !requiredWire.isGlowing)
        {
            inputEnabled = false;
            StartCoroutine(WaitForRequiredWire());
        }
    }

    void Update()
    {
        // Inspector testing controls
        HandleInspectorControls();
    }

    // Handle inspector testing controls
    void HandleInspectorControls()
    {
        // Complete Connect4 puzzle instantly
        if (completeConnect4Puzzle)
        {
            completeConnect4Puzzle = false; // Reset the flag
            Debug.Log("Inspector: Completing Connect4 puzzle instantly...");
            HandleConnect4Completion();
        }

        // Reset board
        if (resetBoard)
        {
            resetBoard = false; // Reset the flag
            Debug.Log("Inspector: Resetting Connect4 board...");
            InitializeBoard();
        }
    }

    // Setup callbacks for mirror system events
    void SetupMirrorCallbacks()
    {
        if (mirrorChainController != null)
        {
            mirrorChainController.OnSequenceStart.AddListener(OnMirrorSequenceStart);
            mirrorChainController.OnSequenceComplete.AddListener(OnMirrorSequenceComplete);

            // Ensure mirror system is reset to initial state
            mirrorChainController.ResetSystem();
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

        // Reset game state flags
        inputEnabled = true;

        // Reset mirror system if it exists (but don't do this during Start() to avoid conflicts)
        if (mirrorChainController != null && Time.time > 0.1f) // Small delay to avoid Start() conflicts
        {
            mirrorChainController.ResetSystem();
        }

        if (audioSource != null && resetSound != null)
        {
            audioSource.PlayOneShot(resetSound);
        }

        Debug.Log("Connect4 board initialized successfully.");
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
        if (!inputEnabled || (mirrorChainController != null && mirrorChainController.IsSequenceActive()))
            return;

        int row = InsertDisc(column, DiscType.Player);
        if (row != -1)
        {
            if (CheckWin(column, row, (int)DiscType.Player))
            {
                Debug.Log("Player won!");
                HandleConnect4Completion();
                return;
            }

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

    // Handle Connect4 completion (either by winning or inspector button)
    void HandleConnect4Completion()
    {
        if (audioSource != null && playerWinSound != null)
            audioSource.PlayOneShot(playerWinSound);

        // Trigger wire flicker effect
        TriggerWireFlickerEffect();

        // Trigger mirror sequence if enabled
        if (triggerMirrorsOnWin && mirrorChainController != null)
        {
            Debug.Log("Connect4 completed! Starting mirror puzzle...");
            mirrorChainController.TriggerSequence();
            inputEnabled = false; // Disable Connect4 input during mirror sequence
        }
        else
        {
            // No mirrors, just reset after delay
            StartCoroutine(ResetBoardAfterDelay());
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

    private void TriggerWireFlickerEffect()
    {
        if (wiresToFlicker != null && wiresToFlicker.Count > 0)
        {
            foreach (var wire in wiresToFlicker)
            {
                if (wire != null)
                    StartCoroutine(wire.FlickerThenGlow());
            }
        }
        else
        {
            Debug.LogWarning("No wires assigned for flicker effect!");
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
        inputEnabled = true; // Enable gameplay.
    }

    // Called when mirror sequence starts
    void OnMirrorSequenceStart()
    {
        Debug.Log("Connect4: Mirror sequence started, Connect4 input disabled.");
        inputEnabled = false;
    }

    // Called when mirror sequence completes
    void OnMirrorSequenceComplete()
    {
        Debug.Log("Connect4: Mirror sequence completed. Mirror puzzle is now active!");
        // Keep Connect4 input disabled - the mirror puzzle is now the active game
    }

    // Public method to manually trigger the mirror sequence (for other scripts)
    public void TriggerMirrorSequence()
    {
        if (mirrorChainController != null)
        {
            mirrorChainController.TriggerSequence();
        }
    }

    // Public method to reset everything
    public void ResetEverything()
    {
        if (mirrorChainController != null)
        {
            mirrorChainController.ResetSystem();
        }
        InitializeBoard();
    }
}