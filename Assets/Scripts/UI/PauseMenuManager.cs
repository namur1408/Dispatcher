using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;
using System;

public class PauseMenuManager : MonoBehaviour
{
    public static PauseMenuManager Instance;

    [Header("UI References")]
    [Tooltip("Перетащите сюда Canvas или панель вашего меню")]
    public GameObject pauseCanvasObj;
    
    [Tooltip("Текст для даты (AUG 19 2038)")]
    public TextMeshProUGUI dateText;
    
    [Tooltip("Текст для времени (PM 14:18)")]
    public TextMeshProUGUI timeText;

    [Header("Settings UI")]
    [Tooltip("Перетащите сюда все главные кнопки (Resume, Settings, MainMenu), чтобы они скрывались при открытии настроек")]
    public GameObject[] pauseMainButtons; 
    public GameObject settingsPanel;  // Settings panel
    public GameObject settingsButtonsContainer;
    public GameObject audioSlidersContainer;
    public GameObject graphicsPanelContainer;

    [Header("Audio Sliders")]
    public Slider musicSlider;
    public Slider sfxSlider;

    [Header("VHS Effect Settings")]
    public bool enableVHSEffect = true;
    public int scanlinesCount = 40; // More lines as they are now like small noise
    public float noiseSpeed = 0.05f; // Update a little faster for the “noise” effect
    
    [Header("Text Glitch (Chromatic Aberration)")]
    [Tooltip("Автоматически применить эффект ко всем текстам в меню паузы (кнопкам, дате и т.д.)")]
    public bool applyGlitchToAllTexts = true;
    [Tooltip("Или добавьте вручную тексты, на которых хотите сделать RGB искажение")]
    public TextMeshProUGUI[] glitchTextsToEffect;
    public float rgbOffset = 4f; // How many pixels do the colors spread across?
    
    private bool isPaused = false;
    private float noiseTimer = 0f;
    private GameObject vhsContainer;
    private RectTransform vhsContainerRt;
    private RectTransform[] scanlineRects;
    private Image[] scanlineImages;

    // Cache clone data.
    private class GlitchTextData
    {
        public TextMeshProUGUI original;
        public TextMeshProUGUI redClone;
        public TextMeshProUGUI blueClone;
    }
    private System.Collections.Generic.List<GlitchTextData> glitchDataList = new System.Collections.Generic.List<GlitchTextData>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Removed automatic generation of UI at your request
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (enableVHSEffect && pauseCanvasObj != null)
        {
            CreateVHSEffect();
            CreateChromaticAberration();
        }
    }

    void Update()
    {
#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
        }
