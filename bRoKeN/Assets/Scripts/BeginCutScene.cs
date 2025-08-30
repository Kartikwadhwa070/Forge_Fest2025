using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;

public class BeginCutScene : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private VideoPlayer videoPlayer; // Assign in Inspector
    [SerializeField] private RawImage rawImage;      // Assign the RawImage showing the video
    [SerializeField] private GameObject canvas;      // Assign the Canvas (or any GameObject to disable)

    void Start()
    {
        // Automatically assign references if not set
        if (videoPlayer == null)
            videoPlayer = GetComponent<VideoPlayer>();

        if (rawImage == null)
            rawImage = GetComponentInChildren<RawImage>();

        if (canvas == null)
            canvas = gameObject; // Use this GameObject if unassigned

        // Set up the video player
        rawImage.texture = videoPlayer.targetTexture; // Ensure the RawImage uses the Render Texture
        videoPlayer.Play(); // Start playing the video

        // Listen for the video end event
        videoPlayer.loopPointReached += OnVideoEnd;
    }

    // Called when the video finishes
    private void OnVideoEnd(VideoPlayer source)
    {
        canvas.SetActive(false); // Disable the Canvas (or GameObject)
    }

    // Unsubscribe from the event to prevent memory leaks
    private void OnDestroy()
    {
        if (videoPlayer != null)
            videoPlayer.loopPointReached -= OnVideoEnd;
    }
}