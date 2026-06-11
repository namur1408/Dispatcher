using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    [Header("Scene Settings")]
    public string gameSceneName = "SampleScene";

    [Header("UI Panels")]
    public GameObject mainButtonsPanel;
    public GameObject continueSelectPanel; // Новая панель для выбора Continue / New Game
    public GameObject modeSelectPanel;
    public GameObject settingsPanel; // Панель настроек

    [Header("Settings Sub-Panels")]
    public GameObject settingsButtonsContainer; // Контейнер с кнопками (Languages, Sounds, Graphics)
    public GameObject audioSlidersContainer;    // Контейнер с ползунками (Master, Music)

    [Header("Boot Animation Settings")]
    public TextMeshProUGUI bootText;
    public string okColorHex = "#4AF626";
    public float typingSpeed = 0.02f;

    [Header("Glitch Effect Containers")]
    public RectTransform textContainer;
    public RectTransform entireScreenContainer;

    private Vector2 baseTextPos;
    private Vector3 baseTextScale;
    private CanvasGroup textCanvasGroup;

    private Vector2 baseScreenPos;
    private Vector3 baseScreenScale;
    private CanvasGroup screenCanvasGroup;

    private System.Text.StringBuilder bootStringBuilder = new System.Text.StringBuilder(512);

    void Start()
    {
        // Изначально включена только главная панель
        if (modeSelectPanel) modeSelectPanel.SetActive(false);
        if (continueSelectPanel) continueSelectPanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(false);
        
        if (settingsButtonsContainer) settingsButtonsContainer.SetActive(true);
        if (audioSlidersContainer) audioSlidersContainer.SetActive(false);

        // Применяем сохраненную громкость при старте
        if (PlayerPrefs.HasKey("MasterVolume"))
        {
            AudioListener.volume = PlayerPrefs.GetFloat("MasterVolume");
        }

        if (textContainer != null)
        {
            baseTextPos = textContainer.anchoredPosition;
            baseTextScale = textContainer.localScale;

            textCanvasGroup = textContainer.GetComponent<CanvasGroup>();
            if (textCanvasGroup == null) textCanvasGroup = textContainer.gameObject.AddComponent<CanvasGroup>();
        }

        if (entireScreenContainer != null)
        {
            baseScreenPos = entireScreenContainer.anchoredPosition;
            baseScreenScale = entireScreenContainer.localScale;

            screenCanvasGroup = entireScreenContainer.GetComponent<CanvasGroup>();
            if (screenCanvasGroup == null) screenCanvasGroup = entireScreenContainer.gameObject.AddComponent<CanvasGroup>();
        }

        StartCoroutine(BootSequenceRoutine());
        StartCoroutine(BackgroundTextGlitchRoutine());
    }

    private IEnumerator BootSequenceRoutine()
    {
        bootStringBuilder.Clear();
        bootText.SetText(bootStringBuilder);
        
        yield return new WaitForSeconds(1.0f);
        yield return StartCoroutine(TypeString("AEGIS OS [v1.4] - BOOT SEQUENCE\n"));
        yield return new WaitForSeconds(0.4f);
        yield return StartCoroutine(LoadModule("> SYSTEM_CORE"));
        yield return StartCoroutine(LoadModule("> RADAR_ARRAY"));
        yield return StartCoroutine(LoadModule("> COMMS_LINK"));
        yield return StartCoroutine(LoadModule("> MAIL_CLIENT"));
        yield return StartCoroutine(LoadModule("> DECRYPT_DIRECTIVES"));

        yield return StartCoroutine(TypeString("> LOADING_MAP_DATA... ["));

        int baseLen = bootStringBuilder.Length;
        int percent = 0;

        while (percent < 98)
        {
            percent += Random.Range(2, 9);
            if (percent > 98) percent = 98;

            bootStringBuilder.Length = baseLen;
            bootStringBuilder.Append(percent).Append("%]");
            bootText.SetText(bootStringBuilder);
            
            yield return new WaitForSeconds(Random.Range(0.05f, 0.2f));
        }

        yield return new WaitForSeconds(2.0f);

        bootStringBuilder.Length = baseLen;
        bootStringBuilder.Append("100%]\n");
        bootText.SetText(bootStringBuilder);
        
        yield return new WaitForSeconds(0.4f);

        yield return StartCoroutine(TypeString("> STATUS: "));
        
        bootStringBuilder.Append($"<color={okColorHex}>NOMINAL</color>");
        bootText.SetText(bootStringBuilder);
    }

    private IEnumerator LoadModule(string moduleName)
    {
        yield return StartCoroutine(TypeString(moduleName));

        for (int i = 0; i < 3; i++)
        {
            yield return new WaitForSeconds(Random.Range(0.2f, 0.4f));
            bootStringBuilder.Append(".");
            bootText.SetText(bootStringBuilder);
        }

        yield return new WaitForSeconds(Random.Range(0.4f, 1.0f));
        bootStringBuilder.Append($" <color={okColorHex}>OK</color>\n");
        bootText.SetText(bootStringBuilder);
    }

    private IEnumerator TypeString(string textToType)
    {
        foreach (char c in textToType)
        {
            bootStringBuilder.Append(c);
            bootText.SetText(bootStringBuilder);
            yield return new WaitForSeconds(typingSpeed);
        }
    }

    private IEnumerator BackgroundTextGlitchRoutine()
    {
        if (textContainer == null) yield break;

        while (true)
        {
            yield return new WaitForSeconds(Random.Range(4f, 10f));

            textContainer.anchoredPosition = baseTextPos + new Vector2(Random.Range(-15f, 15f), Random.Range(-5f, 5f));
            textContainer.localScale = new Vector3(baseTextScale.x * 1.02f, baseTextScale.y * 0.98f, 1f);
            textCanvasGroup.alpha = Random.Range(0.5f, 0.8f);
            yield return new WaitForSeconds(Random.Range(0.05f, 0.12f));

            textContainer.anchoredPosition = baseTextPos + new Vector2(Random.Range(-5f, 5f), Random.Range(-15f, 15f));
            textContainer.localScale = new Vector3(baseTextScale.x * 0.99f, baseTextScale.y * 1.02f, 1f);
            textCanvasGroup.alpha = Random.Range(0.7f, 1f);
            yield return new WaitForSeconds(Random.Range(0.03f, 0.08f));

            RestoreTextNormal();

            if (Random.value > 0.7f)
            {
                yield return new WaitForSeconds(Random.Range(0.1f, 0.3f));

                textContainer.anchoredPosition = baseTextPos + new Vector2(Random.Range(-10f, 10f), 0);
                textCanvasGroup.alpha = 0.6f;
                yield return new WaitForSeconds(0.05f);

                RestoreTextNormal();
            }
        }
    }

    private void RestoreTextNormal()
    {
        if (textContainer != null)
        {
            textContainer.anchoredPosition = baseTextPos;
            textContainer.localScale = baseTextScale;
            if (textCanvasGroup != null) textCanvasGroup.alpha = 1f;
        }
    }

    private IEnumerator PanelTransitionGlitch(GameObject hidePanel, GameObject showPanel)
    {
        if (entireScreenContainer == null)
        {
            hidePanel.SetActive(false);
            showPanel.SetActive(true);
            yield break;
        }

        entireScreenContainer.anchoredPosition = baseScreenPos + new Vector2(Random.Range(-30f, 30f), Random.Range(-15f, 15f));
        entireScreenContainer.localScale = new Vector3(baseScreenScale.x * 1.08f, baseScreenScale.y * 0.9f, 1f);
        screenCanvasGroup.alpha = 0.3f;
        yield return new WaitForSeconds(0.08f);

        hidePanel.SetActive(false);
        showPanel.SetActive(true);

        entireScreenContainer.anchoredPosition = baseScreenPos + new Vector2(Random.Range(-10f, 10f), Random.Range(-25f, 25f));
        entireScreenContainer.localScale = new Vector3(baseScreenScale.x * 0.95f, baseScreenScale.y * 1.05f, 1f);
        screenCanvasGroup.alpha = 0.6f;
        yield return new WaitForSeconds(0.08f);

        entireScreenContainer.anchoredPosition = baseScreenPos + new Vector2(Random.Range(-5f, 5f), 0);
        entireScreenContainer.localScale = baseScreenScale;
        screenCanvasGroup.alpha = 0.8f;
        yield return new WaitForSeconds(0.05f);

        RestoreScreenNormal();
    }

    private void RestoreScreenNormal()
    {
        if (entireScreenContainer != null)
        {
            entireScreenContainer.anchoredPosition = baseScreenPos;
            entireScreenContainer.localScale = baseScreenScale;
            if (screenCanvasGroup != null) screenCanvasGroup.alpha = 1f;
        }
    }

    public void OnStartClicked()
    {
        if (GameSaveManager.HasSave())
        {
            // Если есть сейв, переходим в панель продолжения
            StartCoroutine(PanelTransitionGlitch(mainButtonsPanel, continueSelectPanel));
        }
        else
        {
            // Если сейва нет, сразу переходим к выбору мода
            StartCoroutine(PanelTransitionGlitch(mainButtonsPanel, modeSelectPanel));
        }
    }

    public void OnContinueClicked()
    {
        GameSaveManager.loadedData = GameSaveManager.LoadGame();
        StartCoroutine(LoadSceneWithPreload());
    }

    public void OnNewGameClicked()
    {
        // Удаляем сохранение и идем в панель выбора мода
        GameSaveManager.DeleteSave();
        StartCoroutine(PanelTransitionGlitch(continueSelectPanel, modeSelectPanel));
    }

    public void OnBackFromContinueClicked()
    {
        // Возврат из панели Continue в главное меню
        StartCoroutine(PanelTransitionGlitch(continueSelectPanel, mainButtonsPanel));
    }

    public void OnBackClicked()
    {
        // Возврат из панели выбора мода в главное меню (или Continue, но для простоты вернем в главное)
        StartCoroutine(PanelTransitionGlitch(modeSelectPanel, mainButtonsPanel));
    }

    public void OnSettingsClicked()
    {
        // Убедимся, что при входе в настройки показываются кнопки, а не ползунки
        if (settingsButtonsContainer) settingsButtonsContainer.SetActive(true);
        if (audioSlidersContainer) audioSlidersContainer.SetActive(false);

        // Переход в панель настроек
        StartCoroutine(PanelTransitionGlitch(mainButtonsPanel, settingsPanel));
    }

    public void OnBackFromSettingsClicked()
    {
        // Если мы внутри подменю ползунков, возвращаемся к кнопкам настроек
        if (audioSlidersContainer != null && audioSlidersContainer.activeSelf)
        {
            audioSlidersContainer.SetActive(false);
            if (settingsButtonsContainer) settingsButtonsContainer.SetActive(true);
        }
        else
        {
            // Иначе возвращаемся в главное меню
            StartCoroutine(PanelTransitionGlitch(settingsPanel, mainButtonsPanel));
        }
    }

    public void OnLanguagesClicked()
    {
        Debug.Log("Languages Settings Clicked - Coming Soon");
    }

    public void OnAudioClicked()
    {
        Debug.Log("Audio Settings Clicked - Coming Soon");
    }

    public void OnGraphicsClicked()
    {
        Debug.Log("Graphics Settings Clicked - Coming Soon");
    }

    public void OnMasterVolumeChanged(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat("MasterVolume", value);
        PlayerPrefs.Save();
    }

    public void OnMusicVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat("MusicVolume", value);
        PlayerPrefs.Save();
        // Применяем громкость к BackgroundMusic, если он существует
        var bgMusic = FindFirstObjectByType<BackgroundMusic>();
        if (bgMusic != null)
        {
            var audioSource = bgMusic.GetComponent<AudioSource>();
            if (audioSource != null)
            {
                // Для простоты напрямую меняем volume AudioSource. 
                // Идеально было бы умножать на targetVolume, но это самый быстрый способ
                audioSource.volume = value; 
            }
        }
    }

    public void OnStartWithTutorialClicked()
    {
        ResetGlobalStatics(); 
        PlayerPrefs.SetInt("SkipTutorial", 0);
        PlayerPrefs.Save();
        StartCoroutine(LoadSceneWithPreload());
    }

    public void OnStartSkipTutorialClicked()
    {
        ResetGlobalStatics(); 
        PlayerPrefs.SetInt("SkipTutorial", 1);
        PlayerPrefs.Save();
        
        if (ButtonSoundManager.instance != null) ButtonSoundManager.instance.StopAllSounds();
        StartCoroutine(LoadSceneWithPreload());
    }

    private IEnumerator LoadSceneWithPreload()
    {
        // Создаем черный экран для плавного затемнения поверх всего
        GameObject fadeObj = new GameObject("MainMenuFade");
        Canvas fadeCanvas = fadeObj.AddComponent<Canvas>();
        fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        fadeCanvas.sortingOrder = 32000; // Поверх всего
        fadeObj.AddComponent<UnityEngine.UI.GraphicRaycaster>(); // Блокируем клики
        
        UnityEngine.UI.Image fadeImg = fadeObj.AddComponent<UnityEngine.UI.Image>();
        fadeImg.color = new Color(0, 0, 0, 0);
        fadeImg.raycastTarget = true;
        
        RectTransform rt = fadeObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        float fadeDuration = 1.0f;

        // Плавное затухание музыки
        if (BackgroundMusic.Instance != null)
        {
            BackgroundMusic.Instance.FadeOutToZero(fadeDuration);
        }

        // Плавное затухание (увеличиваем альфу до 1)
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            fadeImg.color = new Color(0, 0, 0, Mathf.Clamp01(timer / fadeDuration));
            yield return null;
        }
        fadeImg.color = Color.black;

        // После затухания начинаем загрузку сцены
        AsyncOperation op = SceneManager.LoadSceneAsync(gameSceneName);
        op.allowSceneActivation = false;
        
        while (op.progress < 0.9f)
        {
            yield return null;
        }

        op.allowSceneActivation = true;
    }

    private void ResetGlobalStatics()
    {
        StoryManager.isFirstGameLoad = true;
        StoryManager.currentDay = 1;
        AegisMailApp.ClearInbox();
    }

    public void OnExitClicked()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}