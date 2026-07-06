using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Events;

/// <summary>
/// Facade for the Communications system. 
/// Retains all inspector references but delegates heavy logic to CommsUIController and CommsDocumentLogic.
/// </summary>
public class CommsManager : SingletonMB<CommsManager>
{
    [Header("Document Prefabs")]
    public GameObject manifestPrefab;
    public GameObject radarPrefab;
    public GameObject cheatSheetPrefab;
    public GameObject pilotReportPrefab;
    public GameObject defaultDocPrefab;

    [Header("UI General")]
    public Transform deskArea;
    public TextMeshProUGUI chatHistoryText;
    public ScrollRect chatScroll;
    public GameObject confrontButton;
    public GameObject endCommsButton;

    [Header("Folder UI")]
    public GameObject folderUI;
    public TextMeshProUGUI folderCallsignText;

    [Header("Single Question Button")]
    public GameObject askButton;
    public TextMeshProUGUI askButtonText;

    [Header("Decrypter Settings")]
    public GameObject decryptionMachineObj;
    public GameObject decryptionPaperPrefab;
    
    [Header("Teletype Settings")]
    public float typeDelay = 0.05f; 

    [Header("Audio Settings")]
    public AudioSource commsAudioSource;
    public AudioClip printerSound;
    public AudioClip typewriterSound;
    [Range(0f, 1f)] public float effectsVolume = 1f;

    [Header("Printer Animation")]
    public float paperScrollDelayMin = 0.05f;
    public float paperScrollDelayMax = 0.15f;
    public int paperScrollJerksMin = 4;
    public int paperScrollJerksMax = 8;

    [Header("Single Scene Return Mode (Optional)")]
    public Camera returnCamera;
    public GameObject returnScreenRoot;
    public GameObject currentCommsRoot;
    public UnityEvent onReturn;

    // --- Sub-Controllers ---
    private CommsUIController uiController;
    private CommsDocumentLogic docLogic;

    // --- State ---
    private FlightData currentData;
    private FlightInterrogationState currentState;
    private string pendingQuestionTopic = "";
    private bool isTyping = false;

    private FactScanner firstFactScanner;
    private int firstFactIndex = -1;

