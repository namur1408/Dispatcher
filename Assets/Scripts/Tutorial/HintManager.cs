using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class HintManager : MonoBehaviour
{
    private static HintManager _instance;
    public static HintManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject obj = new GameObject("HintManager");
                _instance = obj.AddComponent<HintManager>();
                DontDestroyOnLoad(obj);
            }
            return _instance;
        }
    }
    
    [Header("UI Settings")]
    public TMP_FontAsset customFont;
    
    private GameObject hintUI;
    private TextMeshProUGUI hintText;
    private TextMeshProUGUI triangleText;
    private CanvasGroup hintCanvasGroup;
    private Button dismissButton;

    private Coroutine queueRoutine;
    
    private class HintRequest
    {
        public string text;
        public float duration;
    }
    
    private Queue<HintRequest> hintQueue = new Queue<HintRequest>();
    private bool isShowingHint = false;
    private bool skipCurrentHint = false;

    // Hint states so they only show once
    public bool hintShown_Email = false;
    public bool hintShown_ContactPlane = false;
    public bool hintShown_AskQuestion = false;
    public bool hintShown_SelectRunway = false;
    public bool hintShown_UnloadPlane = false;

    void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        CreateHintUI();
    }

    void CreateHintUI()
    {
        hintUI = new GameObject("HintUI");
        hintUI.transform.SetParent(this.transform);
        
        Canvas canvas = hintUI.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30000;
        hintUI.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        hintCanvasGroup = hintUI.AddComponent<CanvasGroup>();
        hintCanvasGroup.alpha = 0f;

        GameObject bgObj = new GameObject("HintBG");
        bgObj.transform.SetParent(hintUI.transform, false);
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.5f);
        
        dismissButton = bgObj.AddComponent<Button>();
        dismissButton.onClick.AddListener(OnHintClicked);

        RectTransform bgRT = bgObj.GetComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0.5f, 0f);
        bgRT.anchorMax = new Vector2(0.5f, 0f);
        bgRT.pivot = new Vector2(0.5f, 0f);
        bgRT.anchoredPosition = new Vector2(0, 50);
        bgRT.sizeDelta = new Vector2(1400, 300);

        GameObject textObj = new GameObject("HintText");
        textObj.transform.SetParent(bgObj.transform, false);
        hintText = textObj.AddComponent<TextMeshProUGUI>();
        hintText.alignment = TextAlignmentOptions.Center;
        hintText.fontSize = 60;
        hintText.color = Color.white;
        hintText.enableWordWrapping = true;
        if (customFont != null) hintText.font = customFont;

        RectTransform textRT = textObj.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = new Vector2(20, 100);
        textRT.offsetMax = new Vector2(-20, -20);
        
        // Triangle
        GameObject triObj = new GameObject("TriangleText");
        triObj.transform.SetParent(bgObj.transform, false);
        triangleText = triObj.AddComponent<TextMeshProUGUI>();
        triangleText.alignment = TextAlignmentOptions.Center;
        triangleText.fontSize = 40;
        triangleText.color = Color.white;
        triangleText.text = "<b>V</b>";
        if (customFont != null) triangleText.font = customFont;
        
        RectTransform triRT = triObj.GetComponent<RectTransform>();
        triRT.anchorMin = new Vector2(0.5f, 0f);
        triRT.anchorMax = new Vector2(0.5f, 0f);
        triRT.pivot = new Vector2(0.5f, 0f);
        triRT.anchoredPosition = new Vector2(0, 15);
        triRT.sizeDelta = new Vector2(100, 50);
        triRT.localScale = new Vector3(1.5f, 0.8f, 1f);
        
        StartCoroutine(AnimateTriangle());
        hintUI.SetActive(false); // Hide completely by default
    }
    
    private void OnHintClicked()
    {
        if (isShowingHint)
        {
            skipCurrentHint = true;
        }
    }
    
    private IEnumerator AnimateTriangle()
    {
        RectTransform triRT = triangleText.GetComponent<RectTransform>();
        float startY = 15f;
        while (true)
        {
            float yOffset = Mathf.Sin(Time.unscaledTime * 6f) * 10f;
            triRT.anchoredPosition = new Vector2(0, startY + yOffset);
            yield return null;
        }
    }

    public void ShowHint(string text, float duration = 5f)
    {
        if (StoryManager.currentDay != 1) return; // Only show on Day 1
        
        hintQueue.Enqueue(new HintRequest { text = text, duration = duration });
        
        if (!isShowingHint)
        {
            queueRoutine = StartCoroutine(ProcessHintQueue());
        }
    }

    private IEnumerator ProcessHintQueue()
    {
        isShowingHint = true;
        hintUI.SetActive(true);
        
        while (hintQueue.Count > 0)
        {
            HintRequest request = hintQueue.Dequeue();
            hintText.text = request.text;
            skipCurrentHint = false;
            
            // Fade in
            float t = 0;
            while (t < 0.3f)
            {
                t += Time.deltaTime;
                hintCanvasGroup.alpha = Mathf.Clamp01(t / 0.3f);
                yield return null;
            }
            hintCanvasGroup.alpha = 1f;

            // Wait for duration or click
            float timer = 0;
            while (timer < request.duration && !skipCurrentHint)
            {
                timer += Time.deltaTime;
                yield return null;
            }

            // Fade out
            t = 0;
            while (t < 0.3f)
            {
                t += Time.deltaTime;
                hintCanvasGroup.alpha = 1f - Mathf.Clamp01(t / 0.3f);
                yield return null;
            }
            hintCanvasGroup.alpha = 0f;
            
            // Small gap between hints
            yield return new WaitForSeconds(0.2f);
        }
        
        isShowingHint = false;
        hintUI.SetActive(false);
    }

    // Specific Triggers
    public void TriggerEmailHint()
    {
        if (!hintShown_Email)
        {
            ShowHint("HINT: Before starting the shift, check your Email on the Terminal to read the daily directives.", 8f);
            hintShown_Email = true;
        }
    }

    public void TriggerContactPlaneHint()
    {
        if (!hintShown_ContactPlane)
        {
            ShowHint("HINT: To contact a plane, click it on the Radar, then exit and click the blinking Radio.", 8f);
            hintShown_ContactPlane = true;
        }
    }

    public void TriggerAskQuestionHint()
    {
        if (!hintShown_AskQuestion)
        {
            ShowHint("HINT: Click on any field inside the Pilot's statement paper (e.g. Cargo, Weight) to ask a question about it.", 8f);
            hintShown_AskQuestion = true;
        }
    }

    public void TriggerSelectRunwayHint()
    {
        if (!hintShown_SelectRunway)
        {
            ShowHint("HINT: You allowed landing. Now go to the Terminal and select an available runway for this flight.", 8f);
            hintShown_SelectRunway = true;
        }
    }

    public void TriggerUnloadPlaneHint()
    {
        if (!hintShown_UnloadPlane)
        {
            ShowHint("HINT: Plane landed! Open Terminal -> Resources, select the plane and click [UNLOAD] to free the runway.", 8f);
            hintShown_UnloadPlane = true;
        }
    }
}
