using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
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

    [Header("VHS Effect Settings")]
    public bool enableVHSEffect = true;
    public int scanlinesCount = 40; // Больше линий, так как они теперь как мелкий шум
    public float noiseSpeed = 0.05f; // Обновляем чуть быстрее для эффекта "шума"
    
    [Header("Text Glitch (Chromatic Aberration)")]
    [Tooltip("Добавьте сюда все тексты (PAUSE, RESUME, и т.д.), на которых хотите сделать RGB искажение")]
    public TextMeshProUGUI[] glitchTextsToEffect;
    public float rgbOffset = 4f; // На сколько пикселей разъезжаются цвета
    
    private bool isPaused = false;
    private float noiseTimer = 0f;
    private GameObject vhsContainer;
    private RectTransform vhsContainerRt;
    private RectTransform[] scanlineRects;
    private Image[] scanlineImages;

    // Храним данные для клонов
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
            // Убрана автоматическая генерация UI по вашей просьбе
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

        // Обновляем эффекты и время каждый кадр, пока игра на паузе
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
    }

    public void OnSettingsClicked()
    {
        Debug.Log("Settings opened");
        // Здесь будет вызов окна настроек
    }

    public void OnExitClicked()
    {
        Time.timeScale = 1f; // Возвращаем время перед выходом
        
        if (RadarManager.Instance != null)
        {
            RadarManager.Instance.SaveToGlobalManager();
        }
        
        // Сохраняем игру в файл перед выходом
        GameSaveManager.SaveGame();
        PlayerPrefs.Save();
        
        SceneManager.LoadScene("MainMenu");
    }

    private void UpdateDateAndTime()
    {
        if (dateText != null)
        {
            // Форматируем дату игры (в игре август, начинается с 19 числа)
            int day = 1;
            // Пытаемся получить текущий день из StoryManager
            day = StoryManager.currentDay; 
            
            string formattedDate = "AUG " + (18 + day).ToString("D2") + "\n2038";
            dateText.text = formattedDate;
        }

        if (timeText != null)
        {
            // Показываем реальное время компьютера (как на старых VHS записях)
            timeText.text = DateTime.Now.ToString("tt HH:mm").ToUpper();
        }
    }

    private void CreateVHSEffect()
    {
        // Создаем контейнер для помех прямо внутри вашего канваса
        vhsContainer = new GameObject("VHS_Noise_Lines");
        vhsContainer.transform.SetParent(pauseCanvasObj.transform, false);
        vhsContainer.transform.SetAsLastSibling(); // Помехи всегда будут поверх кнопок!

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
            // Теперь центрируем якорь, чтобы линии были не на весь экран, а кусочками
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
        
        // Используем unscaledDeltaTime, так как Time.timeScale = 0 на паузе
        noiseTimer += Time.unscaledDeltaTime;
        if (noiseTimer < noiseSpeed) return;
        
        noiseTimer = 0f; // Сбрасываем таймер
        
        float halfWidth = vhsContainerRt.rect.width * 0.5f;
        float halfHeight = vhsContainerRt.rect.height * 0.5f;
        
        for (int i = 0; i < scanlinesCount; i++)
        {
            RectTransform rt = scanlineRects[i];
            Image img = scanlineImages[i];

            // Первая линия (i == 0) всегда будет отвечать за ту самую редкую толстую полосу
            if (i == 0)
            {
                if (UnityEngine.Random.value > 0.95f) // Появляется еще реже (шанс 5% за кадр анимации)
                {
                    rt.gameObject.SetActive(true);
                    float yPos = UnityEngine.Random.Range(-halfHeight, halfHeight);
                    rt.anchoredPosition = new Vector2(0, yPos);
                    // Ширина контейнера плюс небольшой запас, чтобы точно перекрыть весь экран
                    rt.sizeDelta = new Vector2(halfWidth * 2f + 200f, UnityEngine.Random.Range(20f, 80f)); 
                    img.color = new Color(1f, 1f, 1f, UnityEngine.Random.Range(0.1f, 0.3f));
                }
                else
                {
                    rt.gameObject.SetActive(false);
                }
                continue;
            }

            // Все остальные линии — это постоянный мелкий белый шум
            if (UnityEngine.Random.value > 0.7f) // Шум тоже стал более редким (шанс 30%)
            {
                rt.gameObject.SetActive(true);
                
                float xPos = UnityEngine.Random.Range(-halfWidth, halfWidth);
                float yPos = UnityEngine.Random.Range(-halfHeight, halfHeight);
                
                // Супер маленькие линии
                float width = UnityEngine.Random.Range(5f, 40f);
                float height = UnityEngine.Random.Range(1f, 3f);
                
                rt.anchoredPosition = new Vector2(xPos, yPos);
                rt.sizeDelta = new Vector2(width, height);
                
                // Делаем шум белым и полупрозрачным
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
        if (glitchTextsToEffect == null || glitchTextsToEffect.Length == 0) return;

        foreach (var txt in glitchTextsToEffect)
        {
            if (txt == null) continue;

            GlitchTextData data = new GlitchTextData();
            data.original = txt;

            // Клонируем красный канал
            data.redClone = CloneTextForGlitch(txt, new Color(1f, 0f, 0f, 0.8f));

            // Клонируем синий канал
            data.blueClone = CloneTextForGlitch(txt, new Color(0f, 0.5f, 1f, 0.8f));
            
            glitchDataList.Add(data);

            // Чтобы оригинальный текст (белый/зеленый) был поверх клонов
            txt.transform.SetAsLastSibling();
        }
    }

    private TextMeshProUGUI CloneTextForGlitch(TextMeshProUGUI original, Color glitchColor)
    {
        GameObject cloneObj = Instantiate(original.gameObject, original.transform.parent);
        cloneObj.name = original.name + "_GlitchClone";
        
        // Удаляем лишние скрипты с клона (например кнопки), оставляем только текст
        var components = cloneObj.GetComponents<MonoBehaviour>();
        foreach (var comp in components)
        {
            if (!(comp is TextMeshProUGUI)) Destroy(comp);
        }

        TextMeshProUGUI cloneTxt = cloneObj.GetComponent<TextMeshProUGUI>();
        cloneTxt.color = glitchColor;
        
        // Отключаем Raycast, чтобы клоны не мешали нажимать на кнопки
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

            // Синхронизируем текст (чтобы время и дата обновлялись у клонов)
            if (data.redClone.text != data.original.text) data.redClone.text = data.original.text;
            if (data.blueClone.text != data.original.text) data.blueClone.text = data.original.text;

            // Синхронизируем позицию относительно оригинала
            if (shouldUpdateOffset)
            {
                Vector2 basePos = data.original.rectTransform.anchoredPosition;
                data.redClone.rectTransform.anchoredPosition = basePos + new Vector2(currentRedOffset, 0);
                data.blueClone.rectTransform.anchoredPosition = basePos + new Vector2(currentBlueOffset, 0);
            }
        }
    }
}
