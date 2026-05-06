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

    [Header("Single Question Button")]
    public GameObject askButton;
    public TextMeshProUGUI askButtonText;

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

    void Awake()
    {
        Instance = this;
        if (confrontButton != null) confrontButton.SetActive(false);
        if (askButton != null) askButton.SetActive(false);
    }

    void Start()
    {
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
        string manifestText = $"<align=center><b>FLIGHT MANIFEST</b></align>\n\n" +
                              $"<b>FLIGHT:</b> {currentData.callsign}\n" +
                              $"<link=\"unlock_origin\"><b>ORIGIN:</b></link> <link=\"man_origin\">{currentData.manifestOrigin}</link>\n" +
                              $"<b>CARGO:</b> <link=\"man_cargo\">{currentData.manifestCargo.ToUpper()}</link>\n" +
                              $"<link=\"unlock_weight\"><b>WEIGHT:</b></link> <link=\"man_weight\">{currentData.manifestCargoAmount} UNITS</link>\n";

        SpawnDocument(manifestPrefab, manifestText, new Vector2(-380, 80));

        string radarLogText = $"<align=center><b>RADAR REPORT</b></align>\n\n" +
                              $"<link=\"unlock_speed\"><b>SPEED:</b></link> <link=\"rad_speed\">{currentData.speed * 10f} KTS</link>\n" +
                              $"<b>CLASS:</b> {GetPlaneClass()}\n" +
                              $"<link=\"unlock_cargo\"><b>SENSOR:</b></link> UNKNOWN\n";
        SpawnDocument(radarPrefab, radarLogText, new Vector2(-150, -20));

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
        SpawnDocument(cheatSheetPrefab, cheatSheetText, new Vector2(210, 140));

        GameObject reportObj = Instantiate(pilotReportPrefab != null ? pilotReportPrefab : defaultDocPrefab, deskArea);
        reportObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(100, -120);
        pilotReportDoc = reportObj.GetComponent<DocumentUI>();
        UpdatePilotReport();
    }

    void UpdatePilotReport()
    {
        string reportText = $"<align=center><b>PILOT'S STATEMENT</b></align>\n\n";
        if (!askedCargo && !askedOrigin && !askedWeight && !askedSpeed)
        {
            reportText += "<i>No interrogation data.</i>";
        }
        else
        {
            if (askedOrigin) reportText += $"<b>ORIGIN:</b> <link=\"rep_origin\">{GetStatedOrigin()}</link>\n";
            if (askedCargo) reportText += $"<b>CARGO:</b> <link=\"rep_cargo\">{GetStatedCargo().ToUpper()}</link>\n";
            if (askedWeight) reportText += $"<b>WEIGHT:</b> <link=\"rep_weight\">{GetStatedWeight()} UNITS</link>\n";
            if (askedSpeed) reportText += $"<b>SPEED:</b> <link=\"rep_speed\">{GetStatedSpeed()} KTS</link>\n";
        }
        pilotReportDoc.SetContent(reportText);
    }

    public void SelectFact(string factID, string factText, FactScanner scanner, int linkIndex)
    {
        if (isTyping) return;

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
                question = "State your cargo purpose.";
                answer = $"We are transporting {GetStatedCargo().ToUpper()}.";
                break;
            case "origin":
                question = "Confirm your point of origin.";
                answer = $"Flight originated from {GetStatedOrigin()}.";
                break;
            case "weight":
                question = "Report cargo weight.";
                answer = $"Manifest states {GetStatedWeight()} UNITS.";
                break;
            case "speed":
                question = "Confirm your current airspeed.";
                answer = $"Instruments show {GetStatedSpeed()} KTS.";
                break;
        }

        string topic = pendingQuestionTopic;
        askButton.SetActive(false);
        pendingQuestionTopic = "";

        StartCoroutine(Routine_TypewriterChat(question, answer, topic));
    }

    void CheckContradiction(string secondID, FactScanner secondScanner, int secondIndex)
    {
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
                isValid = true; isLie = false;
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
        yield return new WaitForSeconds(d);
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

    void ScrollToBottom()
    {
        if (chatScroll != null)
        {
            Canvas.ForceUpdateCanvases();
            chatScroll.verticalNormalizedPosition = 0f;
        }
    }

    IEnumerator Routine_StartChat()
    {
        isTyping = true;
        chatHistoryText.text = "";

        string prefix = $"<b>[{currentData.callsign}]:</b> ";
        string message = "Bastion-7, requesting landing corridor.";

        float thinkTime = Random.Range(1f, 3f);
        float elapsed = 0f;
        int dots = 1;
        while (elapsed < thinkTime)
        {
            chatHistoryText.text = prefix + new string('.', dots);
            ScrollToBottom();

            dots = (dots % 3) + 1;
            float step = 0.3f;
            elapsed += step;
            yield return new WaitForSeconds(step);
        }

        chatHistoryText.text = prefix;
        foreach (char c in message)
        {
            chatHistoryText.text += c;
            ScrollToBottom();
            yield return new WaitForSeconds(0.03f);
        }
        chatHistoryText.text += "\n";
        ScrollToBottom();

        currentData.chatHistory = chatHistoryText.text;

        isTyping = false;
    }

    IEnumerator Routine_TypewriterChat(string question, string answer, string dataTopicToUpdate)
    {
        isTyping = true;

        chatHistoryText.text += $"\n<b>[YOU]:</b> {question}\n";
        ScrollToBottom();

        yield return new WaitForSeconds(0.5f);

        string prefix = $"<b>[{currentData.callsign}]:</b> ";
        string baseText = chatHistoryText.text;

        float thinkTime = Random.Range(1.5f, 4.0f);
        float elapsed = 0f;
        int dots = 1;

        while (elapsed < thinkTime)
        {
            chatHistoryText.text = baseText + prefix + new string('.', dots);
            ScrollToBottom();

            dots = (dots % 3) + 1;
            float step = 0.3f;
            elapsed += step;
            yield return new WaitForSeconds(step);
        }

        chatHistoryText.text = baseText + prefix;
        foreach (char c in answer)
        {
            chatHistoryText.text += c;
            ScrollToBottom();
            yield return new WaitForSeconds(0.03f);
        }
        chatHistoryText.text += "\n";
        ScrollToBottom();

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
        SceneManager.LoadScene("SampleScene");
    }

    void SpawnDocument(GameObject prefab, string text, Vector2 pos)
    {
        GameObject doc = Instantiate(prefab != null ? prefab : defaultDocPrefab, deskArea);
        doc.GetComponent<RectTransform>().anchoredPosition = pos;
        doc.transform.rotation = Quaternion.Euler(0, 0, Random.Range(-2f, 2f));
        doc.GetComponent<DocumentUI>().SetContent(text);
    }

    string GetPlaneClass() => currentData.callsign.StartsWith("TR") ? "Passenger" : (currentData.callsign.StartsWith("GE") ? "Cargo" : "Courier");
}