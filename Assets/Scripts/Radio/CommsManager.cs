using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CommsManager : MonoBehaviour
{
    public static CommsManager Instance;

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

    private FlightData currentData;
    private string firstFactID = "";
    private FactScanner firstFactScanner;
    private int firstFactIndex = -1;
    private string pendingQuestionTopic = "";

    private bool askedCargo = false;
    private bool askedOrigin = false;
    private bool askedWeight = false;
    private bool askedSpeed = false;

    private string currentLieTopic = "";
    private DocumentUI pilotReportDoc;

    private bool isTyping = false;
    private Coroutine scrollCoroutine;
    private bool isAnimatingPaper = false;

    private GameObject manifestDocInstance;
    private GameObject radarDocInstance;
    private GameObject cheatSheetDocInstance;

    private AudioSource dedicatedPrinterSource;
    private AudioSource dedicatedTypewriterSource;

    private void PlaySound(AudioClip clip)
    {
        if (commsAudioSource != null && clip != null)
        {
            commsAudioSource.PlayOneShot(clip, effectsVolume);
        }
    }

    private void StartTypewriterSound()
    {
        if (typewriterSound == null) return;
        
        if (dedicatedTypewriterSource == null)
        {
            dedicatedTypewriterSource = gameObject.AddComponent<AudioSource>();
            dedicatedTypewriterSource.playOnAwake = false;
        }
        
        dedicatedTypewriterSource.clip = typewriterSound;
        dedicatedTypewriterSource.volume = effectsVolume;
        dedicatedTypewriterSource.loop = true;
        dedicatedTypewriterSource.Play();
    }

    private void StopTypewriterSound()
    {
        if (dedicatedTypewriterSource != null)
        {
            dedicatedTypewriterSource.Stop();
        }
    }

    private void StartPrinterSound()
    {
        if (printerSound == null) return;
        
        if (dedicatedPrinterSource == null)
        {
            dedicatedPrinterSource = gameObject.AddComponent<AudioSource>();
            dedicatedPrinterSource.playOnAwake = false;
        }
        
        dedicatedPrinterSource.clip = printerSound;
        dedicatedPrinterSource.volume = effectsVolume;
        dedicatedPrinterSource.loop = true;
        dedicatedPrinterSource.Play();
    }

    private void StopPrinterSound()
    {
        if (dedicatedPrinterSource != null)
        {
            dedicatedPrinterSource.Stop();
        }
    }

    void Awake()
    {
        Instance = this;
        if (confrontButton != null) confrontButton.SetActive(false);
        if (askButton != null) askButton.SetActive(false);

        if (chatHistoryText != null)
        {
            chatHistoryText.alignment = TextAlignmentOptions.TopLeft;
        }

        if (folderUI != null && folderUI.GetComponent<DraggablePaper>() == null)
        {
            folderUI.AddComponent<DraggablePaper>();
        }
    }

    private void ConfigureChatScrollView()
    {
        if (chatScroll == null) return;

        RectTransform rect = chatScroll.GetComponent<RectTransform>();
        if (rect != null)
        {
            // Lock the anchors to Right-Stretch:
            // - Horizontal: Anchored to the right of the screen (Right = -50f, Width = 509f, Left = -559f)
            // - Vertical: Stretches vertically with Top = 18.055f and Bottom = -861.645f offsets to match teletype height
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 0.5f);
            
            rect.offsetMin = new Vector2(-559f, -861.645f);
            rect.offsetMax = new Vector2(-50f, 18.055f);
            
            Debug.Log($"[UI] Programmatically locked Chat Scroll View to a responsive Right-Stretch layout.");
        }
    }

    void Start()
    {
        ConfigureChatScrollView();
        
        Canvas.ForceUpdateCanvases();

        // Enforce the starting paper position where only the top 150 pixels stick out from the bottom
        if (chatScroll != null && chatScroll.content != null && chatScroll.viewport != null)
        {
            float viewportHeight = chatScroll.viewport.rect.height;
            RectTransform contentRect = chatScroll.content;
            float startY = -viewportHeight + 150f;
            contentRect.anchoredPosition = new Vector2(contentRect.anchoredPosition.x, startY);
        }

        string callsign = RadioManager.activeCallsign;
        if (FlightDataManager.Instance != null)
        {
            currentData = FlightDataManager.Instance.savedFlights.Find(f => f.callsign == callsign);
            if (currentData != null)
            {
                askedCargo = currentData.askedCargo;
                askedOrigin = currentData.askedOrigin;
                askedWeight = currentData.askedWeight;
                askedSpeed = currentData.askedSpeed;

                GenerateDocuments();

                if (string.IsNullOrEmpty(currentData.chatHistory))
                {
                    StartCoroutine(Routine_StartChat());
                }
                else
                {
                    chatHistoryText.text = currentData.chatHistory;
                    ScrollToBottom();
                }
            }
        }
    }

    string GetStatedOrigin() => !string.IsNullOrEmpty(currentData.spokenOrigin) ? currentData.spokenOrigin : currentData.manifestOrigin;
    string GetStatedCargo() => !string.IsNullOrEmpty(currentData.spokenCargo) ? currentData.spokenCargo : currentData.manifestCargo;
    string GetStatedWeight() => !string.IsNullOrEmpty(currentData.spokenWeight) ? currentData.spokenWeight : currentData.manifestCargoAmount.ToString();
    string GetStatedSpeed() => !string.IsNullOrEmpty(currentData.spokenSpeed) ? currentData.spokenSpeed : (currentData.speed * 10f).ToString();

    void GenerateDocuments()
    {
        string highlightStart = (TutorialManager.isTutorialActive && RadioTutorialManager.Instance != null && !RadioTutorialManager.isRadioTutorialCompleted) ? "<color=yellow>" : "";
        string highlightEnd = (TutorialManager.isTutorialActive && RadioTutorialManager.Instance != null && !RadioTutorialManager.isRadioTutorialCompleted) ? "</color>" : "";

        string manifestText = $"<align=center><b>FLIGHT MANIFEST</b></align>\n\n" +
                              $"<b>FLIGHT:</b> {currentData.callsign}\n" +
                              $"<link=\"unlock_origin\"><b>ORIGIN:</b></link> <link=\"man_origin\">{currentData.manifestOrigin}</link>\n" +
                              $"<b>CARGO:</b> {highlightStart}<link=\"man_cargo\">{currentData.manifestCargo.ToUpper()}</link>{highlightEnd}\n" +
                              $"<link=\"unlock_weight\"><b>WEIGHT:</b></link> <link=\"man_weight\">{currentData.manifestCargoAmount} UNITS</link>\n";

        manifestDocInstance = SpawnDocument(manifestPrefab, manifestText, currentData.manifestPos, true);

        string radarLogText = $"<align=center><b>RADAR REPORT</b></align>\n\n" +
                              $"<link=\"unlock_speed\"><b>SPEED:</b></link> <link=\"rad_speed\">{currentData.speed * 10f} KTS</link>\n" +
                              $"<b>CLASS:</b> {GetPlaneClass()}\n" +
                              $"{highlightStart}<link=\"unlock_cargo\"><b>SENSOR:</b></link>{highlightEnd} UNKNOWN\n";
        radarDocInstance = SpawnDocument(radarPrefab, radarLogText, currentData.radarPos, false);

        if (!TutorialManager.isTutorialActive)
        {
            string cheatSheetText = $"<size=80%><b>QUICK REF:</b>\n\n" +
                                    $"<b>[GE] Heavy Cargo</b>\n" +
                                    $"<link=\"rule_ge_speed\">Speed: < 850 KTS</link>\n" +
                                    $"<link=\"rule_ge_weight\">Max Wt: 500 UNITS</link>\n\n" +
                                    $"<b>[TR] Passenger</b>\n" +
                                    $"<link=\"rule_tr_cargo\">Cargo: PEOPLE ONLY</link>\n" +
                                    $"<link=\"rule_tr_speed\">Speed: 700-780 KTS</link>\n\n" +
                                    $"<b>[QY] Light Courier</b>\n" +
                                    $"<link=\"rule_qy_speed\">Speed: > 800 KTS</link>\n" +
                                    $"<link=\"rule_qy_weight\">Max Wt: 50 UNITS</link>\n</size>";
            cheatSheetDocInstance = SpawnDocument(cheatSheetPrefab, cheatSheetText, currentData.cheatSheetPos, false);
        }

        GameObject reportObj = Instantiate(pilotReportPrefab != null ? pilotReportPrefab : defaultDocPrefab, deskArea);
        reportObj.GetComponent<RectTransform>().anchoredPosition = currentData.pilotReportPos;

        reportObj.transform.rotation = Quaternion.Euler(0, 0, Random.Range(-2f, 2f));
        pilotReportDoc = reportObj.GetComponent<DocumentUI>();
        UpdatePilotReport();

        if (folderUI != null)
        {
            folderUI.SetActive(!currentData.isFolderTorn);
            if (folderCallsignText != null)
            {
                folderCallsignText.gameObject.SetActive(!currentData.isFolderTorn);
                folderCallsignText.text = currentData.callsign;
            }
        }
    }

    void UpdatePilotReport()
    {
        string highlightStart = (TutorialManager.isTutorialActive && RadioTutorialManager.Instance != null && !RadioTutorialManager.isRadioTutorialCompleted) ? "<color=yellow>" : "";
        string highlightEnd = (TutorialManager.isTutorialActive && RadioTutorialManager.Instance != null && !RadioTutorialManager.isRadioTutorialCompleted) ? "</color>" : "";

        string reportText = $"<align=center><b>PILOT'S STATEMENT</b></align>\n\n";
        if (!askedCargo && !askedOrigin && !askedWeight && !askedSpeed)
        {
            reportText += "<i>No interrogation data.</i>";
        }
        else
        {
            if (askedOrigin) reportText += $"<b>ORIGIN:</b> <link=\"rep_origin\">{GetStatedOrigin()}</link>\n";
            if (askedCargo) reportText += $"<b>CARGO:</b> {highlightStart}<link=\"rep_cargo\">{GetStatedCargo().ToUpper()}</link>{highlightEnd}\n";
            if (askedWeight) reportText += $"<b>WEIGHT:</b> <link=\"rep_weight\">{GetStatedWeight()} UNITS</link>\n";
            if (askedSpeed) reportText += $"<b>SPEED:</b> <link=\"rep_speed\">{GetStatedSpeed()} KTS</link>\n";
        }
        pilotReportDoc.SetContent(reportText);
    }

    public void SelectFact(string factID, string factText, FactScanner scanner, int linkIndex)
    {
        if (isTyping) return;

        if (RadioTutorialManager.Instance != null && !RadioTutorialManager.isRadioTutorialCompleted)
        {
            RadioTutorialManager.Instance.NotifyDocumentClicked();
        }

        if (factID.StartsWith("unlock_"))
        {
            string key = factID.Replace("unlock_", "");
            bool alreadyAsked = false;
            if (key == "cargo" && askedCargo) alreadyAsked = true;
            if (key == "origin" && askedOrigin) alreadyAsked = true;
            if (key == "weight" && askedWeight) alreadyAsked = true;
            if (key == "speed" && askedSpeed) alreadyAsked = true;
            if (alreadyAsked) return;

            scanner.HighlightLink(linkIndex, new Color32(0, 150, 255, 255));
            StartCoroutine(ResetColorRoutine(scanner, linkIndex, 0.5f));
            pendingQuestionTopic = key;
            askButtonText.text = $"ASK ABOUT {key.ToUpper()}";
            askButton.SetActive(true);
            return;
        }

        if (askButton != null) askButton.SetActive(false);

        if (firstFactID == "")
        {
            firstFactID = factID;
            firstFactScanner = scanner;
            firstFactIndex = linkIndex;
            scanner.HighlightLink(linkIndex, new Color32(255, 200, 0, 255));
        }
        else
        {
            CheckContradiction(factID, scanner, linkIndex);
        }
    }

    public void AskQuestion()
    {
        if (isTyping || string.IsNullOrEmpty(pendingQuestionTopic)) return;

        string question = "";
        string answer = "";

        switch (pendingQuestionTopic)
        {
            case "cargo":
                question = !string.IsNullOrEmpty(currentData.customQuestionCargo)
                    ? currentData.customQuestionCargo
                    : "State your cargo purpose.";
                answer = !string.IsNullOrEmpty(currentData.customAnswerCargo)
                    ? currentData.customAnswerCargo
                    : $"We are transporting {GetStatedCargo().ToUpper()}.";
                break;
            case "origin":
                question = !string.IsNullOrEmpty(currentData.customQuestionOrigin)
                    ? currentData.customQuestionOrigin
                    : "Confirm your point of origin.";
                answer = !string.IsNullOrEmpty(currentData.customAnswerOrigin)
                    ? currentData.customAnswerOrigin
                    : $"Flight originated from {GetStatedOrigin()}.";
                break;
            case "weight":
                question = !string.IsNullOrEmpty(currentData.customQuestionWeight)
                    ? currentData.customQuestionWeight
                    : "Report cargo weight.";
                answer = !string.IsNullOrEmpty(currentData.customAnswerWeight)
                    ? currentData.customAnswerWeight
                    : $"Manifest states {GetStatedWeight()} UNITS.";
                break;
            case "speed":
                question = !string.IsNullOrEmpty(currentData.customQuestionSpeed)
                    ? currentData.customQuestionSpeed
                    : "Confirm your current airspeed.";
                answer = !string.IsNullOrEmpty(currentData.customAnswerSpeed)
                    ? currentData.customAnswerSpeed
                    : $"Instruments show {GetStatedSpeed()} KTS.";
                break;
        }

        string topic = pendingQuestionTopic;
        askButton.SetActive(false);
        pendingQuestionTopic = "";

        if (RadioTutorialManager.Instance != null && !RadioTutorialManager.isRadioTutorialCompleted)
        {
            RadioTutorialManager.Instance.NotifyQuestionAsked();
        }

        StartCoroutine(Routine_TypewriterChat(question, answer, topic));
    }

    void CheckContradiction(string secondID, FactScanner secondScanner, int secondIndex)
    {
        if (RadioTutorialManager.Instance != null && !RadioTutorialManager.isRadioTutorialCompleted)
        {
            RadioTutorialManager.Instance.NotifyFactsCompared();
        }

        bool isValid = false;
        bool isLie = false;

        if (firstFactID.StartsWith("rule_") || secondID.StartsWith("rule_"))
        {
            string rule = firstFactID.StartsWith("rule_") ? firstFactID : secondID;
            string fact = firstFactID.StartsWith("rule_") ? secondID : firstFactID;

            if (rule.Contains("_ge_") && currentData.callsign.StartsWith("GE"))
            {
                if (rule == "rule_ge_speed" && (fact == "rad_speed" || fact == "rep_speed"))
                {
                    isValid = true;
                    float speedToCheck = fact == "rad_speed" ? currentData.speed * 10f : float.Parse(GetStatedSpeed());
                    if (speedToCheck >= 850f) isLie = true;
                }
                else if (rule == "rule_ge_weight" && (fact == "man_weight" || fact == "rep_weight"))
                {
                    isValid = true;
                    float weightToCheck = fact == "man_weight" ? currentData.manifestCargoAmount : float.Parse(GetStatedWeight());
                    if (weightToCheck > 500) isLie = true;
                }
            }
            else if (rule.Contains("_tr_") && currentData.callsign.StartsWith("TR"))
            {
                if (rule == "rule_tr_cargo" && (fact == "man_cargo" || fact == "rep_cargo"))
                {
                    isValid = true;
                    string cargoToCheck = fact == "man_cargo" ? currentData.manifestCargo : GetStatedCargo();
                    if (cargoToCheck != "People") isLie = true;
                }
                else if (rule == "rule_tr_speed" && (fact == "rad_speed" || fact == "rep_speed"))
                {
                    isValid = true;
                    float speedToCheck = fact == "rad_speed" ? currentData.speed * 10f : float.Parse(GetStatedSpeed());
                    if (speedToCheck < 700f || speedToCheck > 780f) isLie = true;
                }
            }
            else if (rule.Contains("_qy_") && currentData.callsign.StartsWith("QY"))
            {
                if (rule == "rule_qy_speed" && (fact == "rad_speed" || fact == "rep_speed"))
                {
                    isValid = true;
                    float speedToCheck = fact == "rad_speed" ? currentData.speed * 10f : float.Parse(GetStatedSpeed());
                    if (speedToCheck <= 800f) isLie = true;
                }
                else if (rule == "rule_qy_weight" && (fact == "man_weight" || fact == "rep_weight"))
                {
                    isValid = true;
                    float weightToCheck = fact == "man_weight" ? currentData.manifestCargoAmount : float.Parse(GetStatedWeight());
                    if (weightToCheck > 50) isLie = true;
                }
            }
        }
        else
        {
            if (CheckPair(firstFactID, secondID, "rad_class", "man_cargo") || CheckPair(firstFactID, secondID, "rad_class", "rep_cargo"))
            {
                isValid = true;
                string cargo = firstFactID.Contains("cargo") ?
                    (firstFactID == "man_cargo" ? currentData.manifestCargo : GetStatedCargo()) :
                    (secondID == "man_cargo" ? currentData.manifestCargo : GetStatedCargo());

                if (currentData.callsign.StartsWith("TR") && cargo != "People") isLie = true;
                if (currentData.callsign.StartsWith("GE") && cargo == "People") isLie = true;
            }
            else if (CheckPair(firstFactID, secondID, "rad_sensor", "man_cargo") || CheckPair(firstFactID, secondID, "rad_sensor", "rep_cargo"))
            {
                isValid = true;
                string cargoToCompare = (secondID == "man_cargo" || firstFactID == "man_cargo") ? currentData.manifestCargo : GetStatedCargo();
                isLie = (currentData.cargo.ToUpper() != cargoToCompare.ToUpper());
            }
            else if (CheckPair(firstFactID, secondID, "man_cargo", "rep_cargo"))
            {
                isValid = true; isLie = (currentData.manifestCargo.ToUpper() != GetStatedCargo().ToUpper());
            }
            else if (CheckPair(firstFactID, secondID, "man_origin", "rep_origin"))
            {
                isValid = true; isLie = (currentData.manifestOrigin.ToUpper() != GetStatedOrigin().ToUpper());
            }
            else if (CheckPair(firstFactID, secondID, "man_weight", "rep_weight"))
            {
                isValid = true; isLie = (currentData.manifestCargoAmount.ToString() != GetStatedWeight());
            }
            else if (CheckPair(firstFactID, secondID, "rad_speed", "rep_speed"))
            {
                isValid = true; isLie = ((currentData.speed * 10f).ToString() != GetStatedSpeed());
            }
        }

        Color32 resColor = !isValid ? new Color32(255, 140, 0, 255) : (isLie ? new Color32(255, 0, 0, 255) : new Color32(0, 255, 0, 255));
        firstFactScanner.HighlightLink(firstFactIndex, resColor);
        secondScanner.HighlightLink(secondIndex, resColor);

        if (isLie)
        {
            if (firstFactID.Contains("cargo") || secondID.Contains("cargo") || firstFactID.Contains("class") || secondID.Contains("class")) currentLieTopic = "cargo";
            else if (firstFactID.Contains("origin") || secondID.Contains("origin")) currentLieTopic = "origin";
            else if (firstFactID.Contains("weight") || secondID.Contains("weight")) currentLieTopic = "weight";
            else if (firstFactID.Contains("speed") || secondID.Contains("speed")) currentLieTopic = "speed";

            confrontButton.SetActive(true);

            if (RadioTutorialManager.Instance != null && !RadioTutorialManager.isRadioTutorialCompleted)
            {
                RadioTutorialManager.Instance.NotifyContradictionFound();
            }
        }

        if (isValid && !isLie && (firstFactID.Contains("cargo") || secondID.Contains("cargo")))
        {
            if (currentData.manifestCargo.ToUpper() == currentData.cargo.ToUpper())
            {
                currentData.isCargoKnown = true;
            }
        }

        StartCoroutine(ResetColorRoutine(firstFactScanner, firstFactIndex, 2f));
        StartCoroutine(ResetColorRoutine(secondScanner, secondIndex, 2f));
        firstFactID = "";

    }

    bool CheckPair(string i1, string i2, string t1, string t2) => (i1 == t1 && i2 == t2) || (i1 == t2 && i2 == t1);

    IEnumerator ResetColorRoutine(FactScanner s, int i, float d)
    {
        yield return new WaitForSecondsRealtime(d);
        if (s != null)
        {
            Color32 originalColor = s.GetComponent<TextMeshProUGUI>().color;
            s.HighlightLink(i, originalColor);
        }
    }

    public void OnConfront()
    {
        if (isTyping) return;

        if (currentLieTopic == "cargo")
        {
            currentData.isCargoKnown = true;
        }

        string exp = "Atmospheric interference, dispatcher. Everything is normal.";

        if (currentLieTopic == "cargo" && !string.IsNullOrEmpty(currentData.explanationCargo)) exp = currentData.explanationCargo;
        else if (currentLieTopic == "origin" && !string.IsNullOrEmpty(currentData.explanationOrigin)) exp = currentData.explanationOrigin;
        else if (currentLieTopic == "weight" && !string.IsNullOrEmpty(currentData.explanationWeight)) exp = currentData.explanationWeight;
        else if (currentLieTopic == "speed" && !string.IsNullOrEmpty(currentData.explanationSpeed)) exp = currentData.explanationSpeed;
        else if (!string.IsNullOrEmpty(currentData.customExplanation)) exp = currentData.customExplanation;

        confrontButton.SetActive(false);
        StartCoroutine(Routine_TypewriterChat("Explain this discrepancy.", exp, ""));
    }

    void ScrollToBottom(bool animate = false)
    {
        if (chatScroll != null && chatHistoryText != null)
        {
            Canvas.ForceUpdateCanvases();
            chatScroll.movementType = ScrollRect.MovementType.Unrestricted;
            
            RectTransform contentRect = chatScroll.content;
            RectTransform viewportRect = chatScroll.viewport;
            
            float textHeight = chatHistoryText.preferredHeight;
            float viewportHeight = viewportRect.rect.height;
            
            float topMargin = 0f;
            RectTransform textRect = chatHistoryText.GetComponent<RectTransform>();
            if (textRect != null) topMargin = Mathf.Abs(textRect.anchoredPosition.y);

            float targetY = textHeight + topMargin - viewportHeight + 50f; 
            
            if (animate)
            {
                if (scrollCoroutine != null) StopCoroutine(scrollCoroutine);
                scrollCoroutine = StartCoroutine(Routine_AnimatePaperUp(contentRect, targetY));
            }
            else
            {
                contentRect.anchoredPosition = new Vector2(contentRect.anchoredPosition.x, targetY);
            }
        }
    }

    IEnumerator Routine_AnimatePaperUp(RectTransform contentRect, float targetY)
    {
        isAnimatingPaper = true;
        chatScroll.velocity = Vector2.zero;

        float startY = contentRect.anchoredPosition.y;
        if (targetY <= startY) 
        {
            contentRect.anchoredPosition = new Vector2(contentRect.anchoredPosition.x, targetY);
            isAnimatingPaper = false;
            yield break;
        }

        float currentY = startY;
        float distance = targetY - startY;
        
        int jerks = Random.Range(Mathf.Max(1, paperScrollJerksMin), Mathf.Max(2, paperScrollJerksMax));
        float step = distance / jerks;

        StartPrinterSound();
        StartTypewriterSound();

        for (int i = 0; i < jerks; i++)
        {
            yield return new WaitForSecondsRealtime(Random.Range(paperScrollDelayMin, paperScrollDelayMax));
            
            currentY += step;
            if (i == jerks - 1) currentY = targetY; 

            contentRect.anchoredPosition = new Vector2(contentRect.anchoredPosition.x, currentY);
        }

        StopPrinterSound();
        StopTypewriterSound();
        isAnimatingPaper = false;
    }

    void LateUpdate()
    {
        if (chatScroll != null && chatHistoryText != null && !isAnimatingPaper)
        {
            RectTransform contentRect = chatScroll.content;
            float textHeight = chatHistoryText.preferredHeight;
            float viewportHeight = chatScroll.viewport.rect.height;
            
            float topMargin = 0f;
            RectTransform textRect = chatHistoryText.GetComponent<RectTransform>();
            if (textRect != null) topMargin = Mathf.Abs(textRect.anchoredPosition.y);

            float targetY = textHeight + topMargin - viewportHeight + 50f;
            
            // Разрешаем прятать бумагу вниз, но оставляем видимым верхний край
            float minY = -viewportHeight + 150f; 
            float maxY = targetY;

            if (minY > maxY) minY = maxY;

            float clampedY = Mathf.Clamp(contentRect.anchoredPosition.y, minY, maxY);
            
            if (contentRect.anchoredPosition.y != clampedY)
            {
                contentRect.anchoredPosition = new Vector2(contentRect.anchoredPosition.x, clampedY);
                chatScroll.velocity = Vector2.zero; 
            }
        }
    }

    IEnumerator Routine_StartChat()
    {
        isTyping = true;
        chatHistoryText.text = "";

        string prefix = $"<b>[{currentData.callsign}]:</b> ";
        string message = "Bastion-7, requesting landing corridor.";

        chatHistoryText.text += prefix + message + "\n\n";
        
        ScrollToBottom(true);
        yield return new WaitForSecondsRealtime(1f); // Ждем пока бумага выедет

        currentData.chatHistory = chatHistoryText.text;
        isTyping = false;
    }

    IEnumerator Routine_TypewriterChat(string question, string answer, string dataTopicToUpdate)
    {
        isTyping = true;

        chatHistoryText.text += $"<b>[YOU]:</b> {question}\n\n";
        ScrollToBottom(true);
        
        yield return new WaitForSecondsRealtime(Random.Range(2f, 3f)); // Пауза 2-3 секунды перед ответом

        string prefix = $"<b>[{currentData.callsign}]:</b> ";
        chatHistoryText.text += prefix + answer + "\n\n";
        
        ScrollToBottom(true);
        yield return new WaitForSecondsRealtime(1f); // Ждем пока бумага выедет с ответом

        currentData.chatHistory = chatHistoryText.text;

        if (dataTopicToUpdate == "cargo") { askedCargo = true; currentData.askedCargo = true; }
        else if (dataTopicToUpdate == "origin") { askedOrigin = true; currentData.askedOrigin = true; }
        else if (dataTopicToUpdate == "weight") { askedWeight = true; currentData.askedWeight = true; }
        else if (dataTopicToUpdate == "speed") { askedSpeed = true; currentData.askedSpeed = true; }

        UpdatePilotReport();
        isTyping = false;
    }

    public void EndInterrogation()
    {
        if (currentData != null)
        {
            if (manifestDocInstance != null) currentData.manifestPos = manifestDocInstance.GetComponent<RectTransform>().anchoredPosition;
            if (radarDocInstance != null) currentData.radarPos = radarDocInstance.GetComponent<RectTransform>().anchoredPosition;
            if (cheatSheetDocInstance != null) currentData.cheatSheetPos = cheatSheetDocInstance.GetComponent<RectTransform>().anchoredPosition;
        }
        if (RadarManager.Instance != null) RadarManager.Instance.SaveToGlobalManager();
        if (ButtonSoundManager.instance != null) ButtonSoundManager.instance.StopAllSounds();
        SceneManager.LoadScene("SampleScene");
    }

    public void OnFolderTorn()
    {
        if (currentData != null) currentData.isFolderTorn = true;
        
        // 1. Убираем надпись с названием рейса во время анимации
        if (folderCallsignText != null) folderCallsignText.gameObject.SetActive(false);

        // 2. Документы появляются как только анимация началась
        ShowDocuments();
    }

    // Вызывается из OnFolderTorn (в самом начале анимации)
    public void ShowDocuments()
    {
        // Показываем манифест прямо под папкой
        if (manifestDocInstance != null) 
        {
            if (folderUI != null)
            {
                manifestDocInstance.transform.position = folderUI.transform.position;
            }
            manifestDocInstance.SetActive(true);
            manifestDocInstance.transform.SetAsLastSibling();
        }

        // Поднимаем и остальные документы
        if (radarDocInstance != null) radarDocInstance.transform.SetAsLastSibling();
        if (cheatSheetDocInstance != null) cheatSheetDocInstance.transform.SetAsLastSibling();
        if (pilotReportDoc != null) pilotReportDoc.transform.SetAsLastSibling();
        
        // Важно: теперь мы перемещаем папку поверх всех документов, 
        // чтобы пока она растворяется, документы были ЗА ней.
        if (folderUI != null) folderUI.transform.SetAsLastSibling();
    }

    GameObject SpawnDocument(GameObject prefab, string text, Vector2 pos, bool hiddenInFolder = false)
    {
        GameObject doc = Instantiate(prefab != null ? prefab : defaultDocPrefab, deskArea);
        
        if (hiddenInFolder && !currentData.isFolderTorn)
        {
            doc.SetActive(false); // Hide until torn
        }
        else
        {
            doc.GetComponent<RectTransform>().anchoredPosition = pos;
        }

        doc.transform.rotation = Quaternion.Euler(0, 0, Random.Range(-2f, 2f));
        doc.GetComponent<DocumentUI>().SetContent(text);
        
        return doc;
    }

    string GetPlaneClass() => currentData.callsign.StartsWith("TR") ? "Passenger" : (currentData.callsign.StartsWith("GE") ? "Cargo" : "Courier");
}
