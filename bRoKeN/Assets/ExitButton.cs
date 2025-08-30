using UnityEngine;

public class ExitButton : MonoBehaviour
{
    // This method can be called by a UI Button's OnClick event
    // or by another interaction method (such as OnMouseDown).
    public void ExitGame()
    {
        Debug.Log("Exit button pressed. Exiting application.");
        Application.Quit();

        // Optional: If you're testing in the Editor, you can stop play mode:
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

}
