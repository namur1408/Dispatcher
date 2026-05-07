using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class BackgroundMusic : MonoBehaviour
{
    private static BackgroundMusic instance;
    private AudioSource audioSource;

    [Header("Аудио Треки")]
    public AudioClip menuMusic;
    public AudioClip introMusic;
    public AudioClip gameMusic;

    [Header("Настройки сцен")]
    public string menuSceneName = "MainMenu";
    public string introSceneName = "IntroScene"; 

    [Header("Настройки громкости")]
    public float normalVolume = 0.7f;
    public float quietVolume = 0.03f;
    public float fadeSpeed = 1.2f;

    private List<string> quietScenes = new List<string>
    {
        "BigRadarScene",
        "CommsScene",
        "ManualScene",
        "SampleScene",
        "TVInfoScene"
    };

    private Coroutine fadeCoroutine;

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
        AudioClip targetClip = gameMusic;
        float targetVolume = quietScenes.Contains(scene.name) ? quietVolume : normalVolume;

        if (scene.name == menuSceneName)
        {
            targetClip = menuMusic;
            targetVolume = normalVolume;
        }
        else if (scene.name == introSceneName)
        {
            targetClip = introMusic;
            targetVolume = normalVolume;
        }

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(SwitchTrackAndFade(targetClip, targetVolume));
    }

    IEnumerator SwitchTrackAndFade(AudioClip newClip, float targetVol)
    {
        if (audioSource.clip == newClip)
        {
            if (!audioSource.isPlaying) audioSource.Play();

            while (!Mathf.Approximately(audioSource.volume, targetVol))
            {
                audioSource.volume = Mathf.MoveTowards(audioSource.volume, targetVol, fadeSpeed * Time.deltaTime);
                yield return null;
            }
            audioSource.volume = targetVol;
        }
        else
        {
            if (audioSource.isPlaying)
            {
                while (audioSource.volume > 0)
                {
                    audioSource.volume = Mathf.MoveTowards(audioSource.volume, 0, fadeSpeed * Time.deltaTime);
                    yield return null;
                }
            }
            audioSource.clip = newClip;
            audioSource.Play();

            while (!Mathf.Approximately(audioSource.volume, targetVol))
            {
                audioSource.volume = Mathf.MoveTowards(audioSource.volume, targetVol, fadeSpeed * Time.deltaTime);
                yield return null;
            }
            audioSource.volume = targetVol;
        }
    }
}