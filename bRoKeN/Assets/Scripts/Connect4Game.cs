using UnityEngine;

public class Connect4Game : MonoBehaviour
{
    public GameObject playerDiscPrefab;   // Assign player disc prefab
    public GameObject computerDiscPrefab; // Assign computer disc prefab
    public Transform[] columnButtons;     // Assign buttons for each column
    public Transform[,] slots;            // Assign slots on the board (2D array of slot positions)
    public float discDropSpeed = 2f;      // Speed of disc dropping

    private int[,] board;                 // Logical representation of the board
    private int rows = 5;                 // Number of rows
    private int columns = 6;              // Number of columns
    private bool isPlayerTurn = true;     // Tracks player turn
    private bool isGameRunning = true;    // Tracks game state

    void Start()
    {
        InitializeBoard();
    }

    void InitializeBoard()
    {
        board = new int[rows, columns];

        // Clear the board and initialize visual slots
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                board[row, col] = 0;
            }
        }
    }

    public void OnColumnButtonPressed(int column)
    {
        if (!isGameRunning || !isPlayerTurn) return;

        if (PlaceDisc(column, 1)) // Player places a disc
        {
            isPlayerTurn = false;
            CheckGameState();

            // Wait 1-2 seconds and let the computer play
            Invoke(nameof(ComputerPlay), Random.Range(1f, 2f));
        }
    }

    void ComputerPlay()
    {
        if (!isGameRunning) return;

        int column = Random.Range(0, columns);
        while (!CanPlaceDisc(column))
        {
            column = Random.Range(0, columns);
        }

        if (PlaceDisc(column, 2)) // Computer places a disc
        {
            isPlayerTurn = true;
            CheckGameState();
        }
    }

    bool PlaceDisc(int column, int player)
    {
        for (int row = rows - 1; row >= 0; row--)
        {
            if (board[row, column] == 0)
            {
                board[row, column] = player;

                // Instantiate the disc at the correct slot
                GameObject disc = Instantiate(player == 1 ? playerDiscPrefab : computerDiscPrefab);
                disc.transform.position = new Vector3(columnButtons[column].position.x, 10f, columnButtons[column].position.z); // Drop from above
                StartCoroutine(DropDisc(disc, slots[row, column].position));

                return true;
            }
        }

        return false; // Column is full
    }

    bool CanPlaceDisc(int column)
    {
        return board[0, column] == 0; // Check if the top row is empty
    }

    System.Collections.IEnumerator DropDisc(GameObject disc, Vector3 targetPosition)
    {
        while (Vector3.Distance(disc.transform.position, targetPosition) > 0.1f)
        {
            disc.transform.position = Vector3.MoveTowards(disc.transform.position, targetPosition, discDropSpeed * Time.deltaTime);
            yield return null;
        }

        disc.transform.position = targetPosition;
    }

    void CheckGameState()
    {
        // Check if any column is full
        for (int col = 0; col < columns; col++)
        {
            if (board[0, col] != 0) // Topmost row is full
            {
                ResetBoard();
                return;
            }
        }

        // Add winning condition logic if needed
    }

    void ResetBoard()
    {
        // Destroy all discs
        foreach (Transform slot in slots)
        {
            if (slot.childCount > 0)
            {
                Destroy(slot.GetChild(0).gameObject);
            }
        }

        InitializeBoard();
        isPlayerTurn = true;
    }
}
