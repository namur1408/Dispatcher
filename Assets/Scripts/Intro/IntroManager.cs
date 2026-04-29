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

    [Header("Аудио (Опционально)")]
    public AudioClip frameSound;
}

public class IntroManager : MonoBehaviour
{
    [Header("UI Элементы")]
    public Image displayImage;
    public TextMeshProUGUI displayText;

    [Header("Аудио")]
    public AudioSource mainBGMSource;
    public AudioSource frameSoundSource;

    [Header("Настройки текста")]
    public float typingSpeed = 0.05f;
    public float pauseDuration = 1.0f; 

    [Header("Сюжет")]
    public StoryFrame[] frames;

    [Header("Загрузка")]
    public string nextSceneName = "Main Menu";

    private bool isTyping = false;
    private bool isSpeaking = false; 
    private bool skipRequested = false;

    void Start()
    {
        if (mainBGMSource != null && mainBGMSource.clip != null)
        {
            mainBGMSource.loop = true;
            mainBGMSource.Play();
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

            if (frames[i].frameSound != null)
            {
                if (mainBGMSource != null && mainBGMSource.isPlaying) mainBGMSource.Pause();
                if (frameSoundSource != null)
                {
                    frameSoundSource.clip = frames[i].frameSound;
                    frameSoundSource.Play();
                }
            }
            else
            {
                if (frameSoundSource != null) frameSoundSource.Stop();
                if (mainBGMSource != null && !mainBGMSource.isPlaying) mainBGMSource.UnPause();
            }

            if (frames[i].image != null) displayImage.sprite = frames[i].image;

            isTyping = true;
            isSpeaking = true; 

            Coroutine talkingCoroutine = null;
            if (frames[i].talkingImage != null)
            {
                talkingCoroutine = StartCoroutine(AnimateMouth(frames[i]));
            }

            yield return StartCoroutine(TypeText(frames[i].text));

            if (talkingCoroutine != null)
            {
                StopCoroutine(talkingCoroutine);
                if (frames[i].image != null) displayImage.sprite = frames[i].image;
            }

            float timer = 0;
            while (timer < frames[i].delayAfter && !skipRequested)
            {
                timer += Time.deltaTime;
                yield return null;
            }
        }

        LoadNextScene();
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

        if (string.IsNullOrEmpty(fullText))
        {
            isTyping = false;
            isSpeaking = false;
            yield break;
        }

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
        SceneManager.LoadScene(nextSceneName);
    }
}