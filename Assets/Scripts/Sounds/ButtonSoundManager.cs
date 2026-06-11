using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ButtonSoundManager : MonoBehaviour
{
    public static ButtonSoundManager instance;

    [Header("Настройки звука по умолчанию")]
    public AudioClip clickSound;

    [Range(0f, 1f)]
    public float volume = 0.7f;

    private AudioSource audioSource;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

            audioSource.playOnAwake = false;
            audioSource.ignoreListenerVolume = true;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
    void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    void Start()
    {
        // Periodically check for dynamically instantiated buttons (e.g. TV flight lists)
        InvokeRepeating(nameof(AssignSoundsToAllButtons), 1f, 1.5f);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AssignSoundsToAllButtons();
    }

    public void AssignSoundsToAllButtons()
    {
        Button[] allButtons = Resources.FindObjectsOfTypeAll<Button>();
        foreach (Button btn in allButtons)
        {
            if (btn.gameObject.scene.name == null) continue;

            if (btn.GetComponent<ButtonSoundTrigger>() == null)
            {
                btn.gameObject.AddComponent<ButtonSoundTrigger>();
            }
        }
    }

    public void PlayDefaultClick()
    {
        if (clickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(clickSound, volume);
        }
    }

    public void PlaySpecialSound(AudioClip clip, float customVolume)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip, customVolume);
        }
    }

    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);
    }

    public void StopAllSounds()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }
}