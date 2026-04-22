using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

[System.Serializable]
public struct StoryFrame
{
    public Sprite image;
    [TextArea(3, 5)]
    public string text;

    [Tooltip("Задержка перед переходом к следующему кадру")]
    public float delayAfter;

    [Header("Аудио (Опционально)")]
    [Tooltip("Звук/Музыка для этого кадра. Если указан, основная музыка встанет на паузу.")]
    public AudioClip frameSound;
}

public class IntroManager : MonoBehaviour
{
    [Header("UI Элементы")]
    public Image displayImage;
    public TextMeshProUGUI displayText;

    [Header("Аудио")]
    [Tooltip("Источник для основной фоновой музыки")]
    public AudioSource mainBGMSource;
    [Tooltip("Источник для звуков/музыки конкретных кадров")]
    public AudioSource frameSoundSource;

    [Header("Настройки текста")]
    public float typingSpeed = 0.05f;

    [Header("Сюжет")]
    public StoryFrame[] frames;

    [Header("Загрузка")]
    public string nextSceneName = "Main Menu";

    private bool isTyping = false;
    private bool skipRequested = false;

    void Start()
    {
        // Запускаем основную музыку, если она назначена
        if (mainBGMSource != null && mainBGMSource.clip != null)
        {
            mainBGMSource.loop = true; // Зацикливаем фон
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

            // --- ЛОГИКА АУДИО ---
            if (frames[i].frameSound != null)
            {
                // Если у кадра есть свой звук: ставим фон на паузу и играем звук кадра
                if (mainBGMSource != null && mainBGMSource.isPlaying)
                    mainBGMSource.Pause();

                if (frameSoundSource != null)
                {
                    frameSoundSource.clip = frames[i].frameSound;
                    frameSoundSource.Play();
                }
            }
            else
            {
                // Если у кадра нет своего звука: останавливаем звук прошлого кадра и возвращаем фон
                if (frameSoundSource != null)
                    frameSoundSource.Stop();

                if (mainBGMSource != null && !mainBGMSource.isPlaying)
                    mainBGMSource.UnPause();
            }

            // 1. Устанавливаем картинку
            if (frames[i].image != null) displayImage.sprite = frames[i].image;

            // 2. Печатаем текст
            yield return StartCoroutine(TypeText(frames[i].text));

            // 3. Ждем задержку или клик
            float timer = 0;
            while (timer < frames[i].delayAfter && !skipRequested)
            {
                timer += Time.deltaTime;
                yield return null;
            }
        }

        LoadNextScene();
    }

    IEnumerator TypeText(string fullText)
    {
        displayText.text = "";

        if (string.IsNullOrEmpty(fullText))
        {
            isTyping = false;
            yield break;
        }

        isTyping = true;

        foreach (char c in fullText)
        {
            if (!isTyping) break;

            displayText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }

        displayText.text = fullText;
        isTyping = false;
    }

    void LoadNextScene()
    {
        SceneManager.LoadScene(nextSceneName);
    }
}