#else
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
#endif

        // Process effects and update UI while paused.
        if (isPaused)
        {
            UpdateDateAndTime();
            
            if (enableVHSEffect)
            {
                UpdateVHSEffect();
                AnimateChromaticAberration();
            }
        }
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;
        
        if (pauseCanvasObj != null)
        {
            pauseCanvasObj.SetActive(isPaused);
        }

        if (isPaused)
        {
            // Reset the panels to the initial ones (Main menu pause)
            if (pauseMainButtons != null)
            {
                foreach (var btn in pauseMainButtons)
                {
                    if (btn != null) btn.SetActive(true);
                }
            }
            if (settingsPanel != null) settingsPanel.SetActive(false);

            // Synchronizing sliders when opening a pause
            float savedMusic = PlayerPrefs.GetFloat("MusicVolume", 0.15f);
            float savedSFX   = PlayerPrefs.GetFloat("SFXVolume",   0.5f);
            
            if (musicSlider != null) musicSlider.SetValueWithoutNotify(savedMusic);
            if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(savedSFX);
        }
    }

    public void OnSettingsClicked()
    {
        // Open settings sub-menu.
        if (pauseMainButtons != null)
        {
            foreach (var btn in pauseMainButtons)
            {
                if (btn != null) btn.SetActive(false);
            }
        }
        if (settingsPanel != null) settingsPanel.SetActive(true);
        
        if (settingsButtonsContainer != null) settingsButtonsContainer.SetActive(true);
        if (audioSlidersContainer != null) audioSlidersContainer.SetActive(false);
        if (graphicsPanelContainer != null) graphicsPanelContainer.SetActive(false);
    }

    public void OnBackFromSettingsClicked()
    {
        // Handle audio sub-menu navigation.
        if (audioSlidersContainer != null && audioSlidersContainer.activeSelf)
        {
            audioSlidersContainer.SetActive(false);
            if (settingsButtonsContainer != null) settingsButtonsContainer.SetActive(true);
        }
        // Handle graphics sub-menu navigation.
        else if (graphicsPanelContainer != null && graphicsPanelContainer.activeSelf)
        {
            graphicsPanelContainer.SetActive(false);
            if (settingsButtonsContainer != null) settingsButtonsContainer.SetActive(true);
        }
        else
        {
            // Returning to the main pause menu
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (pauseMainButtons != null)
            {
                foreach (var btn in pauseMainButtons)
                {
                    if (btn != null) btn.SetActive(true);
                }
            }
        }
    }

    public void OnAudioClicked()
    {
        if (graphicsPanelContainer != null) graphicsPanelContainer.SetActive(false);
        if (settingsButtonsContainer != null) settingsButtonsContainer.SetActive(false);
        if (audioSlidersContainer != null) audioSlidersContainer.SetActive(true);
    }

    public void OnGraphicsClicked()
    {
        if (audioSlidersContainer != null) audioSlidersContainer.SetActive(false);
        if (settingsButtonsContainer != null) settingsButtonsContainer.SetActive(false);
        if (graphicsPanelContainer != null) graphicsPanelContainer.SetActive(true);
    }

    public void OnHighGraphicsClicked()
    {
        Debug.Log("[PauseSettings] Graphics → HIGH");
        if (GraphicsQualityManager.Instance != null)
            GraphicsQualityManager.Instance.ApplyQuality(false); // false = HIGH
        else
        {
            int highLevel = QualitySettings.names.Length - 1;
            QualitySettings.SetQualityLevel(highLevel, true);
            PlayerPrefs.SetInt("GraphicsQuality", highLevel);
            PlayerPrefs.Save();
        }
        StartCoroutine(DeselectNextFrame());
    }

    public void OnLowGraphicsClicked()
    {
        Debug.Log("[PauseSettings] Graphics → LOW");
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

    private System.Collections.IEnumerator DeselectNextFrame()
    {
        yield return null;
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    public void OnMusicVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat("MusicVolume", value);
        PlayerPrefs.Save();

        if (BackgroundMusic.Instance != null)
        {
            BackgroundMusic.Instance.SetMusicVolume(value);
        }
        else
        {
            var bgMusic = FindFirstObjectByType<BackgroundMusic>();
            if (bgMusic != null) bgMusic.SetMusicVolume(value);
        }
    }

    public void OnSFXVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat("SFXVolume", value);
        PlayerPrefs.Save();

        if (ButtonSoundManager.instance != null)
            ButtonSoundManager.instance.SetVolume(value);
    }

    public void OnExitClicked()
    {
        Time.timeScale = 1f; // We return the time before leaving
        
        if (RadarManager.Instance != null)
        {
            RadarManager.Instance.SaveToGlobalManager();
        }
        
        // Persist game state to disk before exit.
        GameSaveManager.SaveGame();
        PlayerPrefs.Save();
        
        SceneManager.LoadScene("MainMenu");
    }

    private void UpdateDateAndTime()
    {
        if (dateText != null)
        {
            // We format the date of the game (in the game it is August, it starts on the 19th)
            int day = 1;
            // Trying to get the current day from StoryManager
            day = StoryManager.currentDay; 
            
            string formattedDate = "AUG " + (18 + day).ToString("D2") + "\n2038";
            dateText.text = formattedDate;
        }

        if (timeText != null)
        {
            // Display real-world system time (VHS style). (like old VHS recordings)
            timeText.text = DateTime.Now.ToString("tt HH:mm").ToUpper();
        }
    }

    private void CreateVHSEffect()
    {
        // Create a container for clutter right inside your canvas
        vhsContainer = new GameObject("VHS_Noise_Lines");
        vhsContainer.transform.SetParent(pauseCanvasObj.transform, false);
        vhsContainer.transform.SetAsLastSibling(); // The noise will always be on top of the buttons!

        vhsContainerRt = vhsContainer.AddComponent<RectTransform>();
        vhsContainerRt.anchorMin = Vector2.zero;
        vhsContainerRt.anchorMax = Vector2.one;
        vhsContainerRt.offsetMin = Vector2.zero;
        vhsContainerRt.offsetMax = Vector2.zero;

        scanlineRects = new RectTransform[scanlinesCount];
        scanlineImages = new Image[scanlinesCount];

        for (int i = 0; i < scanlinesCount; i++)
        {
            GameObject lineObj = new GameObject("VHSLine");
            lineObj.transform.SetParent(vhsContainer.transform, false);
            
            Image img = lineObj.AddComponent<Image>();
            img.raycastTarget = false; 
            
            RectTransform lineRt = lineObj.GetComponent<RectTransform>();
            // Now we center the anchor so that the lines are not on the entire screen, but in pieces
            lineRt.anchorMin = new Vector2(0.5f, 0.5f);
            lineRt.anchorMax = new Vector2(0.5f, 0.5f);
            
            scanlineRects[i] = lineRt;
            scanlineImages[i] = img;
            
            lineObj.SetActive(false);
        }
    }

    private void UpdateVHSEffect()
    {
        if (scanlineRects == null || vhsContainerRt == null) return;
        
        // We use unscaledDeltaTime, since Time.timeScale = 0 on pause
        noiseTimer += Time.unscaledDeltaTime;
        if (noiseTimer < noiseSpeed) return;
        
        noiseTimer = 0f; // Resetting the timer
        
        float halfWidth = vhsContainerRt.rect.width * 0.5f;
        float halfHeight = vhsContainerRt.rect.height * 0.5f;
        
        for (int i = 0; i < scanlinesCount; i++)
        {
            RectTransform rt = scanlineRects[i];
            Image img = scanlineImages[i];

            // The first line (i == 0) will always be responsible for that very rare thick stripe
            if (i == 0)
            {
                if (UnityEngine.Random.value > 0.95f) // Appears even less frequently (5% chance per animation frame)
                {
                    rt.gameObject.SetActive(true);
                    float yPos = UnityEngine.Random.Range(-halfHeight, halfHeight);
                    rt.anchoredPosition = new Vector2(0, yPos);
                    // Container width plus a small margin to accurately cover the entire screen
                    rt.sizeDelta = new Vector2(halfWidth * 2f + 200f, UnityEngine.Random.Range(20f, 80f)); 
                    img.color = new Color(1f, 1f, 1f, UnityEngine.Random.Range(0.1f, 0.3f));
                }
                else
                {
                    rt.gameObject.SetActive(false);
                }
                continue;
            }

            // All other lines are constant fine white noise
            if (UnityEngine.Random.value > 0.7f) // The noise also became less frequent (30% chance)
            {
                rt.gameObject.SetActive(true);
                
                float xPos = UnityEngine.Random.Range(-halfWidth, halfWidth);
                float yPos = UnityEngine.Random.Range(-halfHeight, halfHeight);
                
                // Super small lines
                float width = UnityEngine.Random.Range(5f, 40f);
                float height = UnityEngine.Random.Range(1f, 3f);
                
                rt.anchoredPosition = new Vector2(xPos, yPos);
                rt.sizeDelta = new Vector2(width, height);
                
                // Making the noise white and translucent
                float alpha = UnityEngine.Random.Range(0.1f, 0.6f);
                img.color = new Color(1f, 1f, 1f, alpha);
            }
            else
            {
                rt.gameObject.SetActive(false);
            }
        }
    }

    private void CreateChromaticAberration()
    {
        if (applyGlitchToAllTexts && pauseCanvasObj != null)
        {
            // Automatically find all texts inside the pause menu
            glitchTextsToEffect = pauseCanvasObj.GetComponentsInChildren<TextMeshProUGUI>(true);
        }

        if (glitchTextsToEffect == null || glitchTextsToEffect.Length == 0) return;

        foreach (var txt in glitchTextsToEffect)
        {
            if (txt == null) continue;
            
            // Protection: we do not add the effect to already created clones
            if (txt.name.EndsWith("_GlitchClone")) continue;

            GlitchTextData data = new GlitchTextData();
            data.original = txt;

            // Clone the red channel
            data.redClone = CloneTextForGlitch(txt, new Color(1f, 0f, 0f, 0.8f));

            // Cloning the blue channel
            data.blueClone = CloneTextForGlitch(txt, new Color(0f, 0.5f, 1f, 0.8f));
            
            glitchDataList.Add(data);

            // So that the original text is on top of the clones
            txt.transform.SetAsLastSibling();
        }
    }

    private TextMeshProUGUI CloneTextForGlitch(TextMeshProUGUI original, Color glitchColor)
    {
        GameObject cloneObj = Instantiate(original.gameObject, original.transform.parent);
        cloneObj.name = original.name + "_GlitchClone";
        
        // We remove unnecessary scripts from the clone (for example buttons), leaving only the text
        var components = cloneObj.GetComponents<MonoBehaviour>();
        foreach (var comp in components)
        {
            if (!(comp is TextMeshProUGUI)) Destroy(comp);
        }

        TextMeshProUGUI cloneTxt = cloneObj.GetComponent<TextMeshProUGUI>();
        cloneTxt.color = glitchColor;
        
        // Disable Raycast so that clones do not interfere with pressing buttons
        cloneTxt.raycastTarget = false;
        
        return cloneTxt;
    }

    private void AnimateChromaticAberration()
    {
        bool shouldUpdateOffset = UnityEngine.Random.value > 0.8f;
        float currentRedOffset = 0f;
        float currentBlueOffset = 0f;

        if (shouldUpdateOffset)
        {
            currentRedOffset = UnityEngine.Random.Range(rgbOffset * 0.5f, rgbOffset * 1.5f);
            currentBlueOffset = UnityEngine.Random.Range(-rgbOffset * 1.5f, -rgbOffset * 0.5f);
        }

        foreach (var data in glitchDataList)
        {
            if (data.original == null) continue;

            // Synchronize the text (so that the time and date are updated for the clones)
            if (data.redClone.text != data.original.text) data.redClone.text = data.original.text;
            if (data.blueClone.text != data.original.text) data.blueClone.text = data.original.text;

            // Synchronizing the position relative to the original
            if (shouldUpdateOffset)
            {
                Vector2 basePos = data.original.rectTransform.anchoredPosition;
                data.redClone.rectTransform.anchoredPosition = basePos + new Vector2(currentRedOffset, 0);
                data.blueClone.rectTransform.anchoredPosition = basePos + new Vector2(currentBlueOffset, 0);
            }
        }
    }
}
