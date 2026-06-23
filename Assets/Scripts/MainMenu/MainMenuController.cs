using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
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
    public GameObject settingsButtonsContainer;
    public GameObject audioSlidersContainer;
    public GameObject graphicsPanelContainer;

    [Header("Audio Sliders")]
    public Slider musicSlider;  // Перетащи Slider_Music сюда
    public Slider sfxSlider;    // Перетащи Slider_SFX сюда

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
        bool fromGame = PlayerPrefs.GetInt("SettingsFromGame", 0) == 1;

        if (fromGame)
        {
            // Открываем сразу настройки
            if (mainButtonsPanel) mainButtonsPanel.SetActive(false);
            if (modeSelectPanel) modeSelectPanel.SetActive(false);
            if (continueSelectPanel) continueSelectPanel.SetActive(false);
            if (settingsPanel) settingsPanel.SetActive(true);
            
            // Если мы пришли из игры для настроек, скрываем текст бут-секвенции
            if (bootText != null) bootText.gameObject.SetActive(false);
        }
        else
        {
            // Обычный запуск главного меню
            if (mainButtonsPanel) mainButtonsPanel.SetActive(true);
            if (modeSelectPanel) modeSelectPanel.SetActive(false);
            if (continueSelectPanel) continueSelectPanel.SetActive(false);
            if (settingsPanel) settingsPanel.SetActive(false);
        }
        
        if (settingsButtonsContainer) settingsButtonsContainer.SetActive(true);
        if (audioSlidersContainer) audioSlidersContainer.SetActive(false);
        if (graphicsPanelContainer) graphicsPanelContainer.SetActive(false);

        // Если настройки ещё не были установлены нами — принудительно ставим дефолты
        // (сбрасывает старые значения сохранённые до смены дефолтов)
        if (!PlayerPrefs.HasKey("AudioDefaultsSet"))
        {
            PlayerPrefs.SetFloat("MusicVolume", 0.15f);
            PlayerPrefs.SetFloat("SFXVolume",   0.5f);
            PlayerPrefs.SetInt("AudioDefaultsSet", 1);
            PlayerPrefs.Save();
        }

        // Восстанавливаем сохранённую громкость при старте
        float savedMusic = PlayerPrefs.GetFloat("MusicVolume", 0.15f);
        float savedSFX   = PlayerPrefs.GetFloat("SFXVolume",   0.5f);

        // Защита от нулей
        if (savedMusic < 0.01f) { savedMusic = 0.15f; PlayerPrefs.SetFloat("MusicVolume", savedMusic); }
        if (savedSFX   < 0.01f) { savedSFX   = 0.5f;  PlayerPrefs.SetFloat("SFXVolume",   savedSFX);   }
        PlayerPrefs.Save();

        // Устанавливаем слайдеры БЕЗ вызова OnValueChanged
        if (musicSlider != null) musicSlider.SetValueWithoutNotify(savedMusic);
        if (sfxSlider   != null) sfxSlider.SetValueWithoutNotify(savedSFX);

        // Применяем значения к системам звука
        if (BackgroundMusic.Instance != null)
            BackgroundMusic.Instance.SetMusicVolume(savedMusic);
        if (ButtonSoundManager.instance != null)
            ButtonSoundManager.instance.SetVolume(savedSFX);

        Debug.Log($"[Audio Init] Music={savedMusic:F2}, SFX={savedSFX:F2}");

        // Применяем сохранённое качество графики
        if (PlayerPrefs.HasKey("GraphicsQuality"))
        {
            QualitySettings.SetQualityLevel(PlayerPrefs.GetInt("GraphicsQuality"), true);
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

        if (!fromGame)
        {
            StartCoroutine(BootSequenceRoutine());
            StartCoroutine(BackgroundTextGlitchRoutine());
        }
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
        // Защита от незаполненных полей в Inspector
        if (hidePanel == null || showPanel == null)
        {
            Debug.LogWarning($"[Glitch] Панель не назначена в Inspector! hide={hidePanel}, show={showPanel}");
            if (hidePanel != null) hidePanel.SetActive(false);
            if (showPanel != null) showPanel.SetActive(true);
            yield break;
        }

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
        // Убедимся, что при входе в настройки показываются кнопки, а не подпанели
        if (settingsButtonsContainer) settingsButtonsContainer.SetActive(true);
        if (audioSlidersContainer) audioSlidersContainer.SetActive(false);
        if (graphicsPanelContainer) graphicsPanelContainer.SetActive(false);

        // Переход в панель настроек
        StartCoroutine(PanelTransitionGlitch(mainButtonsPanel, settingsPanel));
    }

    public void OnBackFromSettingsClicked()
    {
        // Если мы внутри подменю аудио — возвращаемся к кнопкам настроек
        if (audioSlidersContainer != null && audioSlidersContainer.activeSelf)
        {
            StartCoroutine(PanelTransitionGlitch(audioSlidersContainer, settingsButtonsContainer));
        }
        // Если мы внутри подменю графики — возвращаемся к кнопкам настроек
        else if (graphicsPanelContainer != null && graphicsPanelContainer.activeSelf)
        {
            StartCoroutine(PanelTransitionGlitch(graphicsPanelContainer, settingsButtonsContainer));
        }
        else
        {
            // Проверяем, пришли ли мы из игры
            if (PlayerPrefs.GetInt("SettingsFromGame", 0) == 1)
            {
                // Сбрасываем флаг и возвращаемся в игру
                PlayerPrefs.SetInt("SettingsFromGame", 0);
                PlayerPrefs.Save();
                SceneManager.LoadScene(gameSceneName);
            }
            else
            {
                // Иначе возвращаемся в главное меню
                StartCoroutine(PanelTransitionGlitch(settingsPanel, mainButtonsPanel));
            }
        }
    }

    public void OnAudioClicked()
    {
        // Открыть панель аудио – с глитч-анимацией
        if (graphicsPanelContainer && graphicsPanelContainer.activeSelf)
            graphicsPanelContainer.SetActive(false);
        StartCoroutine(PanelTransitionGlitch(settingsButtonsContainer, audioSlidersContainer));
    }

    public void OnGraphicsClicked()
    {
        // Открыть панель графики – с глитч-анимацией
        if (audioSlidersContainer && audioSlidersContainer.activeSelf)
            audioSlidersContainer.SetActive(false);
        StartCoroutine(PanelTransitionGlitch(settingsButtonsContainer, graphicsPanelContainer));
    }

    // --- Graphics quality ---

    public void OnHighGraphicsClicked()
    {
        Debug.Log("[Settings] Graphics → HIGH");
        if (GraphicsQualityManager.Instance != null)
            GraphicsQualityManager.Instance.ApplyQuality(false); // false = HIGH
        else
        {
            // Fallback если менеджера нет на сцене
            int highLevel = QualitySettings.names.Length - 1;
            QualitySettings.SetQualityLevel(highLevel, true);
            PlayerPrefs.SetInt("GraphicsQuality", highLevel);
            PlayerPrefs.Save();
        }
        StartCoroutine(DeselectNextFrame());
    }

    public void OnLowGraphicsClicked()
    {
        Debug.Log("[Settings] Graphics → LOW");
        if (GraphicsQualityManager.Instance != null)
            GraphicsQualityManager.Instance.ApplyQuality(true); // true = LOW
        else
        {
            QualitySettings.SetQualityLevel(0, true);
            PlayerPrefs.SetInt("GraphicsQuality", 0);
            PlayerPrefs.Save();
        }
        StartCoroutine(DeselectNextFrame());
    }

    // Сбрасываем выделение кнопки на следующий кадр —
    // иначе Unity не даёт нажать ту же кнопку повторно
    private IEnumerator DeselectNextFrame()
    {
        yield return null;
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    public void OnMusicVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat("MusicVolume", value);
        PlayerPrefs.Save();

        Debug.Log($"[Audio] Music slider → {value:F2} | BackgroundMusic.Instance = {BackgroundMusic.Instance}");

        if (BackgroundMusic.Instance != null)
        {
            BackgroundMusic.Instance.SetMusicVolume(value);
        }
        else
        {
            // Если синглтон почему-то недоступен — ищем напрямую
            var bgMusic = FindFirstObjectByType<BackgroundMusic>();
            if (bgMusic != null) bgMusic.SetMusicVolume(value);
            else Debug.LogWarning("[Audio] BackgroundMusic не найден в сцене!");
        }
    }

    public void OnSFXVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat("SFXVolume", value);
        PlayerPrefs.Save();

        Debug.Log($"[Audio] SFX slider → {value:F2} | ButtonSoundManager.instance = {ButtonSoundManager.instance}");

        if (ButtonSoundManager.instance != null)
            ButtonSoundManager.instance.SetVolume(value);
        else
            Debug.LogWarning("[Audio] ButtonSoundManager не найден!");
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