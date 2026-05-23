using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class BackgroundMusic : MonoBehaviour
{
    private static BackgroundMusic instance;
    private AudioSource audioSource;

    [Header("Аудио Треки")]
    public AudioClip menuMusic;
    [Range(0f, 1f)] public float menuVolume = 1f;

    public AudioClip tvMusic;
    [Range(0f, 1f)] public float tvVolume = 0.5f;

    public AudioClip gameMusic;
    [Range(0f, 1f)] public float gameVolume = 0.5f;

    [Header("Настройки сцен")]
    public string menuSceneName = "MainMenu";
    public string tvSceneName = "TVInfoScene";

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            audioSource = GetComponent<AudioSource>();
        }
        else
        {
            Destroy(gameObject);
            return;
        }
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
        AudioClip targetClip;
        float targetVolume;

        // Если это главное меню - играем первый трек
        if (scene.name == menuSceneName)
        {
            targetClip = menuMusic;
            targetVolume = menuVolume;
        }
        // Если это сцена телевизора - играем звук телевизора
        else if (scene.name == tvSceneName)
        {
            targetClip = tvMusic;
            targetVolume = tvVolume;
        }
        // Во всех остальных сценах - играем трек игры
        else 
        {
            targetClip = gameMusic;
            targetVolume = gameVolume;
        }

        // Мгновенное переключение трека и громкости
        if (audioSource.clip != targetClip)
        {
            audioSource.clip = targetClip;
            audioSource.volume = targetVolume;
            audioSource.Play();
        }
        else
        {
            // Если трек тот же, просто мгновенно меняем громкость
            audioSource.volume = targetVolume;
            if (!audioSource.isPlaying) audioSource.Play();
        }
    }
}