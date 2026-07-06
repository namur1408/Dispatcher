using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Handles UI interactions, document spawning, and teletype animations for Radio/Comms.
/// Extracted from CommsManager to prevent God Object anti-pattern.
/// </summary>
public class CommsUIController
{
    private CommsManager manager;

    // We cache all the required references so we don't have to keep digging into manager.
    private TextMeshProUGUI chatHistoryText;
    private ScrollRect chatScroll;
    private GameObject folderUI;
    private TextMeshProUGUI folderCallsignText;
    private GameObject askButton;
    private TextMeshProUGUI askButtonText;
    private GameObject confrontButton;
    private Transform deskArea;

    // Prefabs
    private GameObject manifestPrefab;
    private GameObject radarPrefab;
    private GameObject cheatSheetPrefab;
    private GameObject pilotReportPrefab;
    private GameObject defaultDocPrefab;
    private GameObject decryptionPaperPrefab;

    // Instances
    public GameObject manifestDocInstance { get; private set; }
    public GameObject radarDocInstance { get; private set; }
    public GameObject cheatSheetDocInstance { get; private set; }
    public DocumentUI pilotReportDoc { get; private set; }
    public GameObject decryptionPaperInstance { get; private set; }

    private bool isAnimatingPaper = false;
    private Coroutine scrollCoroutine;

    public CommsUIController(CommsManager manager)
    {
        this.manager = manager;
        
        chatHistoryText = manager.chatHistoryText;
        chatScroll = manager.chatScroll;
        folderUI = manager.folderUI;
        folderCallsignText = manager.folderCallsignText;
        askButton = manager.askButton;
        askButtonText = manager.askButtonText;
        confrontButton = manager.confrontButton;
        deskArea = manager.deskArea;

        manifestPrefab = manager.manifestPrefab;
        radarPrefab = manager.radarPrefab;
        cheatSheetPrefab = manager.cheatSheetPrefab;
        pilotReportPrefab = manager.pilotReportPrefab;
        defaultDocPrefab = manager.defaultDocPrefab;
        decryptionPaperPrefab = manager.decryptionPaperPrefab;
    }

    public void SetupFolder(FlightData data, FlightInterrogationState currentState)
    {
        if (folderUI != null)
        {
            bool showFolder = currentState != null && !currentState.isFolderTorn;
            folderUI.SetActive(showFolder);
            if (showFolder)
            {
                var tearComponents = folderUI.GetComponentsInChildren<FolderTearInteractable>(true);
                foreach (var tearComponent in tearComponents)
                {
                    tearComponent.ResetTear();
                }
            }
            if (folderCallsignText != null)
            {
                folderCallsignText.gameObject.SetActive(showFolder);
                folderCallsignText.text = data.callsign;
            }
        }
    }

    public void ClearDocuments()
    {
        if (manifestDocInstance != null) { Object.Destroy(manifestDocInstance); manifestDocInstance = null; }
        if (radarDocInstance != null) { Object.Destroy(radarDocInstance); radarDocInstance = null; }
        if (cheatSheetDocInstance != null) { Object.Destroy(cheatSheetDocInstance); cheatSheetDocInstance = null; }
        if (pilotReportDoc != null) { Object.Destroy(pilotReportDoc.gameObject); pilotReportDoc = null; }
        if (decryptionPaperInstance != null) { Object.Destroy(decryptionPaperInstance); decryptionPaperInstance = null; }
    }

