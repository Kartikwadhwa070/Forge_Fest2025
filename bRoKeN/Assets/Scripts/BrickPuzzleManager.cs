using UnityEngine;
using System.Collections.Generic;

public class BrickPuzzleManager : MonoBehaviour
{
    [Header("Bricks")]
    [SerializeField] private Brick[] bricks; // Assign in Inspector

    [Header("Solution")]
    // This array's length should match the number of bricks (9).
    // true means that brick is *required* to be pressed for the puzzle to be solved
    [SerializeField] private bool[] solution;

    [Header("Wire Control")]
    [SerializeField] private List<WireGlowController> wiresToTurnOn; // List of wires to toggle on when solved

    // A flag to prevent re-triggering the solve event multiple times if you want
    private bool puzzleSolved = false;

    private void Awake()
    {
        // Make sure each Brick references this puzzle manager
        for (int i = 0; i < bricks.Length; i++)
        {
            bricks[i].puzzleManager = this;
        }
    }

    public void CheckPuzzleState()
    {
        // Skip checking if it's already solved (if you don’t want multiple triggers)
        if (puzzleSolved) return;

        // Compare the current brick pressed states to the solution
        bool allMatch = true;
        for (int i = 0; i < bricks.Length; i++)
        {
            bool currentPressed = bricks[i].IsPressed();
            bool requiredPressed = solution[i];

            if (currentPressed != requiredPressed)
            {
                allMatch = false;
                break;
            }
        }

        // If they match, puzzle is solved!
        if (allMatch)
        {
            puzzleSolved = true;
            OnPuzzleSolved();
        }
    }

    private void OnPuzzleSolved()
    {
        Debug.Log("Puzzle solved!");

        // Turn on the wires with flickering effect
        TurnOnWiresWithFlickering();

        // Additional actions (if any)
        // - Enable/disable objects
        // - Trigger other scripts
    }

    private void TurnOnWiresWithFlickering()
    {
        foreach (WireGlowController wire in wiresToTurnOn)
        {
            if (wire != null)
            {
                // Start the flickering coroutine for each wire
                StartCoroutine(wire.FlickerThenGlow());
            }
        }
    }
}
