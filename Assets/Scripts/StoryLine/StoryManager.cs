using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class StoryManager : MonoBehaviour
{
    public static StoryManager Instance;

    [Header("Scene Management")]
    public string mainSceneName = "MainMenu";

    [Header("Transition UI")]
    public GameObject transitionScreen;
    public CanvasGroup transitionCanvasGroup;
    public TextMeshProUGUI dayText;

    [Header("Typing Settings")]
    public float typingSpeed = 0.05f;
    public AudioSource typingAudioSource;
    public AudioClip[] typingSounds;

    [Header("Testing")]
    public bool skipTutorialAndStartDay1 = false;

    private EventSystem cachedEventSystem;
    public static bool isFirstGameLoad = true;
    public static int currentDay = 1;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

#if !UNITY_EDITOR
        skipTutorialAndStartDay1 = false;
#endif

        cachedEventSystem = Object.FindFirstObjectByType<EventSystem>();

        if (isFirstGameLoad || PlayerPrefs.HasKey("StartDayNumber") || skipTutorialAndStartDay1)
        {
            LockPlayerInput(true);
            ForceBlackScreen();
        }
        else
        {
            if (transitionScreen != null) transitionScreen.SetActive(false);
            if (transitionCanvasGroup != null) transitionCanvasGroup.blocksRaycasts = false;
        }
    }

    void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
    void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (PlayerPrefs.HasKey("StartDayNumber"))
        {
            isFirstGameLoad = false;
            currentDay = PlayerPrefs.GetInt("StartDayNumber");
            PlayerPrefs.DeleteKey("StartDayNumber"); 

            ForceBlackScreen();
            StartCoroutine(WaitAndStartDay(currentDay, true));
        }
    }

    void Start()
    {
        if (skipTutorialAndStartDay1)
        {
            isFirstGameLoad = false;
            StartCoroutine(WaitAndStartDay(1, true));
            return;
        }

        if (PlayerPrefs.HasKey("StartDayNumber"))
        {
            isFirstGameLoad = false;
            currentDay = PlayerPrefs.GetInt("StartDayNumber");
            PlayerPrefs.DeleteKey("StartDayNumber");
            StartCoroutine(WaitAndStartDay(currentDay, true));
        }
        else if (isFirstGameLoad)
        {
            isFirstGameLoad = false;
            currentDay = 1;

            if (TutorialManager.isTutorialActive)
            {
                StartCoroutine(TutorialTransitionSequence());
            }
            else
            {
                StartCoroutine(WaitAndStartDay(1, true));
            }
        }
    }

    private IEnumerator TutorialTransitionSequence()
    {
        LockPlayerInput(true);
        ForceBlackScreen();

        yield return new WaitForSecondsRealtime(0.5f);

        yield return StartCoroutine(Fade(1f, 0f, 1.0f));

        if (transitionScreen != null) transitionScreen.SetActive(false);
        if (transitionCanvasGroup != null) transitionCanvasGroup.blocksRaycasts = false;

        LockPlayerInput(false);
    }

    private void ForceBlackScreen()
    {
        if (transitionScreen != null)
        {
            Canvas canvas = transitionScreen.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 32000;
            }
            RectTransform rt = transitionScreen.GetComponent<RectTransform>();
            if (rt != null) rt.localScale = Vector3.one;

            transitionScreen.SetActive(true);

            if (transitionCanvasGroup != null)
            {
                transitionCanvasGroup.alpha = 1f;
                transitionCanvasGroup.blocksRaycasts = true;
            }
            if (dayText != null) dayText.text = "";
        }
    }

    private void LockPlayerInput(bool isLocked)
    {
        if (cachedEventSystem != null) cachedEventSystem.enabled = !isLocked;
    }

    private IEnumerator WaitAndStartDay(int dayNumber, bool isScreenAlreadyBlack = false)
    {
        yield return new WaitForSecondsRealtime(0.5f);
        
        if (dayNumber == 1)
        {
            PlayerPrefs.SetInt("ReputationXP", 0);
            PlayerPrefs.Save();
        }

        StartCoroutine(DayTransitionSequence(dayNumber, isScreenAlreadyBlack));
    }



    public void EndCurrentShift()
    {
        StartCoroutine(EndShiftRoutine());
    }

    private IEnumerator EndShiftRoutine()
    {
        LockPlayerInput(true);

        try { EvaluateShiftResults(currentDay); } catch { }

        if (FlightDataManager.Instance != null)
        {
            FlightDataManager.Instance.isShiftActive = false;
        }

        ForceBlackScreen();
        if (transitionCanvasGroup != null) transitionCanvasGroup.alpha = 0f;
        yield return StartCoroutine(Fade(0f, 1f, 1.5f));

        string endText = $"<size=150%>SHIFT {currentDay} COMPLETED</size>\r\n\r\n\r\n<color=#888888><size=70%>PROCESSING DATA...</size></color>";
        yield return StartCoroutine(TypeText(endText));

        yield return new WaitForSecondsRealtime(2f);

        // Day Summary Calculation
        int planesAccepted = 0;
        int fuelChange = 0;
        int medsChange = 0;
        int peopleChange = 0;
        int foodAdded = 0;
        int foodEaten = 0;
        int fdmTotalFood = 0;

        int starvedToDeath = 0;

        if (FlightDataManager.Instance != null)
        {
            foreach (var flight in FlightDataManager.Instance.savedFlights)
            {
                if (flight.approved) planesAccepted++;
            }

            fuelChange = FlightDataManager.Instance.totalFuel - FlightDataManager.Instance.startFuelDay;
            medsChange = FlightDataManager.Instance.totalMedicines - FlightDataManager.Instance.startMedsDay;
            peopleChange = FlightDataManager.Instance.totalPeople - FlightDataManager.Instance.startPeopleDay;
            foodAdded = FlightDataManager.Instance.totalFood - FlightDataManager.Instance.startFoodDay;

            // 0.5 units per person per day (1 food feeds 2 people)
            foodEaten = Mathf.RoundToInt(FlightDataManager.Instance.totalPeople * 0.5f); 
            int missingFood = foodEaten - FlightDataManager.Instance.totalFood;
            
            FlightDataManager.Instance.totalFood -= foodEaten;
            if (FlightDataManager.Instance.totalFood < 0) 
            {
                starvedToDeath = missingFood / 2;
                if (starvedToDeath > FlightDataManager.Instance.totalPeople) starvedToDeath = FlightDataManager.Instance.totalPeople;
                
                FlightDataManager.Instance.totalFood = 0;
                FlightDataManager.Instance.totalPeople -= starvedToDeath;
                peopleChange -= starvedToDeath;
            }
            
            fdmTotalFood = FlightDataManager.Instance.totalFood;
        }

        int shiftXpGained = 0;
        if (currentDay == 1)
        {
            int eng = PlayerPrefs.GetInt("Trigger_Engineer", 0);
            int econ = PlayerPrefs.GetInt("BaseEmergencyEconomy", 0);
            if (eng == 0 && econ == 0) shiftXpGained = 150; 
            else if (eng == 1) shiftXpGained = 50; 
        }

        int xpPenalty = starvedToDeath;
        int totalXpGained = shiftXpGained - xpPenalty;
        
        int startXp = PlayerPrefs.GetInt("ReputationXP", 0);
        int finalXp = startXp + totalXpGained;
        if (finalXp < 0) finalXp = 0;
        PlayerPrefs.SetInt("ReputationXP", finalXp);

        string Col(int val) => val >= 0 ? "<color=#4AF626>" : "<color=#FF3030>";
        string Sign(int val) => val > 0 ? "+" : "";

        string summary = $"<align=center><size=280%>END OF DAY {currentDay}</size>\n\n<size=230%><align=left>";
        summary += $"<pos=20%>PLANES APPROVED<pos=70%><color=#FFFFFF>{planesAccepted}</color>\n";
        summary += $"<pos=20%>PEOPLE GAINED<pos=70%>{Col(peopleChange)}{Sign(peopleChange)}{peopleChange}</color>\n";
        summary += $"<pos=20%>NET FUEL<pos=70%>{Col(fuelChange)}{Sign(fuelChange)}{fuelChange}</color>\n";
        summary += $"<pos=20%>NET MEDICINE<pos=70%>{Col(medsChange)}{Sign(medsChange)}{medsChange}</color>\n";
        summary += $"<pos=20%>FOOD SALVAGED<pos=70%>{Col(foodAdded)}{Sign(foodAdded)}{foodAdded}</color>\n";
        summary += $"<pos=20%>FOOD CONSUMED<pos=70%><color=#FF3030>-{foodEaten}</color>\n";
        
        if (starvedToDeath > 0)
        {
            summary += $"<pos=20%>STARVATION DEATHS<pos=70%><color=#FF3030>{starvedToDeath}</color>\n";
        }
        
        summary += $"<pos=20%>------------------------------------------------------\n";
        summary += $"<pos=20%>REMAINING FOOD<pos=70%>{Col(fdmTotalFood)}{fdmTotalFood}</color>\n\n";

        summary += $"<pos=20%>SHIFT REPUTATION GAIN<pos=70%>{Col(shiftXpGained)}{Sign(shiftXpGained)}{shiftXpGained} XP</color>\n";
        if (starvedToDeath > 0)
        {
            summary += $"<pos=20%>STARVATION PENALTY<pos=70%><color=#FF3030>-{xpPenalty} XP</color>\n";
        }

        summary += $"</align></size>\n\n\n\n";
        summary += $"<align=center><color=#888888><size=140%>[ CLICK TO SLEEP ]</size></color>";

        // Build XP Bar UI BEFORE text typing
        GameObject barBgObj = new GameObject("XP_Bar_Bg");
        barBgObj.transform.SetParent(dayText.transform, false);
        RectTransform bgRt = barBgObj.AddComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0.5f, 1f); // Absolute top edge
        bgRt.anchorMax = new Vector2(0.5f, 1f);
        bgRt.pivot = new Vector2(0.5f, 1f);     // Top pivot
        bgRt.sizeDelta = new Vector2(700, 40); 
        bgRt.anchoredPosition = new Vector2(0, 150); // 30px from top edge 
        UnityEngine.UI.Image bgImg = barBgObj.AddComponent<UnityEngine.UI.Image>();
        bgImg.color = new Color(0.15f, 0.15f, 0.15f, 1f);

        GameObject barFillObj = new GameObject("XP_Bar_Fill");
        barFillObj.transform.SetParent(barBgObj.transform, false);
        RectTransform fillRt = barFillObj.AddComponent<RectTransform>();
        fillRt.anchorMin = new Vector2(0, 0);
        fillRt.anchorMax = new Vector2(Mathf.Clamp01(startXp / 350f), 1);
        fillRt.pivot = new Vector2(0, 0.5f);
        fillRt.sizeDelta = Vector2.zero;
        fillRt.anchoredPosition = Vector2.zero;
        UnityEngine.UI.Image fillImg = barFillObj.AddComponent<UnityEngine.UI.Image>();
        fillImg.color = new Color(0.29f, 0.96f, 0.15f, 1f);

        GameObject textObj = new GameObject("XP_Text");
        textObj.transform.SetParent(barBgObj.transform, false);
        RectTransform txtRt = textObj.AddComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
        txtRt.sizeDelta = Vector2.zero;
        txtRt.anchoredPosition = new Vector2(0, 40); // Above the bar
        TextMeshProUGUI xpTxt = textObj.AddComponent<TextMeshProUGUI>();
        xpTxt.font = dayText.font;
        xpTxt.fontSize = 32;
        xpTxt.alignment = TextAlignmentOptions.Center;
        xpTxt.color = Color.white;
        xpTxt.text = $"REPUTATION: {startXp} / 350";

        // Add Next Level text to the right
        GameObject lvlObj = new GameObject("Level_Text");
        lvlObj.transform.SetParent(barBgObj.transform, false);
        RectTransform lvlRt = lvlObj.AddComponent<RectTransform>();
        lvlRt.anchorMin = new Vector2(1, 0.5f); 
        lvlRt.anchorMax = new Vector2(1, 0.5f);
        lvlRt.pivot = new Vector2(0, 0.5f); 
        lvlRt.sizeDelta = new Vector2(200, 40);
        lvlRt.anchoredPosition = new Vector2(20, 0); // Offset 20px to the right of the bar
        TextMeshProUGUI lvlTxt = lvlObj.AddComponent<TextMeshProUGUI>();
        lvlTxt.font = dayText.font;
        lvlTxt.fontSize = 32;
        lvlTxt.alignment = TextAlignmentOptions.Left;
        lvlTxt.color = new Color(0.29f, 0.96f, 0.15f, 1f); // Green color
        lvlTxt.text = "LEVEL 1";

        yield return StartCoroutine(TypeText(summary));
        
        // Animate XP Bar AFTER typing is done
        float animTime = 1.5f;
        float elapsed = 0f;
        while (elapsed < animTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animTime;
            int curXp = Mathf.RoundToInt(Mathf.Lerp(startXp, finalXp, t));
            fillRt.anchorMax = new Vector2(Mathf.Clamp01(curXp / 350f), 1);
            xpTxt.text = $"REPUTATION: {curXp} / 350";
            yield return null;
        }

        // Wait for user to click
        yield return new WaitUntil(() => UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame);

        if (barBgObj != null) Destroy(barBgObj);
        if (dayText != null) dayText.text = "";

        currentDay++;
        PlayerPrefs.SetInt("StartDayNumber", currentDay);
        PlayerPrefs.Save();

        if (!string.IsNullOrEmpty(mainSceneName) && mainSceneName != SceneManager.GetActiveScene().name)
        {
            if (ButtonSoundManager.instance != null) ButtonSoundManager.instance.StopAllSounds();
            AsyncOperation op = SceneManager.LoadSceneAsync(mainSceneName);
            while (!op.isDone) yield return null;
        }
        else
        {
            StartCoroutine(WaitAndStartDay(currentDay, true));
        }
    }

    private void EvaluateShiftResults(int shiftDay)
    {
        if (FlightDataManager.Instance == null) return;

        if (shiftDay == 1)
        {
            bool letRefugeesIn = false;

            foreach (var flight in FlightDataManager.Instance.savedFlights)
            {
                if (flight.callsign == "TR-404" && flight.approved) letRefugeesIn = true;
            }

            int finalFuel = FlightDataManager.Instance.totalFuel;
            bool fuelTargetMet = (finalFuel >= 400);

            // Save variables for Day 2 in memory
            PlayerPrefs.SetInt("BaseEmergencyEconomy", fuelTargetMet ? 0 : 1);
            PlayerPrefs.SetInt("Trigger_Engineer", letRefugeesIn ? 1 : 0);
            PlayerPrefs.Save();

            if (letRefugeesIn)
            {
                AegisMailApp.ReceiveNewEmail(new EmailData
                {
                    sender = "Chief Engineer Mitchell",
                    subject = "Thank you from the survivors",
                    date = "20.08.2038",
                    body = "Dispatcher, I was on board TR-404. You saved my life and the lives of 64 others when our engines were failing. The Director is furious about the fuel shortage, but I've already set up a workspace in the hangar. I will do everything I can to help you optimize the base systems. We owe you our lives."
                });
            }
            else
            {
                AegisMailApp.ReceiveNewEmail(new EmailData
                {
                    sender = "Aegis Auto-Alert",
                    subject = "CRASH REPORT: TR-404",
                    date = "20.08.2038",
                    body = "AUTOMATED NOTIFICATION:\n\nFlight TR-404 lost signal 40 miles off the coast of Bastion-7. Presumed destroyed by the storm.\n\nCasualties: 65.\nSurvivors: 0."
                });
            }
        }
        else if (shiftDay == 2)
        {
            bool acceptedEQ = false;
            bool acceptedMeds = false;
            bool acceptedFuel = false;

            foreach (var flight in FlightDataManager.Instance.savedFlights)
            {
                if (flight.approved)
                {
                    if (flight.callsign == "GE-99") acceptedEQ = true;
                    if (flight.callsign == "QY-01") acceptedMeds = true;
                    if (flight.callsign == "GE-55") acceptedFuel = true;
                }
            }

            int engineerTrigger = PlayerPrefs.GetInt("Trigger_Engineer", 0);
            int day3Slots = 3;

            if (engineerTrigger == 1) // Branch B
            {
                if (acceptedEQ)
                {
                    day3Slots = 4; // Combo
                }
                else if (!acceptedMeds && !acceptedFuel)
                {
                    day3Slots = 2; // Failed completely
                }
            }

            PlayerPrefs.SetInt("Day3Slots", day3Slots);
            PlayerPrefs.Save();
        }
    }

    private IEnumerator DayTransitionSequence(int dayNumber, bool isScreenAlreadyBlack)
    {
        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.StopTutorial();
        }

        LockPlayerInput(true);
        ForceBlackScreen(); 

        if (!isScreenAlreadyBlack) yield return StartCoroutine(Fade(0f, 1f, 1.0f));

        if (FlightDataManager.Instance != null)
        {
            if (dayNumber == 1)
            {
                FlightDataManager.Instance.ResetForNewShift(220, 140, 180, 5);
                FlightDataManager.Instance.maxPlanes = 3;
            }
            else if (dayNumber == 2)
            {
                FlightDataManager.Instance.ResetForNewShift(
                    FlightDataManager.Instance.totalFuel,
                    FlightDataManager.Instance.totalFood,
                    FlightDataManager.Instance.totalPeople,
                    FlightDataManager.Instance.totalMedicines
                );
                FlightDataManager.Instance.maxPlanes = 3;
            }
            else if (dayNumber >= 3)
            {
                FlightDataManager.Instance.ResetForNewShift(
                    FlightDataManager.Instance.totalFuel,
                    FlightDataManager.Instance.totalFood,
                    FlightDataManager.Instance.totalPeople,
                    FlightDataManager.Instance.totalMedicines
                );
                FlightDataManager.Instance.maxPlanes = PlayerPrefs.GetInt("Day3Slots", 3);
            }
        }

        string displayDate = (18 + dayNumber) + ".08.2038";
        string targetText = $"<size=150%>SHIFT {dayNumber}</size>\r\n\r\n\r\n<color=#888888><size=70%>{displayDate}</size></color>";

        yield return StartCoroutine(TypeText(targetText));
        yield return new WaitForSecondsRealtime(2.5f);

        if (dayNumber == 1) SendDay1Directives();
        else if (dayNumber == 2) SendDay2Directives();

        yield return StartCoroutine(Fade(1f, 0f, 1.5f));

        if (transitionScreen != null) transitionScreen.SetActive(false);
        if (transitionCanvasGroup != null) transitionCanvasGroup.blocksRaycasts = false;

        LockPlayerInput(false);

        if (FlightDataManager.Instance != null)
        {
            FlightDataManager.Instance.StartDaySpawning(dayNumber);
        }

        if (dayNumber == 2 && PlayerPrefs.GetInt("BaseEmergencyEconomy", 0) == 1)
        {
            Canvas mainCanvas = Object.FindFirstObjectByType<Canvas>();
            if (mainCanvas != null && mainCanvas.GetComponent<CRTNoiseEffect>() == null)
            {
                mainCanvas.gameObject.AddComponent<CRTNoiseEffect>();
            }
        }
    }

    private IEnumerator TypeText(string textToType)
    {
        if (dayText == null) yield break;

        dayText.text = textToType;
        dayText.maxVisibleCharacters = 0;
        
        // Wait one frame to ensure TextMeshPro has fully parsed the rich text and layout
        yield return null;

        int totalCharacters = dayText.textInfo.characterCount;
        if (totalCharacters <= 0) totalCharacters = textToType.Length;

        int soundIndex = 0;

        // Auto-create AudioSource if missing
        if (typingSounds != null && typingSounds.Length > 0 && typingAudioSource == null)
        {
            typingAudioSource = gameObject.AddComponent<AudioSource>();
            typingAudioSource.playOnAwake = false;
        }

        for (int i = 0; i <= totalCharacters; i++)
        {
            dayText.maxVisibleCharacters = i;
            
            // Play typing sound when adding a character
            if (i > 0 && typingSounds != null && typingSounds.Length > 0 && typingAudioSource != null)
            {
                typingAudioSource.PlayOneShot(typingSounds[soundIndex]);
                soundIndex = (soundIndex + 1) % typingSounds.Length;
            }

            yield return new WaitForSecondsRealtime(typingSpeed);
        }
    }

    private void SendDay1Directives()
    {
        EmailData day1Email = new EmailData
        {
            sender = "Director Reed",
            subject = "DIRECTIVE #1 - URGENT",
            date = "19.08.2038",
            body = "Listen carefully, Dispatcher. Night storm damaged the runways. You only have THREE landing slots available today.\n\nA magnetic storm hit us last night. The base's generators are running at their limit. Your main task for today is to collect Fuel. If we do not collect a critical volume of at least 400 liters of fuel by the end of the shift, tomorrow the base will transition to EMERGENCY ECONOMY MODE.\n\nAnd one more thing. Civilian refugees have been spotted in the sector. We have neither food nor beds for them.\n\nDIRECTIVE #1: Aircraft with civilians (Prefix TR) are STRICTLY FORBIDDEN from landing. Turn them back into the storm."
        };

        try { AegisMailApp.ReceiveNewEmail(day1Email); } catch { }
    }

    private void SendDay2Directives()
    {
        bool letRefugeesIn = PlayerPrefs.GetInt("Trigger_Engineer", 0) == 1;
        bool fuelTargetMet = PlayerPrefs.GetInt("BaseEmergencyEconomy", 0) == 0;

        EmailData day2Email = new EmailData();
        day2Email.date = "20.08.2038";

        if (!letRefugeesIn && fuelTargetMet)
        {
            // Branch A: Marauders
            day2Email.sender = "Director Reed";
            day2Email.subject = "SECURITY ALERT — PERIMETER BREACH";
            day2Email.body = "ATS, listen carefully. That passenger plane you turned away yesterday crashed five miles outside the perimeter. The burning wreckage served as a beacon for local looters. Now these looters have spotted our gates and are actively trying to breach the outer fence. Our fighters will fight with all their might, but they’re unlikely to hold out for long—there are too many of them.\n\nUsing my old connections, I convinced one of our friendly bases to send us reinforcements; a heavy transport carrying a special forces unit is on its way. You MUST immediately clear a landing zone for them. If they don’t secure the perimeter before nightfall, we’ll all be killed.\n\nATTENTION: The enemy might try to send a fake transport. When asked about their cargo, the REAL transport will say the password 'QYEW'. The enemy will say something similar. Do NOT let the enemy land!";
        }
        else if (!letRefugeesIn && !fuelTargetMet)
        {
            // Branch A-2: Marauders + Blackout
            day2Email.sender = "Director Reed";
            day2Email.subject = "PERIMETER BREACH & POWER FAILURE";
            day2Email.body = "You failed the simplest task yesterday. The grid is dying, and we are sitting in the dark.\n\nTo make matters worse, that passenger plane you turned away crashed five miles outside the perimeter. The burning wreckage acted like a beacon for local scavengers. Now, marauders are using our blackout to their advantage and are actively breaching the external gates.\n\nYOUR DIRECTIVE:\n> You have two critical jobs today. First, using my old connections, I convinced one of our friendly bases to send us reinforcements; a heavy transport carrying a special forces unit is on its way. You MUST immediately clear a landing zone for them. Second, get a Fuel transport down here before your radar shuts off completely.\n\nDo not waste time on anything else. If you fail to bring in the ops team or the fuel, we are all dead.\n\nATTENTION: The enemy might try to send a fake transport. When asked about their cargo, the REAL transport will say the password 'QYEW'. The enemy will say something similar. Do NOT let the enemy land!";
        }
        else if (letRefugeesIn && !fuelTargetMet)
        {
            // Branch B-1: Epidemic + Power Outage
            day2Email.sender = "Director Reed";
            day2Email.subject = "QUARANTINE PROTOCOL AND POWER OUTAGE";
            day2Email.body = "You’re an idiot.\n\nNot only are we sitting in the dark because you failed to secure the fuel quota yesterday, but those “civilians” you let in also brought a pathogen with them. It’s absolute hell on the lower levels right now.\n\nYOUR TASK:\n> Today you have two critical tasks to fix the mess you’ve made. First, receive a fuel shipment so your radar doesn’t go completely offline. Second, immediately deliver medical supplies so we don’t rot from the inside out.\n\nADDITIONAL NOTE:\n> Control tower reports that an engineering cargo plane is approaching; by a lucky coincidence, there is an engineer among these refugees who can help us. If you have any room left, take him on board. But fuel and medical supplies are the priority.";
        }
        else if (letRefugeesIn && fuelTargetMet)
        {
            // Branch B-2: Epidemic (No Blackout)
            day2Email.sender = "Director Reed";
            day2Email.subject = "QUARANTINE PROTOCOL";
            day2Email.body = "You met the fuel quota yesterday, so at least the grid is stable. But you just couldn't follow simple orders, could you?\n\nThose \"civilians\" you let in brought a pathogen with them. It is an absolute hellzone on the lower levels right now. Your charity has consequences.\n\nYOUR DIRECTIVE:\n> We need Medical supplies immediately so we don't rot from the inside out. Clear a landing slot for a medical transport.\n\nSECONDARY NOTE:\n> Dispatch reports an engineering cargo plane is inbound. Since our power grid is stable, you don't need to waste a slot on fuel today. Bring the engineers in, but prioritize the meds first.\n\nADDITIONAL NOTE:\n> Control tower reports that an engineering cargo plane is approaching; by a lucky coincidence, there is an engineer among these refugees who can help us. If you have any room left, take him on board. But fuel and medical supplies are the priority.";
        }

        try { AegisMailApp.ReceiveNewEmail(day2Email); } catch { }
    }

    private IEnumerator Fade(float startAlpha, float targetAlpha, float duration)
    {
        if (transitionCanvasGroup == null) yield break;

        float time = 0;
        transitionCanvasGroup.alpha = startAlpha;
        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            transitionCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
            yield return null;
        }
        transitionCanvasGroup.alpha = targetAlpha;
    }
}