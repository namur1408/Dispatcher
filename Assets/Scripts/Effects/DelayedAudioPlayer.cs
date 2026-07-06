using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class DelayedAudioPlayer : MonoBehaviour
{
    [Tooltip("Задержка в секундах перед воспроизведением звука")]
    public float initialDelay = 5f;

    [Tooltip("Если больше 0, звук будет повторяться это время, а затем доиграет до конца и остановится")]
    public float playDuration = 0f;

    private AudioSource audioSource;
    private bool originalLoopState;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        originalLoopState = audioSource.loop;
    }

    void OnEnable()
    {
        // Restoring Loop to its original state every time you turn it on
        if (audioSource != null)
        {
            audioSource.loop = originalLoopState;
        }
        StartCoroutine(PlayWithDelay());
    }

    void OnDisable()
    {
        StopAllCoroutines();
        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }

    private IEnumerator PlayWithDelay()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
            yield return new WaitForSeconds(initialDelay);
            audioSource.Play();

            // If playback time is set, wait for this time and turn off looping
            if (playDuration > 0f)
            {
                yield return new WaitForSeconds(playDuration);
                audioSource.loop = false;
            }
        }
    }
}
