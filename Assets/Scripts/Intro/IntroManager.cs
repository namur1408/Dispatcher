using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

[System.Serializable]
public struct StoryFrame
{
    [Header("Визуал")]
    public Sprite image;
    public Sprite talkingImage;
    public float talkSpeed;

    [Header("Текст")]
    [TextArea(3, 5)]
    public string text;
    public float delayAfter;

    [Header("Аудио")]
    public AudioClip frameSound;
    public float soundDuration;
}

public class IntroManager : MonoBehaviour
{
    [Header("UI Элементы")]
    public Image displayImage;
    public TextMeshProUGUI displayText;

    [Header("Аудио Источники")]
    public AudioSource frameSoundSource;

    [Range(0f, 1f)]
    public float frameSoundVolume = 1.0f;

    [Header("Настройки приглушения")]
    [Range(0f, 1f)]
    public float bgmReducedVolume = 0.2f;
    public float fadeSpeed = 0.5f;

    [Header("Настройки текста")]
    public float typingSpeed = 0.05f;
    public float pauseDuration = 1.0f;

    [Header("Сюжет")]
    public StoryFrame[] frames;

    [Header("Загрузка")]
    public string mainSceneName = "MainMenu";

    private AudioSource mainBGMSource;
    private float originalVolume = 1f;
    private bool isTyping = false;
    private bool isSpeaking = false;
    private bool skipRequested = false;

    void Start()
    {
        GameObject bgmObject = GameObject.Find("MusicManager");
        if (bgmObject != null)
        {
            mainBGMSource = bgmObject.GetComponent<AudioSource>();
            originalVolume = mainBGMSource.volume;
        }

        if (frames.Length > 0)
        {
            StartCoroutine(IntroSequence());
        }
    }

    void Update()
    {
        bool inputPressed = false;
        if (Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame))
            inputPressed = true;
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            inputPressed = true;

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            inputPressed = true;

        if (inputPressed)
        {
            if (isTyping)
            {
                isTyping = false;
            }
            else
            {
                skipRequested = true;
            }
        }
    }

    IEnumerator IntroSequence()
    {
        for (int i = 0; i < frames.Length; i++)
        {
            skipRequested = false;
            StoryFrame currentFrame = frames[i];

            if (currentFrame.frameSound != null)
            {
                if (mainBGMSource != null) StartCoroutine(FadeVolume(mainBGMSource, bgmReducedVolume));

                if (frameSoundSource != null)
                {
                    frameSoundSource.volume = frameSoundVolume;

                    frameSoundSource.clip = currentFrame.frameSound;
                    frameSoundSource.Play();
                    StartCoroutine(StopSoundAfterTime(currentFrame.soundDuration));
                }
            }

            if (currentFrame.image != null) displayImage.sprite = currentFrame.image;

            isTyping = true;
            isSpeaking = true;

            Coroutine talkingCoroutine = null;
            if (currentFrame.talkingImage != null)
                talkingCoroutine = StartCoroutine(AnimateMouth(currentFrame));

            yield return StartCoroutine(TypeText(currentFrame.text));

            if (talkingCoroutine != null)
            {
                StopCoroutine(talkingCoroutine);
                if (currentFrame.image != null) displayImage.sprite = currentFrame.image;
            }

            float timer = 0;
            while (timer < currentFrame.delayAfter && !skipRequested)
            {
                timer += Time.deltaTime;
                yield return null;
            }
        }

        LoadNextScene();
    }

    IEnumerator StopSoundAfterTime(float duration)
    {
        yield return new WaitForSeconds(duration);

        if (frameSoundSource != null) frameSoundSource.Stop();

        if (mainBGMSource != null)
            StartCoroutine(FadeVolume(mainBGMSource, originalVolume));
    }

    IEnumerator FadeVolume(AudioSource source, float targetVolume)
    {
        if (source == null) yield break;

        float startVolume = source.volume;
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * (1 / fadeSpeed);
            source.volume = Mathf.Lerp(startVolume, targetVolume, t);
            yield return null;
        }
        source.volume = targetVolume;
    }

    IEnumerator AnimateMouth(StoryFrame frame)
    {
        float speed = frame.talkSpeed > 0f ? frame.talkSpeed : 0.15f;
        bool isOpen = false;
        while (isTyping)
        {
            if (isSpeaking)
            {
                isOpen = !isOpen;
                displayImage.sprite = isOpen ? frame.talkingImage : frame.image;
                yield return new WaitForSeconds(speed);
            }
            else
            {
                isOpen = false;
                displayImage.sprite = frame.image;
                yield return null;
            }
        }
    }

    IEnumerator TypeText(string fullText)
    {
        displayText.text = "";
        if (string.IsNullOrEmpty(fullText)) { isTyping = false; isSpeaking = false; yield break; }

        for (int i = 0; i < fullText.Length; i++)
        {
            if (!isTyping) break;

            char c = fullText[i];
            if (c == '|')
            {
                isSpeaking = false;

                float pTimer = 0;
                while (pTimer < pauseDuration && isTyping)
                {
                    pTimer += Time.deltaTime;
                    yield return null;
                }

                isSpeaking = true;
                continue;
            }
            displayText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }
        displayText.text = fullText.Replace("|", "");
        isTyping = false;
        isSpeaking = false;
    }

    void LoadNextScene()
    {
        SceneManager.LoadScene(mainSceneName);
    }
}