    public void GenerateDocuments(FlightData currentData, CommsDocumentLogic logic)
    {
        string highlightStart = "";
        string highlightEnd = "";

        FlightInterrogationState currentState = FlightDataManager.Instance.GetOrCreateInterrogationState(currentData.callsign);

        string manifestText = $"<align=center><b>FLIGHT MANIFEST</b></align>\n\n" +
                              $"<b>FLIGHT:</b> {currentData.callsign}\n" +
                              $"<b>ORIGIN:</b> <link=\"man_origin\">{currentData.manifestOrigin}</link>\n" +
                              $"<b>CARGO:</b> {highlightStart}<link=\"man_cargo\">{currentData.manifestCargo.ToUpper()}</link>{highlightEnd}\n" +
                              $"<b>WEIGHT:</b> <link=\"man_weight\">{currentData.manifestCargoAmount} UNITS</link>\n";

        manifestDocInstance = SpawnDocument(manifestPrefab, manifestText, currentState.manifestPos, true, currentState);

        string radarLogText = $"<align=center><b>RADAR REPORT</b></align>\n\n" +
                              $"<b>SPEED:</b> <link=\"rad_speed\">{currentData.speed * 5f} KTS</link>\n" +
                              $"<b>CLASS:</b> {logic.GetPlaneClass()}\n" +
                              $"{highlightStart}<b>SENSOR:</b>{highlightEnd} UNKNOWN\n";
        radarDocInstance = SpawnDocument(radarPrefab, radarLogText, currentState.radarPos, false, currentState);

        string cheatSheetText = $"<size=80%><b>QUICK REF:</b>\n\n" +
                                $"<b>[GE] Heavy Cargo</b>\n" +
                                $"<link=\"rule_ge_speed\">Speed: < 425 KTS</link>\n" +
                                $"<link=\"rule_ge_weight\">Max Wt: 500 UNITS</link>\n\n" +
                                $"<b>[TR] Passenger</b>\n" +
                                $"<link=\"rule_tr_cargo\">Cargo: PEOPLE ONLY</link>\n" +
                                $"<link=\"rule_tr_speed\">Speed: 350-390 KTS</link>\n\n" +
                                $"<b>[QY] Light Courier</b>\n" +
                                $"<link=\"rule_qy_speed\">Speed: > 400 KTS</link>\n" +
                                $"<link=\"rule_qy_weight\">Max Wt: 50 UNITS</link>\n</size>";
        cheatSheetDocInstance = SpawnDocument(cheatSheetPrefab, cheatSheetText, currentState.cheatSheetPos, false, currentState);

        GameObject reportObj = Object.Instantiate(pilotReportPrefab != null ? pilotReportPrefab : defaultDocPrefab, deskArea);
        Vector3 reportLocPos = reportObj.transform.localPosition;
        reportLocPos.z = 0f;
        reportObj.transform.localPosition = reportLocPos;
        reportObj.GetComponent<RectTransform>().anchoredPosition = currentState.pilotReportPos;

        reportObj.transform.rotation = Quaternion.Euler(0, 0, Random.Range(-2f, 2f));
        pilotReportDoc = reportObj.GetComponent<DocumentUI>();
        UpdatePilotReport(currentData, currentState, logic);

        if (StoryManager.currentDay == 2 && (currentData.callsign == "TR-11" || currentData.callsign == "TR-88" || currentData.callsign == "GE-99" || currentData.callsign == "GE-98"))
        {
            string shiftText = $"<align=center><b>ENCRYPTION</b></align>\n<b>TODAY'S SHIFT:</b><align=center><b> -8</b></align></size>";
            decryptionPaperInstance = SpawnDocument(decryptionPaperPrefab != null ? decryptionPaperPrefab : defaultDocPrefab, shiftText, new Vector2(0,0), true, currentState);
        }
    }

    public void UpdatePilotReport(FlightData currentData, FlightInterrogationState currentState, CommsDocumentLogic logic)
    {
        string highlightStart = "";
        string highlightEnd = "";

        string reportText = $"<align=center><b>PILOT'S STATEMENT</b></align>\n\n";

        if (currentState != null && currentState.askedOrigin) reportText += $"<b>ORIGIN:</b> <link=\"rep_origin\">{logic.GetStatedOrigin()}</link>\n";
        else reportText += $"<link=\"unlock_origin\"><b>ORIGIN:</b></link>\n";

        if (currentState != null && currentState.askedCargo) reportText += $"<b>CARGO:</b> {highlightStart}<link=\"rep_cargo\">{logic.GetStatedCargo().ToUpper()}</link>{highlightEnd}\n";
        else reportText += $"{highlightStart}<link=\"unlock_cargo\"><b>CARGO:</b></link>{highlightEnd}\n";

        if (currentState != null && currentState.askedWeight) reportText += $"<b>WEIGHT:</b> <link=\"rep_weight\">{logic.GetStatedWeight()} UNITS</link>\n";
        else reportText += $"<link=\"unlock_weight\"><b>WEIGHT:</b></link>\n";

        if (currentState != null && currentState.askedSpeed) reportText += $"<b>SPEED:</b> <link=\"rep_speed\">{logic.GetStatedSpeed()} KTS</link>\n";
        else reportText += $"<link=\"unlock_speed\"><b>SPEED:</b></link>\n";

        pilotReportDoc.SetContent(reportText);
    }

