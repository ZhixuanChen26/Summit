using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

public class VideoSceneSwitcher : MonoBehaviour
{
    public VideoPlayer videoPlayer; // Assign in Inspector
    public string nextSceneName = "Main"; // Change to your main scene name

    void Start()
    {
        if (videoPlayer == null)
            videoPlayer = GetComponent<VideoPlayer>();

        // Subscribe to the event that triggers when the video finishes
        videoPlayer.loopPointReached += OnVideoFinished;
    }

    void OnVideoFinished(VideoPlayer vp)
    {
        SceneManager.LoadScene(nextSceneName);
    }
}
