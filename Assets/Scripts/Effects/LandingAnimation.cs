using UnityEngine;
using UnityEngine.Video;

public class VideoLandingManager : MonoBehaviour
{
    public static VideoLandingManager Instance;

    public VideoPlayer backgroundVideoPlayer;
    public VideoPlayer landingVideoPlayer;

    public VideoClip ambientClip;
    public VideoClip landingClip;

    private int landingQueue = 0;
    private bool isLandingCurrentlyPlaying = false;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (backgroundVideoPlayer != null)
        {
            backgroundVideoPlayer.clip = ambientClip;
            backgroundVideoPlayer.isLooping = true;
            backgroundVideoPlayer.Play();
        }

        if (landingVideoPlayer != null)
        {
            landingVideoPlayer.loopPointReached += OnLandingVideoFinished;
            landingVideoPlayer.gameObject.SetActive(false);
        }
    }

    private void PlayLanding()
    {
        if (landingVideoPlayer != null)
        {
            isLandingCurrentlyPlaying = true;
            landingVideoPlayer.gameObject.SetActive(true);
            landingVideoPlayer.clip = landingClip;
            landingVideoPlayer.isLooping = false;
            landingVideoPlayer.Play();
        }
    }

    public void RequestLandingVideo()
    {
        if (isLandingCurrentlyPlaying)
        {
            landingQueue++;
        }
        else
        {
            PlayLanding();
        }
    }

    private void OnLandingVideoFinished(VideoPlayer vp)
    {
        isLandingCurrentlyPlaying = false;

        if (landingQueue > 0)
        {
            landingQueue--;
            PlayLanding();
        }
        else
        {
            landingVideoPlayer.gameObject.SetActive(false);
        }
    }

    void OnDestroy()
    {
        if (landingVideoPlayer != null)
        {
            landingVideoPlayer.loopPointReached -= OnLandingVideoFinished;
        }
    }
}