using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class BackgroundMusic : MonoBehaviour
{
    private static BackgroundMusic instance;
    private AudioSource audioSource;

    [Header("Трек Главного Меню")]
    public AudioClip menuMusic;
    [Range(0f, 1f)] public float menuVolume = 1f;

    [Header("Треки Игры (Плейлист)")]
    public List<AudioClip> gameMusicTracks = new List<AudioClip>();
    [Range(0f, 1f)] public float gameVolume = 0.5f;

    [Header("Настройки сцен и затухания")]
    public string menuSceneName = "MainMenu";
    public float fadeDuration = 3f; // Decay time in seconds

    private int currentGameTrackIndex = 0;
    private float currentTargetVolume = 1f;
    private float userVolumeMultiplier = 1f; // Multiplier from the slider in the settings
    private bool isFadingOut = false;
    private bool isFadingIn = false;

    private enum MusicState { Menu, Game }
    private MusicState currentState = MusicState.Menu;

    public static BackgroundMusic Instance
    {
        get { return instance; }
    }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            transform.SetParent(null); 
            DontDestroyOnLoad(gameObject);
            
            audioSource = GetComponent<AudioSource>();
            audioSource.loop = false;
            // userVolumeMultiplier is loaded via SetMusicVolume() from MainMenuController.Start()
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    // Call before loading scene
    public void FadeOutToZero(float duration)
    {
        StopAllCoroutines();
        isFadingOut = true;
        isFadingIn = false;
        StartCoroutine(FadeOutCoroutine(duration));
    }

    /// <summary>
    /// Sets the music volume via the slider. Applies instantly.
    /// </summary>
    public void SetMusicVolume(float normalizedValue)
    {
        userVolumeMultiplier = Mathf.Clamp01(normalizedValue);

        // We always apply it immediately, even during a fade -
        // otherwise the slider does not respond for 3 seconds while FadeIn is in progress
        if (audioSource != null && !isFadingOut)
        {
            audioSource.volume = currentTargetVolume * userVolumeMultiplier;
        }
    }

    private IEnumerator FadeOutCoroutine(float duration)
    {
        float startVol = audioSource.volume;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            audioSource.volume = Mathf.Lerp(startVol, 0f, t / duration);
            yield return null;
        }
        audioSource.volume = 0f;
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        MusicState newState = (scene.name == menuSceneName) ? MusicState.Menu : MusicState.Game;

        if (newState != currentState || !audioSource.isPlaying)
        {
            currentState = newState;
            PlayCurrentStateMusic();
        }
    }

    void PlayCurrentStateMusic()
    {
        StopAllCoroutines();
        isFadingOut = false;
        
        AudioClip targetClip = null;
        
        if (currentState == MusicState.Menu)
        {
            targetClip = menuMusic;
            currentTargetVolume = menuVolume;
        }
        else if (currentState == MusicState.Game)
        {
            if (gameMusicTracks.Count > 0)
            {
                if (currentGameTrackIndex >= gameMusicTracks.Count) currentGameTrackIndex = 0;
                targetClip = gameMusicTracks[currentGameTrackIndex];
            }
            currentTargetVolume = gameVolume;
        }

        if (targetClip != null)
        {
            audioSource.clip = targetClip;
            audioSource.volume = 0f; // Starting from scratch for a smooth appearance
            audioSource.Play();
            StartCoroutine(FadeIn());
        }
    }

    void Update()
    {
        if (!audioSource.isPlaying || audioSource.clip == null) return;
        if (isFadingOut || isFadingIn) return; 

        float remainingTime = audioSource.clip.length - audioSource.time;

        // If there is less time left until the end of the track than the fade duration
        if (remainingTime <= fadeDuration)
        {
            StartCoroutine(FadeOutAndNext());
        }
    }

    private IEnumerator FadeIn()
    {
        isFadingIn = true;
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            // Read userVolumeMultiplier every frame -
            // this is how the slider works in real time even during a fade
            float targetVol = currentTargetVolume * userVolumeMultiplier;
            audioSource.volume = Mathf.Lerp(0f, targetVol, timer / fadeDuration);
            yield return null;
        }
        audioSource.volume = currentTargetVolume * userVolumeMultiplier;
        isFadingIn = false;
    }

    private IEnumerator FadeOutAndNext()
    {
        isFadingOut = true;
        float startVolume = audioSource.volume;
        float timer = 0f;
        
        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0f, timer / fadeDuration);
            yield return null;
        }
        
        audioSource.volume = 0f;
        audioSource.Stop();
        isFadingOut = false;

        // Select the next track
        if (currentState == MusicState.Game)
        {
            if (gameMusicTracks.Count > 0)
            {
                currentGameTrackIndex++;
                if (currentGameTrackIndex >= gameMusicTracks.Count)
                {
                    currentGameTrackIndex = 0; // The playlist went in circles
                }
            }
        }
        // For Menu, the track does not change, it will simply start again

        PlayCurrentStateMusic();
    }
}

