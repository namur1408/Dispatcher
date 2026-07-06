using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class CRTNoiseEffect : MonoBehaviour
{
    [Header("Noise Settings")]
    [Tooltip("Количество полос шума на экране")]
    public int scanlinesCount = 40;
    public float noiseSpeed = 0.05f;

    [Tooltip("Включить принудительно, игнорируя сюжетные условия")]
    public bool forceEnable = false;

    [Header("UI Elements Glitch")]
    [Tooltip("Перетащите сюда элементы UI (Canvas, Panels, Texts), которые должны дергаться и искажаться")]
    public RectTransform[] elementsToGlitch;

    private GameObject container;
    private RectTransform[] scanlines;
    private Image[] lineImages;
    private float noiseTimer = 0f;

    private class GlitchData
    {
        public RectTransform rect;
        public Vector2 origPos;
        public Vector3 origScale;
        public CanvasGroup cg;
        
        public TextMeshProUGUI tmp;
        public string cleanText;
        public string scrambledText;
        public float textGlitchTimer;
        public bool isScrambled;
    }
    private List<GlitchData> glitchDataList = new List<GlitchData>();

    void Start()
    {
        RectTransform parentRt = GetComponent<RectTransform>();
        if (parentRt == null) 
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null) parentRt = canvas.GetComponent<RectTransform>();
        }
        
        if (parentRt == null) return;

        container = new GameObject("VHS_Noise_Lines");
        container.transform.SetParent(parentRt, false);
        container.transform.SetAsLastSibling();

        RectTransform containerRt = container.AddComponent<RectTransform>();
        containerRt.anchorMin = Vector2.zero;
        containerRt.anchorMax = Vector2.one;
        containerRt.offsetMin = Vector2.zero;
        containerRt.offsetMax = Vector2.zero;

        scanlines = new RectTransform[scanlinesCount];
        lineImages = new Image[scanlinesCount];

        for (int i = 0; i < scanlinesCount; i++)
        {
            GameObject lineObj = new GameObject("VHSLine");
            lineObj.transform.SetParent(container.transform, false);
            
            Image img = lineObj.AddComponent<Image>();
            img.raycastTarget = false;
            
            RectTransform lineRt = lineObj.GetComponent<RectTransform>();
            lineRt.anchorMin = new Vector2(0.5f, 0.5f);
            lineRt.anchorMax = new Vector2(0.5f, 0.5f);
            
            scanlines[i] = lineRt;
            lineImages[i] = img;
            
            lineObj.SetActive(false);
        }

        // Setup UI Elements Glitch
        if (elementsToGlitch != null)
        {
            foreach (var el in elementsToGlitch)
            {
                if (el == null) continue;
                GlitchData data = new GlitchData();
                data.rect = el;
                data.origPos = el.anchoredPosition;
                data.origScale = el.localScale;
                
                data.cg = el.GetComponent<CanvasGroup>();
                if (data.cg == null) data.cg = el.gameObject.AddComponent<CanvasGroup>();

                data.tmp = el.GetComponent<TextMeshProUGUI>();
                if (data.tmp != null) 
                {
                    data.cleanText = data.tmp.text;
                    data.scrambledText = "";
                }
                
                glitchDataList.Add(data);
            }
        }
    }

    void Update()
    {
        if (scanlines == null || container == null) return;

        bool isEmergency = PlayerPrefs.GetInt("BaseEmergencyEconomy", 0) == 1 && StoryManager.currentDay >= 2;
        
        if (!isEmergency && !forceEnable)
        {
            if (container.activeSelf) container.SetActive(false);
            RestoreAllElements();
            return;
        }
        else
        {
            if (!container.activeSelf) container.SetActive(true);
        }

        UpdateNoiseLines();
        UpdateUIGlitches();
    }

    private void UpdateNoiseLines()
    {
        noiseTimer += Time.unscaledDeltaTime;
        if (noiseTimer < noiseSpeed) return;
        
        noiseTimer = 0f; 
        
        for (int i = 0; i < scanlinesCount; i++)
        {
            RectTransform rt = scanlines[i];
            Image img = lineImages[i];

            if (i == 0)
            {
                if (Random.value > 0.95f) 
                {
                    rt.gameObject.SetActive(true);
                    float yPos = Random.Range(-540f, 540f);
                    rt.anchoredPosition = new Vector2(0, yPos);
                    rt.sizeDelta = new Vector2(10000f, Random.Range(20f, 80f)); 
                    img.color = new Color(1f, 1f, 1f, Random.Range(0.1f, 0.3f));
                }
                else
                {
                    rt.gameObject.SetActive(false);
                }
                continue;
            }

            if (Random.value > 0.7f) 
            {
                rt.gameObject.SetActive(true);
                
                float xPos = Random.Range(-960f, 960f);
                float yPos = Random.Range(-540f, 540f);
                
                float width = Random.Range(5f, 40f);
                float height = Random.Range(1f, 3f);
                
                rt.anchoredPosition = new Vector2(xPos, yPos);
                rt.sizeDelta = new Vector2(width, height);
                
                float alpha = Random.Range(0.1f, 0.6f);
                img.color = new Color(1f, 1f, 1f, alpha);
            }
            else
            {
                rt.gameObject.SetActive(false);
            }
        }
    }

    private void UpdateUIGlitches()
    {
        foreach (var data in glitchDataList)
        {
            // 1. Position, Scale, Alpha Twitch
            if (Random.value > 0.96f) // Little chance to twitch
            {
                data.rect.anchoredPosition = data.origPos + new Vector2(Random.Range(-15f, 15f), Random.Range(-5f, 5f));
                data.rect.localScale = new Vector3(data.origScale.x * Random.Range(0.95f, 1.05f), data.origScale.y * Random.Range(0.95f, 1.05f), 1f);
                data.cg.alpha = Random.Range(0.5f, 0.9f);
            }
            else if (Random.value > 0.8f) // Quick return to place
            {
                data.rect.anchoredPosition = data.origPos;
                data.rect.localScale = data.origScale;
                data.cg.alpha = 1f;
            }

            // 2. Text Scrambling (if there is text)
            if (data.tmp != null)
            {
                // We remember the text if it has changed externally (for example, the timer has been updated)
                if (data.tmp.text != data.cleanText && data.tmp.text != data.scrambledText)
                {
                    data.cleanText = data.tmp.text;
                }

                if (data.isScrambled)
                {
                    data.textGlitchTimer -= Time.unscaledDeltaTime;
                    if (data.textGlitchTimer <= 0)
                    {
                        data.tmp.text = data.cleanText;
                        data.isScrambled = false;
                        data.scrambledText = "";
                    }
                }
                else
                {
                    if (Random.value > 0.98f) // Chance to turn some letters into symbols
                    {
                        data.isScrambled = true;
                        data.textGlitchTimer = Random.Range(0.05f, 0.2f); // Symbols hang for a split second
                        
                        char[] chars = data.cleanText.ToCharArray();
                        string symbols = "!@#$%^&*()_+-=[]{}|;:,.<>?/";
                        for (int i = 0; i < chars.Length; i++)
                        {
                            if (char.IsWhiteSpace(chars[i])) continue;
                            if (Random.value > 0.8f) // 20% of characters in a line are broken
                            {
                                chars[i] = symbols[Random.Range(0, symbols.Length)];
                            }
                        }
                        data.scrambledText = new string(chars);
                        data.tmp.text = data.scrambledText;
                    }
                }
            }
        }
    }

    private void RestoreAllElements()
    {
        foreach (var data in glitchDataList)
        {
            data.rect.anchoredPosition = data.origPos;
            data.rect.localScale = data.origScale;
            data.cg.alpha = 1f;

            if (data.tmp != null && data.isScrambled)
            {
                data.tmp.text = data.cleanText;
                data.isScrambled = false;
                data.scrambledText = "";
            }
        }
    }
}
