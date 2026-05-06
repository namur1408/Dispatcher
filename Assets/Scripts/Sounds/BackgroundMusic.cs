using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class BackgroundMusic : MonoBehaviour
{
    private static BackgroundMusic instance;
    private AudioSource audioSource;

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

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            audioSource = GetComponent<AudioSource>();
            normalVolume = audioSource.volume;
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
        StopAllCoroutines();

        if (quietScenes.Contains(scene.name))
        {
            StartCoroutine(FadeVolume(quietVolume));
        }
        else
        {
            StartCoroutine(FadeVolume(normalVolume));
        }
    }

    IEnumerator FadeVolume(float target)
    {
        while (!Mathf.Approximately(audioSource.volume, target))
        {
            audioSource.volume = Mathf.MoveTowards(audioSource.volume, target, fadeSpeed * Time.deltaTime);
            yield return null;
        }
        audioSource.volume = target;
    }
}