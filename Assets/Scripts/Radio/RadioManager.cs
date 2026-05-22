using UnityEngine;
using UnityEngine.SceneManagement;

public class RadioManager : MonoBehaviour
{
    public static RadioManager Instance;

    [Header("Визуал")]
    public GameObject blinkingLight;
    public float blinkSpeed = 2f;

    [Header("Звук вызова")]
    public AudioClip ringSound;
    [Range(0f, 1f)] public float ringVolume = 0.6f;
    private AudioSource audioSource;

    public static string activeCallsign = "";
    public static bool isNewCall = false;

    private float blinkTimer = 0f;

    void Awake()
    {
        Instance = this;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;

        audioSource.loop = true;

        if (blinkingLight != null) blinkingLight.SetActive(activeCallsign != "");
    }

    void Update()
    {
        if (activeCallsign != "")
        {
            if (isNewCall)
            {
                blinkTimer += Time.deltaTime * blinkSpeed;
                if (blinkingLight != null) blinkingLight.SetActive(Mathf.Sin(blinkTimer) > 0);

                if (ringSound != null && audioSource != null && !audioSource.isPlaying)
                {
                    audioSource.clip = ringSound;
                    audioSource.volume = ringVolume;
                    audioSource.Play();
                }
            }
            else
            {
                if (blinkingLight != null) blinkingLight.SetActive(true);

                if (audioSource != null && audioSource.isPlaying)
                {
                    audioSource.Stop();
                }
            }
        }
        else
        {
            if (blinkingLight != null) blinkingLight.SetActive(false);

            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }
        }
    }

    public void OnRadioClicked()
    {
        if (activeCallsign != "")
        {
            isNewCall = false;

            if (audioSource != null) audioSource.Stop();

            if (RadarManager.Instance != null) RadarManager.Instance.SaveToGlobalManager(); SceneManager.LoadScene("CommsScene");
        }
    }
}