    private GameObject SpawnDocument(GameObject prefab, string text, Vector2 pos, bool hiddenInFolder, FlightInterrogationState currentState)
    {
        GameObject doc = Object.Instantiate(prefab != null ? prefab : defaultDocPrefab, deskArea);
        
        Vector3 locPos = doc.transform.localPosition;
        locPos.z = 0f;
        doc.transform.localPosition = locPos;
        
        if (hiddenInFolder && currentState != null && !currentState.isFolderTorn)
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

    public void ShowDocuments()
    {
        if (radarDocInstance != null) radarDocInstance.transform.SetAsLastSibling();
        if (cheatSheetDocInstance != null) cheatSheetDocInstance.transform.SetAsLastSibling();
        if (pilotReportDoc != null) pilotReportDoc.transform.SetAsLastSibling();

        if (manifestDocInstance != null) 
        {
            if (folderUI != null)
            {
                manifestDocInstance.transform.position = folderUI.transform.position;
            }
            manifestDocInstance.SetActive(true);
            manifestDocInstance.transform.SetAsLastSibling();
        }

        if (decryptionPaperInstance != null)
        {
            if (folderUI != null)
            {
                decryptionPaperInstance.transform.position = folderUI.transform.position;
                RectTransform rt = decryptionPaperInstance.GetComponent<RectTransform>();
                if (rt != null) rt.anchoredPosition += new Vector2(100f, -60f);
            }
            decryptionPaperInstance.SetActive(true);
            decryptionPaperInstance.transform.SetAsLastSibling();
        }
        
        if (folderUI != null) folderUI.transform.SetAsLastSibling();
    }

    public void SetupDecryptionMachine(FlightData currentData, FlightInterrogationState currentState, GameObject decryptionMachineObj)
    {
        if (decryptionMachineObj == null) return;

        bool isSpecialForces = currentData.callsign == "TR-11" || currentData.callsign == "TR-88";
        bool isEquipment     = currentData.callsign == "GE-99" || currentData.callsign == "GE-98";

        if (StoryManager.currentDay == 2 && (isSpecialForces || isEquipment) && currentState != null && currentState.askedCargo)
        {
            decryptionMachineObj.SetActive(true);
            DecryptionMachine dm = decryptionMachineObj.GetComponentInChildren<DecryptionMachine>(true);
            if (dm != null)
            {
                dm.ResetMachine();
                if      (currentData.callsign == "TR-11") dm.SetEncryptedWord("MKPW");
                else if (currentData.callsign == "TR-88") dm.SetEncryptedWord("MKPU");
                else if (currentData.callsign == "GE-99") dm.SetEncryptedWord("AINM");
                else if (currentData.callsign == "GE-98") dm.SetEncryptedWord("AIOX");
            }
        }
        else
        {
            decryptionMachineObj.SetActive(false);
        }
    }

    public void ClearChat()
    {
        if (chatHistoryText != null) chatHistoryText.text = "";
    }

    public void SetChatText(string text)
    {
        if (chatHistoryText != null) chatHistoryText.text = text;
    }

    public void ScrollToBottom(bool animate = false)
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
                if (scrollCoroutine != null) manager.StopCoroutine(scrollCoroutine);
                scrollCoroutine = manager.StartCoroutine(Routine_AnimatePaperUp(contentRect, targetY));
            }
            else
            {
                contentRect.anchoredPosition = new Vector2(contentRect.anchoredPosition.x, targetY);
            }
        }
    }

    private IEnumerator Routine_AnimatePaperUp(RectTransform contentRect, float targetY)
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
        
        int jerks = Random.Range(Mathf.Max(1, manager.paperScrollJerksMin), Mathf.Max(2, manager.paperScrollJerksMax));
        float step = distance / jerks;

        manager.StartPrinterSound();
        manager.StartTypewriterSound();

        for (int i = 0; i < jerks; i++)
        {
            yield return new WaitForSecondsRealtime(Random.Range(manager.paperScrollDelayMin, manager.paperScrollDelayMax));
            
            currentY += step;
            if (i == jerks - 1) currentY = targetY; 

            contentRect.anchoredPosition = new Vector2(contentRect.anchoredPosition.x, currentY);
        }

        manager.StopPrinterSound();
        manager.StopTypewriterSound();
        isAnimatingPaper = false;
    }

    public void LateUpdateUI()
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
}
