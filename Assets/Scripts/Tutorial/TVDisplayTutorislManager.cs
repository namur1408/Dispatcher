using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TVDisplayTutorialManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject subtitlePanel;
    public TextMeshProUGUI subtitleText;

    [Header("Highlights")]
    public GameObject listHighlight;
    public GameObject buttonsHighlight;

    [Header("DispathcerPanel (ApproveButton + DenyButton + SelectedLabel)")]
    public GameObject actionButtonsContainer;

    [Header("TVDisplayInfo (root-объект 'GameObject' в сцене)")]
    public TVDisplayInfo tvDisplayInfo;

    [Header("Mentor Settings")]
    public Color mentorNormalColor = Color.green;

    [Header("Timing Settings")]
    public float typeSpeed    = 0.04f;
    public float msgWaitTime  = 4f;
    public float initialDelay = 1.2f;

    // ── State ──────────────────────────────────────────────────────────────
    public static bool isTVTutorialCompleted = false;
    private bool skipRequested   = false;
    private bool decisionWasMade = false;

    // FIX BUG 3: track whether subtitle is in "clickable" mode or "passthrough" mode
    private Button subtitleButton = null;

    // ── Messages ───────────────────────────────────────────────────────────
    private string msgList    = "This is your FLIGHT REGISTER.\nAll aircraft in your sector appear here.";
    private string msgSelect  = "Here is the aircraft data.\nCheck callsign, country and cargo before deciding.";
    private string msgButtons = "Press ALLOW to clear the aircraft to land.\nPress DENY to turn it away. Decisions are PERMANENT.";
    private string msgDone    = "Decision logged. Repeat for every APPROACHING flight.\nYou are cleared to operate. Good luck, Dispatcher.";

    // ── Awake ──────────────────────────────────────────────────────────────
    void Awake()
    {
        SetupSubtitleButton();
    }

    // FIX BUG 1: setup button separately so it can be called safely
    void SetupSubtitleButton()
    {
        if (subtitlePanel == null) return;

        // Reuse existing Button if already there
        subtitleButton = subtitlePanel.GetComponent<Button>();
        if (subtitleButton != null)
        {
            subtitleButton.onClick.AddListener(OnDialogueClicked);
            return;
        }

        // Need Image for raycast to work
        Image img = subtitlePanel.GetComponent<Image>();
        if (img == null) img = subtitlePanel.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0f); // fully transparent
        img.raycastTarget = true;

        subtitleButton = subtitlePanel.AddComponent<Button>();
        subtitleButton.transition = Selectable.Transition.None;
        subtitleButton.onClick.AddListener(OnDialogueClicked);
    }

    // FIX BUG 3: toggle whether subtitle panel intercepts clicks
    void SetSubtitleClickable(bool clickable)
    {
        if (subtitleButton == null) return;
        subtitleButton.interactable = clickable;

        // Also toggle raycastTarget on the Image so clicks pass through to list
        Image img = subtitlePanel.GetComponent<Image>();
        if (img != null) img.raycastTarget = clickable;
    }

    // ── Start ──────────────────────────────────────────────────────────────
    void Start()
    {
        if (listHighlight    != null) listHighlight.SetActive(false);
        if (buttonsHighlight != null) buttonsHighlight.SetActive(false);
        if (subtitlePanel    != null) subtitlePanel.SetActive(false);

        if (actionButtonsContainer != null)
            actionButtonsContainer.SetActive(isTVTutorialCompleted);

        if (isTVTutorialCompleted) return;

        // Subscribe to decision buttons
        if (tvDisplayInfo != null)
        {
            if (tvDisplayInfo.approveButton != null)
                tvDisplayInfo.approveButton.onClick.AddListener(() => decisionWasMade = true);
            if (tvDisplayInfo.denyButton != null)
                tvDisplayInfo.denyButton.onClick.AddListener(() => decisionWasMade = true);
        }

        StartCoroutine(TVTutorialSequence());
    }

    // ── Main Sequence ──────────────────────────────────────────────────────
    IEnumerator TVTutorialSequence()
    {
        yield return new WaitForSecondsRealtime(initialDelay);

        // ━━ STEP 1: Show flight list message ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // FIX BUG 3: disable click on subtitle so clicks reach the flight list
        Time.timeScale = 0f;
        if (listHighlight != null) listHighlight.SetActive(true);
        subtitlePanel.SetActive(true);
        subtitleText.color = mentorNormalColor;

        SetSubtitleClickable(false); // clicks pass through to flight list
        yield return StartCoroutine(TypeText(msgList));
        // Panel stays visible — player must click a flight to advance

        // Unfreeze so player can click the list
        Time.timeScale = 1f;

        // Wait for flight selection
        yield return new WaitUntil(() => tvDisplayInfo != null && tvDisplayInfo.SelectedIndex >= 0);

        subtitlePanel.SetActive(false);
        if (listHighlight != null) listHighlight.SetActive(false);

        // ━━ STEP 2: Explain aircraft info ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        yield return new WaitForSecondsRealtime(0.4f);

        Time.timeScale = 0f;
        subtitlePanel.SetActive(true);
        subtitleText.color = mentorNormalColor;
        SetSubtitleClickable(true); // player can click to skip
        yield return StartCoroutine(TypeText(msgSelect));
        yield return StartCoroutine(WaitWithSkip(msgWaitTime));
        subtitlePanel.SetActive(false);
        Time.timeScale = 1f;

        // ━━ STEP 3: Show ALLOW / DENY buttons ━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        yield return new WaitForSecondsRealtime(0.3f);

        Time.timeScale = 0f;
        if (actionButtonsContainer != null) actionButtonsContainer.SetActive(true);
        if (buttonsHighlight       != null) buttonsHighlight.SetActive(true);
        subtitlePanel.SetActive(true);
        subtitleText.color = mentorNormalColor;
        SetSubtitleClickable(false); // clicks must reach ALLOW/DENY buttons
        yield return StartCoroutine(TypeText(msgButtons));

        decisionWasMade = false;
        Time.timeScale = 1f;

        // Wait for ALLOW or DENY
        yield return new WaitUntil(() => decisionWasMade);

        subtitlePanel.SetActive(false);
        if (buttonsHighlight != null) buttonsHighlight.SetActive(false);

        // ━━ STEP 4: Done ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        yield return new WaitForSecondsRealtime(0.3f);

        Time.timeScale = 0f;
        subtitlePanel.SetActive(true);
        subtitleText.color = mentorNormalColor;
        SetSubtitleClickable(true); // player can click to skip final message
        yield return StartCoroutine(TypeText(msgDone));
        yield return StartCoroutine(WaitWithSkip(msgWaitTime));
        subtitlePanel.SetActive(false);
        Time.timeScale = 1f;

        isTVTutorialCompleted = true;
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    IEnumerator WaitWithSkip(float time)
    {
        float timer = time;
        skipRequested = false;
        while (timer > 0f && !skipRequested)
        {
            timer -= Time.unscaledDeltaTime;
            yield return null;
        }
        skipRequested = false;
    }

    IEnumerator TypeText(string textToType)
    {
        // FIX BUG 2: do NOT reset skipRequested at start — player may have
        // already clicked before typing began. Only clear it before we start.
        bool wasSkipped = skipRequested;
        skipRequested = false;

        subtitleText.text = "";

        if (wasSkipped)
        {
            // Already clicked before typing started — show full text immediately
            subtitleText.text = textToType;
            yield break;
        }

        foreach (char c in textToType.ToCharArray())
        {
            if (skipRequested)
            {
                subtitleText.text = textToType;
                skipRequested = false;
                yield break;
            }
            subtitleText.text += c;
            yield return new WaitForSecondsRealtime(typeSpeed);
        }
        // Do NOT reset skipRequested here — let WaitWithSkip read it
    }

    // ── Called by Button on subtitlePanel ──────────────────────────────────
    public void OnDialogueClicked() { skipRequested = true; }
}