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

    // Array of columns – assign these in the Inspector.
    public Column[] columns;

    // Disc prefabs for the player and computer. Assign your 3D disc models here.
    public GameObject playerDiscPrefab;
    public GameObject computerDiscPrefab;

    // Range for the computer’s move delay (in seconds).
    public float minDelay = 1.0f;
    public float maxDelay = 2.0f;

    // Wire control for freezing gameplay
    [Header("Wire Control")]
    public WireGlowController requiredWire; // The wire that must be turned on to start the game
    public List<WireGlowController> wiresToFlicker; // List of wires to flicker on player win

    // Audio clips for various events.
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
    [Header("Video Player")]
    public VideoPlayer winVideo; // Assign your VideoPlayer component here via the Inspector

    // Reference to the canvas that holds the VideoPlayer.
    // This canvas should be disabled on Awake.
    public GameObject endCanvas;

    // UI prompt message to tell the user to press P to return to the Main Menu.
    // This should be disabled by default.
    public GameObject promptMessage;

    // Flag used to indicate that the win video has finished and the prompt is active.
    private bool promptActive = false;

    // Internal board state where board[col, row] stores:
    // 0 = empty, 1 = player disc, 2 = computer disc.
    private int[,] board;

    // Keep track of instantiated disc GameObjects so we can clear them when resetting.
    private List<GameObject> activeDiscs = new List<GameObject>();

    // Flag to disable player input while the computer is moving or after a win.
    private bool inputEnabled = true;

    // Awake is called before Start.
    void Awake()
    {
        // Ensure that the end canvas and prompt are disabled as soon as this object awakes.
        if (endCanvas != null)
        {
            endCanvas.SetActive(false);
        }
        if (promptMessage != null)
        {
            promptMessage.SetActive(false);
        }
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
        inputEnabled = true;

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
        if (!inputEnabled)
            return;

        int row = InsertDisc(column, DiscType.Player);
        if (row != -1)
        {
            if (CheckWin(column, row, (int)DiscType.Player))
            {
                Debug.Log("Player won!");
                if (audioSource != null && playerWinSound != null)
                    audioSource.PlayOneShot(playerWinSound);

                // Trigger wire flicker effect when the player wins.
                TriggerWireFlickerEffect();

                // Enable the canvas and play the win video.
                if (winVideo != null)
                {
                    if (endCanvas != null)
                        endCanvas.SetActive(true);

                    winVideo.Play();
                    // Subscribe to the event to know when the video finishes.
                    winVideo.loopPointReached += OnVideoFinished;
                }

                inputEnabled = false;  // Freeze gameplay after a win.
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

    // This method is called when the win video finishes playing.
    private void OnVideoFinished(VideoPlayer vp)
    {
        // Unsubscribe from the event so it doesn't fire again.
        vp.loopPointReached -= OnVideoFinished;
        // Disable the canvas after the video is complete.
        if (endCanvas != null)
        {
            endCanvas.SetActive(false);
        }
        // Activate the prompt message and set the flag.
        if (promptMessage != null)
        {
            promptMessage.SetActive(true);
        }
        promptActive = true;
        Debug.Log("Win video finished. Press P to go back to the Main Menu to play again.");
    }
}
