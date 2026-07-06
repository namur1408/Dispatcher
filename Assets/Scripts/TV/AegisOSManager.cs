using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class AegisOSManager : MonoBehaviour
{
    [Header("App Windows")]
    public GameObject airTrafficWindow;
    public GameObject inboxWindow;

    [Header("Main Scene Name")]
    public string mainDeskSceneName = "SampleScene"; 

    [Header("TV Sound Settings")]
    public AudioClip tvBackgroundSound;
    [Range(0f, 1f)] public float tvSoundVolume = 0.5f;

    private AudioSource tvAudioSource;

    private float originalListenerVolume = 1f;

    void Awake()
    {
        tvAudioSource = gameObject.AddComponent<AudioSource>();
        tvAudioSource.loop = true;
        tvAudioSource.playOnAwake = false;
        tvAudioSource.ignoreListenerVolume = true;
    }

    void OnEnable()
    {
        originalListenerVolume = AudioListener.volume;
        AudioListener.volume = originalListenerVolume * 0.3f; // Slightly muffle other sounds
        if (tvBackgroundSound != null && tvAudioSource != null)
        {
            tvAudioSource.clip = tvBackgroundSound;
            tvAudioSource.volume = tvSoundVolume;
            if (!tvAudioSource.isPlaying) tvAudioSource.Play();
        }
    }

    void OnDisable()
    {
        AudioListener.volume = originalListenerVolume; // Restoring the volume
        if (tvAudioSource != null) tvAudioSource.Stop();
    }

    void Start()
    {
        if (airTrafficWindow) airTrafficWindow.SetActive(false);
        if (inboxWindow) inboxWindow.SetActive(false);
    }

    public void OpenAirTrafficApp()
    {
        if (airTrafficWindow)
        {
            airTrafficWindow.SetActive(true);
            airTrafficWindow.transform.SetAsLastSibling(); 
        }
    }

    public void OpenInboxApp()
    {
        if (inboxWindow)
        {
            inboxWindow.SetActive(true);
            inboxWindow.transform.SetAsLastSibling(); 
        }
    }

    public void CloseWindow(GameObject windowToClose)
    {
        if (windowToClose)
        {
            windowToClose.SetActive(false);
        }
    }
}