using UnityEngine;
using UnityEngine.Video;

public class VideoLandingManager : MonoBehaviour
{
    public static VideoLandingManager Instance;
    public VideoPlayer mainVideoPlayer;

    public float ambientEndTime = 5.0f;

    private int landingQueue = 0;
    private bool isLandingCurrentlyPlaying = false;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (mainVideoPlayer != null)
        {
            mainVideoPlayer.loopPointReached += OnVideoFinished;

            mainVideoPlayer.isLooping = true;

            if (!mainVideoPlayer.isPlaying) mainVideoPlayer.Play();
        }
    }

    void Update()
    {
        if (mainVideoPlayer != null && mainVideoPlayer.isPlaying)
        {
            if (!isLandingCurrentlyPlaying && mainVideoPlayer.time >= ambientEndTime)
            {
                if (landingQueue > 0)
                {
                    isLandingCurrentlyPlaying = true;
                    landingQueue--;
                }
                else
                {
                    mainVideoPlayer.time = 0f;
                }
            }
        }
    }

    public void RequestLandingVideo()
    {
        landingQueue++;
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        isLandingCurrentlyPlaying = false;
    }

    void OnDestroy()
    {
        if (mainVideoPlayer != null) mainVideoPlayer.loopPointReached -= OnVideoFinished;
    }
}