    private AudioSource dedicatedPrinterSource;
    private AudioSource dedicatedTypewriterSource;
    private float previousButtonVolume = 0.7f;

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this) return;

        dedicatedTypewriterSource = gameObject.AddComponent<AudioSource>();
        dedicatedTypewriterSource.playOnAwake = false;

        dedicatedPrinterSource = gameObject.AddComponent<AudioSource>();
        dedicatedPrinterSource.playOnAwake = false;

        if (confrontButton != null) confrontButton.SetActive(false);
        if (askButton != null) askButton.SetActive(false);

        if (chatHistoryText != null) chatHistoryText.alignment = TextAlignmentOptions.TopLeft;

        if (folderUI != null && folderUI.GetComponent<DraggablePaper>() == null)
            folderUI.AddComponent<DraggablePaper>();

        uiController = new CommsUIController(this);
        docLogic = new CommsDocumentLogic();
    }

    void OnEnable()
    {
        if (Instance != this) return;

        if (ButtonSoundManager.instance != null)
        {
            previousButtonVolume = ButtonSoundManager.instance.volume;
            ButtonSoundManager.instance.SetVolume(0f);
        }

        if (HintManager.Instance != null) HintManager.Instance.TriggerAskQuestionHint();

        if (chatScroll != null)
        {
            RectTransform rect = chatScroll.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(1f, 0f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(1f, 0.5f);
                rect.offsetMin = new Vector2(-559f, -861.645f);
                rect.offsetMax = new Vector2(-50f, 18.055f);
            }
            if (chatScroll.content != null && chatScroll.viewport != null)
            {
                float viewportHeight = chatScroll.viewport.rect.height;
                RectTransform contentRect = chatScroll.content;
                float startY = -viewportHeight + 150f;
                contentRect.anchoredPosition = new Vector2(contentRect.anchoredPosition.x, startY);
            }
        }

        RefreshData();
    }

    void OnDisable()
    {
        if (Instance != this) return;
        if (ButtonSoundManager.instance != null)
            ButtonSoundManager.instance.SetVolume(previousButtonVolume);
    }

    public void RefreshData()
    {
        if (uiController == null || docLogic == null) return;

        uiController.ClearDocuments();
        uiController.ClearChat();

        StopAllCoroutines();
        StopPrinterSound();
        StopTypewriterSound();
        isTyping = false;
        pendingQuestionTopic = "";
        
        docLogic.firstFactID = "";
        docLogic.currentLieTopic = "";
        firstFactScanner = null;
        firstFactIndex = -1;

        if (askButton != null) askButton.SetActive(false);
        if (confrontButton != null) confrontButton.SetActive(false);

        string callsign = RadioManager.activeCallsign;
        if (FlightDataManager.Instance != null && !string.IsNullOrEmpty(callsign))
        {
            currentData = FlightDataManager.Instance.savedFlights.Find(f => f.callsign == callsign);
            if (currentData != null)
            {
                currentState = FlightDataManager.Instance.GetOrCreateInterrogationState(callsign);

                docLogic.SetFlightData(currentData, currentState);
                uiController.GenerateDocuments(currentData, docLogic);
                uiController.SetupFolder(currentData, currentState);
                uiController.SetupDecryptionMachine(currentData, currentState, decryptionMachineObj);

                if (string.IsNullOrEmpty(currentState.chatHistory))
                {
                    StartCoroutine(Routine_StartChat());
                }
                else
                {
                    uiController.SetChatText(currentState.chatHistory);
                    uiController.ScrollToBottom(true);
                }
            }
        }
    }

    public void OnFolderTorn()
    {
        if (currentState != null) currentState.isFolderTorn = true;
        
        if (folderCallsignText != null) folderCallsignText.gameObject.SetActive(false);
        uiController.ShowDocuments();
    }

    public void SelectFact(string factID, string factText, FactScanner scanner, int linkIndex)
    {
        if (isTyping) return;

        if (factID.StartsWith("unlock_"))
        {
            string key = factID.Replace("unlock_", "");
            bool alreadyAsked = false;
            if (currentState != null)
            {
                if (key == "cargo" && currentState.askedCargo) alreadyAsked = true;
                if (key == "origin" && currentState.askedOrigin) alreadyAsked = true;
                if (key == "weight" && currentState.askedWeight) alreadyAsked = true;
                if (key == "speed" && currentState.askedSpeed) alreadyAsked = true;
            }
            if (alreadyAsked) return;

            scanner.HighlightLink(linkIndex, new Color32(20, 70, 180, 210));
            StartCoroutine(ResetColorRoutine(scanner, linkIndex, 0.5f));
            pendingQuestionTopic = key;
            askButtonText.text = $"ASK ABOUT {key.ToUpper()}";
            askButton.SetActive(true);
            return;
        }

        if (askButton != null) askButton.SetActive(false);

        if (docLogic.firstFactID == "")
        {
            docLogic.firstFactID = factID;
            firstFactScanner = scanner;
            firstFactIndex = linkIndex;
            scanner.HighlightLink(linkIndex, new Color32(200, 140, 15, 210));
        }
        else
        {
            CheckContradiction(factID, scanner, linkIndex);
        }
    }

    void CheckContradiction(string secondID, FactScanner secondScanner, int secondIndex)
    {
        bool isValid = docLogic.CheckContradiction(docLogic.firstFactID, secondID, out bool isLie, out string lieTopic);

        Color32 resColor = !isValid ? new Color32(180, 80, 15, 210) : (isLie ? new Color32(180, 25, 30, 210) : new Color32(20, 140, 50, 210));
        if (firstFactScanner != null) firstFactScanner.HighlightLink(firstFactIndex, resColor);
        if (secondScanner != null) secondScanner.HighlightLink(secondIndex, resColor);

        if (isLie)
        {
            docLogic.currentLieTopic = lieTopic;
            confrontButton.SetActive(true);
        }

        StartCoroutine(ResetColorRoutine(firstFactScanner, firstFactIndex, 2f));
        StartCoroutine(ResetColorRoutine(secondScanner, secondIndex, 2f));
        docLogic.firstFactID = "";
    }

    public void AskQuestion()
    {
        if (isTyping || string.IsNullOrEmpty(pendingQuestionTopic)) return;

        string question = "";
        string answer = "";

        switch (pendingQuestionTopic)
        {
            case "cargo":
                question = !string.IsNullOrEmpty(currentData.customQuestionCargo) ? currentData.customQuestionCargo : "State your cargo purpose.";
                answer = !string.IsNullOrEmpty(currentData.customAnswerCargo) ? currentData.customAnswerCargo : PilotDialogue.GetAnswer(currentData.personality, "cargo", docLogic.GetStatedCargo().ToUpper());
                break;
            case "origin":
                question = !string.IsNullOrEmpty(currentData.customQuestionOrigin) ? currentData.customQuestionOrigin : "Confirm your point of origin.";
                answer = !string.IsNullOrEmpty(currentData.customAnswerOrigin) ? currentData.customAnswerOrigin : PilotDialogue.GetAnswer(currentData.personality, "origin", docLogic.GetStatedOrigin());
                break;
            case "weight":
                question = !string.IsNullOrEmpty(currentData.customQuestionWeight) ? currentData.customQuestionWeight : "Report cargo weight.";
                answer = !string.IsNullOrEmpty(currentData.customAnswerWeight) ? currentData.customAnswerWeight : PilotDialogue.GetAnswer(currentData.personality, "weight", docLogic.GetStatedWeight());
                break;
            case "speed":
                question = !string.IsNullOrEmpty(currentData.customQuestionSpeed) ? currentData.customQuestionSpeed : "Confirm your current airspeed.";
                answer = !string.IsNullOrEmpty(currentData.customAnswerSpeed) ? currentData.customAnswerSpeed : PilotDialogue.GetAnswer(currentData.personality, "speed", docLogic.GetStatedSpeed());
                break;
        }

        string topic = pendingQuestionTopic;
        askButton.SetActive(false);
        pendingQuestionTopic = "";

        StartCoroutine(Routine_TypewriterChat(question, answer, topic));
    }

    public void OnConfront()
    {
        if (isTyping) return;

        if (docLogic.currentLieTopic == "cargo")
            currentState.isCargoKnown = true;

        string exp = PilotDialogue.GetConfrontResponse(currentData.personality);

        if (docLogic.currentLieTopic == "cargo" && !string.IsNullOrEmpty(currentData.explanationCargo)) exp = currentData.explanationCargo;
        else if (docLogic.currentLieTopic == "origin" && !string.IsNullOrEmpty(currentData.explanationOrigin)) exp = currentData.explanationOrigin;
        else if (docLogic.currentLieTopic == "weight" && !string.IsNullOrEmpty(currentData.explanationWeight)) exp = currentData.explanationWeight;
        else if (docLogic.currentLieTopic == "speed" && !string.IsNullOrEmpty(currentData.explanationSpeed)) exp = currentData.explanationSpeed;
        else if (!string.IsNullOrEmpty(currentData.customExplanation)) exp = currentData.customExplanation;

        confrontButton.SetActive(false);
        StartCoroutine(Routine_TypewriterChat("Explain this discrepancy.", exp, ""));
    }

    public void EndInterrogation()
    {
        if (currentState != null)
        {
            if (uiController.manifestDocInstance != null) currentState.manifestPos = uiController.manifestDocInstance.GetComponent<RectTransform>().anchoredPosition;
            if (uiController.radarDocInstance != null) currentState.radarPos = uiController.radarDocInstance.GetComponent<RectTransform>().anchoredPosition;
            if (uiController.cheatSheetDocInstance != null) currentState.cheatSheetPos = uiController.cheatSheetDocInstance.GetComponent<RectTransform>().anchoredPosition;
            if (uiController.pilotReportDoc != null) currentState.pilotReportPos = uiController.pilotReportDoc.GetComponent<RectTransform>().anchoredPosition;
        }
        if (RadarManager.Instance != null) RadarManager.Instance.SaveToGlobalManager();
        if (ButtonSoundManager.instance != null) ButtonSoundManager.instance.StopAllSounds();

        if (returnCamera != null || returnScreenRoot != null)
        {
            if (returnScreenRoot != null)
            {
                returnScreenRoot.SetActive(true);
                CanvasGroup cg = returnScreenRoot.GetComponent<CanvasGroup>();
                if (cg != null) { cg.alpha = 1f; cg.blocksRaycasts = true; cg.interactable = true; }
                UnityEngine.UI.GraphicRaycaster[] grs = returnScreenRoot.GetComponentsInChildren<UnityEngine.UI.GraphicRaycaster>(true);
                foreach (var gr in grs) gr.enabled = true;
            }
            if (returnCamera != null) returnCamera.gameObject.SetActive(true);
            if (currentCommsRoot != null) currentCommsRoot.SetActive(false);

            ZoomReturnManager zrm = FindAnyObjectByType<ZoomReturnManager>();
            if (zrm != null) zrm.TriggerReturnAnimation();
        }
        else
        {
            SceneManager.LoadScene("SampleScene");
        }
        
        onReturn?.Invoke();
    }

    void LateUpdate()
    {
        if (uiController != null) uiController.LateUpdateUI();
    }

    // --- Sound & UI Coroutines Exposed for UIController ---

    public void StartTypewriterSound()
    {
        if (typewriterSound == null || dedicatedTypewriterSource == null) return;
        dedicatedTypewriterSource.clip = typewriterSound;
        dedicatedTypewriterSource.volume = effectsVolume;
        dedicatedTypewriterSource.loop = true;
        if (!dedicatedTypewriterSource.isPlaying) dedicatedTypewriterSource.Play();
    }

    public void StopTypewriterSound()
    {
        if (dedicatedTypewriterSource != null) dedicatedTypewriterSource.Stop();
    }

    public void StartPrinterSound()
    {
        if (printerSound == null || dedicatedPrinterSource == null) return;
        dedicatedPrinterSource.clip = printerSound;
        dedicatedPrinterSource.volume = effectsVolume;
        dedicatedPrinterSource.loop = true;
        if (!dedicatedPrinterSource.isPlaying) dedicatedPrinterSource.Play();
    }

    public void StopPrinterSound()
    {
        if (dedicatedPrinterSource != null) dedicatedPrinterSource.Stop();
    }

    private IEnumerator ResetColorRoutine(FactScanner s, int i, float d)
    {
        yield return new WaitForSecondsRealtime(d);
        if (s != null && s.gameObject.activeInHierarchy)
        {
            try
            {
                Color32 originalColor = s.GetComponent<TextMeshProUGUI>().color;
                s.HighlightLink(i, originalColor);
            }
            catch (System.Exception) { }
        }
    }

    private IEnumerator Routine_StartChat()
    {
        isTyping = true;
        uiController.ClearChat();

        string prefix = $"<b>[{currentData.callsign}]:</b> ";
        string message = PilotDialogue.GetGreeting(currentData.personality, currentData.callsign);

        chatHistoryText.text += prefix + message + "\n\n";
        uiController.ScrollToBottom(true);
        yield return new WaitForSecondsRealtime(1f);

        if (currentState != null) currentState.chatHistory = chatHistoryText.text;
        isTyping = false;
    }

    private IEnumerator Routine_TypewriterChat(string question, string answer, string dataTopicToUpdate)
    {
        isTyping = true;

        chatHistoryText.text += $"<b>[YOU]:</b> {question}\n\n";
        uiController.ScrollToBottom(true);
        
        var (minDelay, maxDelay) = PilotDialogue.GetResponseDelay(currentData.personality);
        yield return new WaitForSecondsRealtime(Random.Range(minDelay, maxDelay));

        string prefix = $"<b>[{currentData.callsign}]:</b> ";
        chatHistoryText.text += prefix + answer + "\n\n";
        
        uiController.ScrollToBottom(true);
        yield return new WaitForSecondsRealtime(1f);

        if (currentState != null)
        {
            currentState.chatHistory = chatHistoryText.text;

            if (dataTopicToUpdate == "cargo") 
            { 
                currentState.askedCargo = true; 
                uiController.SetupDecryptionMachine(currentData, currentState, decryptionMachineObj);
            }
            else if (dataTopicToUpdate == "origin") { currentState.askedOrigin = true; }
            else if (dataTopicToUpdate == "weight") { currentState.askedWeight = true; }
            else if (dataTopicToUpdate == "speed") { currentState.askedSpeed = true; }
        }

        uiController.UpdatePilotReport(currentData, currentState, docLogic);
        isTyping = false;
    }
}
