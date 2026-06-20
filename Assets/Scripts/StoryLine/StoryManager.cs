using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class StoryManager : SingletonMB<StoryManager>
{
    protected override bool ShouldPersist => true;

    [Header("Scene Management")]
    public string mainSceneName = "MainMenu";

    [Header("UI Canvas Groups")]
    public CanvasGroup transitionCanvasGroup;
    public CanvasGroup storyCanvasGroup;

    [Header("Special Radar Icons")]
    [Tooltip("Иконка упавшего самолета на радаре, появляется на 2-й день в ветке мародеров")]
    public GameObject crashedPlaneRadarIcon;

    [Header("Ambience Audio")]
    [Tooltip("Объект со звуками перестрелок/сирен. Включится на 2 день в ветке мародеров.")]
    public GameObject marauderAmbienceRoot;

    [Header("Transition UI")]
    public GameObject transitionScreen;
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

    [Header("Single Scene Mode (Optional)")]
    public Camera gameCamera;
    public GameObject gameScreenRoot;
    public GameObject currentStoryRoot;

    public enum Day2Outcome { None, Won, Lost_NoSF }
    public Day2Outcome currentDay2Outcome = Day2Outcome.None;
    public int diseaseDeathsThisShift = 0;

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this) return;

#if !UNITY_EDITOR
        skipTutorialAndStartDay1 = false;
#endif

        cachedEventSystem = Object.FindFirstObjectByType<EventSystem>();

        if (isFirstGameLoad || PlayerPrefs.HasKey(SaveKeys.StartDayNumber) || skipTutorialAndStartDay1)
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
        // --- ЗАГРУЗКА СОХРАНЕНИЯ (Continue) ---
        // StoryManager — DontDestroyOnLoad, Start() вызывается только раз при первой сцене.
        // Все последующие загрузки сцен попадают сюда в OnSceneLoaded.
        if (GameSaveManager.loadedData != null)
        {
            isFirstGameLoad = false;
            currentDay = GameSaveManager.loadedData.currentDay;

            if (GameSaveManager.loadedData.savedEmails != null && GameSaveManager.loadedData.savedEmails.Count > 0)
            {
                AegisMailApp.RestoreInbox(GameSaveManager.loadedData.savedEmails);
            }

            bool shiftWasActive = GameSaveManager.loadedData.isShiftActive;

            if (FlightDataManager.Instance != null)
                FlightDataManager.Instance.LoadState(GameSaveManager.loadedData);

            GameSaveManager.loadedData = null;

            if (shiftWasActive)
            {
                ForceBlackScreen();
                StartCoroutine(ResumeMidShiftRoutine());
            }
            else
            {
                StartCoroutine(WaitAndStartDay(currentDay, true));
            }
            return;
        }

        // --- ПЕРЕХОД МЕЖДУ ДНЯМИ ---
        if (PlayerPrefs.HasKey(SaveKeys.StartDayNumber))
        {
            isFirstGameLoad = false;
            currentDay = PlayerPrefs.GetInt(SaveKeys.StartDayNumber);
            PlayerPrefs.DeleteKey(SaveKeys.StartDayNumber);

            ForceBlackScreen();
            StartCoroutine(WaitAndStartDay(currentDay, true));
        }
    }

    void Start()
    {
        // Start() вызывается только при самой первой загрузке сцены.
        // Последующие загрузки обрабатываются в OnSceneLoaded выше.

        // Если loadedData уже обработан в OnSceneLoaded — выходим
        if (GameSaveManager.loadedData != null)
            return; // OnSceneLoaded сработает следом

        if (skipTutorialAndStartDay1)
        {
            isFirstGameLoad = false;
            StartCoroutine(WaitAndStartDay(1, true));
            return;
        }

        if (PlayerPrefs.HasKey(SaveKeys.StartDayNumber))
        {
            isFirstGameLoad = false;
            currentDay = PlayerPrefs.GetInt(SaveKeys.StartDayNumber);
            PlayerPrefs.DeleteKey(SaveKeys.StartDayNumber);
            StartCoroutine(WaitAndStartDay(currentDay, true));
        }
        else if (isFirstGameLoad)
        {
            isFirstGameLoad = false;
            currentDay = 1;
            StartCoroutine(WaitAndStartDay(1, true));
        }
    }

    private IEnumerator ResumeMidShiftRoutine()
    {
        LockPlayerInput(true);
        
        // Ждем, пока RadarManager не будет готов
        yield return new WaitUntil(() => RadarManager.Instance != null);
        
        // Точно как BigRadarLoader.RebuildAll() — читаем FlightDataManager и спавним самолёты
        RadarManager.Instance.RebuildFromFlightData();
        
        yield return new WaitForSeconds(0.5f);
        if (transitionCanvasGroup != null)
            yield return StartCoroutine(Fade(1f, 0f, 1f));

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

    public static bool isInputLocked = false;
    private void LockPlayerInput(bool isLocked)
    {
        isInputLocked = isLocked;
        if (cachedEventSystem == null) cachedEventSystem = Object.FindFirstObjectByType<EventSystem>();
        if (cachedEventSystem != null) cachedEventSystem.enabled = !isLocked;
    }

    private bool isTransitioning = false;

    private IEnumerator WaitAndStartDay(int dayNumber, bool isScreenAlreadyBlack = false)
    {
        if (isTransitioning) yield break;
        isTransitioning = true;

        yield return new WaitForSecondsRealtime(0.5f);
        
        if (dayNumber == 1)
        {
            PlayerPrefs.SetInt(SaveKeys.ReputationXP, 0);
            PlayerPrefs.Save();
        }

        StartCoroutine(DayTransitionSequence(dayNumber, isScreenAlreadyBlack));
    }


    public void TriggerGameOverCaptured()
    {
        StartCoroutine(GameOverCapturedRoutine());
    }

    private IEnumerator GameOverCapturedRoutine()
    {
        LockPlayerInput(true);
        ForceBlackScreen();
        if (transitionCanvasGroup != null) transitionCanvasGroup.alpha = 0f;
        yield return StartCoroutine(Fade(0f, 1f, 1.5f));

        string text = "<align=center><size=150%><color=#FF3030>BASE CAPTURED</color></size>\n\n<color=#888888><size=70%>COMING SOON...</size></color></align>";
        yield return StartCoroutine(TypeText(text));
    }

    public void TriggerGameWonTransition()
    {
        StartCoroutine(GameWonTransitionRoutine());
    }

    private IEnumerator GameWonTransitionRoutine()
    {
        LockPlayerInput(true);
        ForceBlackScreen();
        if (transitionCanvasGroup != null) transitionCanvasGroup.alpha = 0f;
        yield return StartCoroutine(Fade(0f, 1f, 1.5f));

        string text = "<align=center><size=150%><color=#4AF626>YOU SUCCESSFULLY SAVED THE BASE</color></size>\n\n<size=100%>END OF DEMO VERSION</size>\n\n<color=#888888><size=70%>IN DEVELOPMENT...</size></color></align>";
        yield return StartCoroutine(TypeText(text));
    }

    public void TriggerDemoEndTransition()
    {
        StartCoroutine(DemoEndTransitionRoutine());
    }

    private IEnumerator DemoEndTransitionRoutine()
    {
        LockPlayerInput(true);
        ForceBlackScreen();
        if (transitionCanvasGroup != null) transitionCanvasGroup.alpha = 0f;
        yield return StartCoroutine(Fade(0f, 1f, 1.5f));

        string text = "<align=center><size=150%><color=#4AF626>SHIFT COMPLETED</color></size>\n\n<size=100%>END OF DEMO VERSION</size>\n\n<color=#888888><size=70%>IN DEVELOPMENT...</size></color></align>";
        yield return StartCoroutine(TypeText(text));
    }

    public void EndCurrentShift()
    {
        StartCoroutine(EndShiftRoutine());
    }

    private IEnumerator EndShiftRoutine()
    {
        LockPlayerInput(true);
        diseaseDeathsThisShift = 0;

        try { EvaluateShiftResults(currentDay); } catch (System.Exception e) { Debug.LogError("Error in EvaluateShiftResults: " + e.Message + "\n" + e.StackTrace); }

        if (FlightDataManager.Instance != null)
        {
            FlightDataManager.Instance.isShiftActive = false;
        }
        
        // Обязательно сохраняем игру в конце дня, чтобы зафиксировать прогресс
        GameSaveManager.SaveGame();

        ForceBlackScreen();

        AsyncOperation preloadOp = null;
        bool useSingleSceneMode = (gameCamera != null || gameScreenRoot != null);
        if (!useSingleSceneMode && !string.IsNullOrEmpty(mainSceneName) && mainSceneName != SceneManager.GetActiveScene().name)
        {
            preloadOp = SceneManager.LoadSceneAsync(mainSceneName);
            if (preloadOp != null)
            {
                preloadOp.allowSceneActivation = false;
            }
        }

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
            int eng = PlayerPrefs.GetInt(SaveKeys.TriggerEngineer, 0);
            int econ = PlayerPrefs.GetInt(SaveKeys.BaseEmergencyEconomy, 0);
            if (eng == 0 && econ == 0) shiftXpGained = 150; 
            else if (eng == 1) shiftXpGained = 50; 
            else shiftXpGained = 50; 
        }
        else if (currentDay == 2)
        {
            shiftXpGained = 150; // Базовый опыт за прохождение второго дня
        }

        int xpPenalty = starvedToDeath + diseaseDeathsThisShift;
        int totalXpGained = shiftXpGained - xpPenalty;
        
        int startXp = PlayerPrefs.GetInt(SaveKeys.ReputationXP, 0);
        int finalXp = startXp + totalXpGained;
        if (finalXp < 0) finalXp = 0;
        PlayerPrefs.SetInt(SaveKeys.ReputationXP, finalXp);

        UnityEngine.UIElements.VisualTreeAsset uiAsset = Resources.Load<UnityEngine.UIElements.VisualTreeAsset>("UI/EndOfDay");
        GameObject uiObj = null;
        UnityEngine.UIElements.UIDocument uiDoc = null;
        UnityEngine.UIElements.Button sleepBtn = null;
        bool isSleeping = false;

        if (uiAsset != null)
        {
            Debug.Log("[EndOfDay] Found uiAsset, creating UIDocument.");
            uiObj = new GameObject("EndOfDayUI");
            uiDoc = uiObj.AddComponent<UnityEngine.UIElements.UIDocument>();

            // Create a dedicated runtime PanelSettings for EndOfDay
            // match = 1.0 → scale purely by height (best for landscape phones)
            // This avoids the "huge on wide screen" problem from width-only scaling
            var endOfDayPanel = ScriptableObject.CreateInstance<UnityEngine.UIElements.PanelSettings>();
            
            // Try to copy theme from the existing PanelSettings
            var basePanelSettings = Resources.Load<UnityEngine.UIElements.PanelSettings>("PanelSettings");
            if (basePanelSettings == null)
            {
                var allSettings = Resources.FindObjectsOfTypeAll<UnityEngine.UIElements.PanelSettings>();
                if (allSettings != null && allSettings.Length > 0) basePanelSettings = allSettings[0];
            }
            if (basePanelSettings != null)
            {
                endOfDayPanel.themeStyleSheet = basePanelSettings.themeStyleSheet;
            }
            
            endOfDayPanel.scaleMode = UnityEngine.UIElements.PanelScaleMode.ScaleWithScreenSize;
            endOfDayPanel.referenceResolution = new Vector2Int(1200, 675);
            endOfDayPanel.screenMatchMode = UnityEngine.UIElements.PanelScreenMatchMode.MatchWidthOrHeight;
            endOfDayPanel.match = 1f; // 1 = height-based scaling → correct on landscape phones
            endOfDayPanel.sortingOrder = 32001;

            uiDoc.panelSettings = endOfDayPanel;
            Debug.Log("[EndOfDay] Custom PanelSettings created (height-based scaling).");
            
            uiDoc.visualTreeAsset = uiAsset;
            uiDoc.sortingOrder = 32001; 

            if (transitionCanvasGroup != null) transitionCanvasGroup.alpha = 0f;
            if (dayText != null) dayText.text = "";

            var root = uiDoc.rootVisualElement;
            Debug.Log("[EndOfDay] Root element acquired.");

            root.Q<UnityEngine.UIElements.Label>("repVal").text = startXp.ToString();
            root.Q<UnityEngine.UIElements.Label>("dayTitle").text = $"END OF DAY {currentDay}";
            root.Q<UnityEngine.UIElements.Label>("planesVal").text = planesAccepted == 0 ? "0" : (planesAccepted > 0 ? $"+{planesAccepted}" : planesAccepted.ToString());
            root.Q<UnityEngine.UIElements.Label>("peopleVal").text = peopleChange == 0 ? "0" : (peopleChange > 0 ? $"+{peopleChange}" : peopleChange.ToString());
            root.Q<UnityEngine.UIElements.Label>("fuelVal").text = fuelChange == 0 ? "0" : (fuelChange > 0 ? $"+{fuelChange}" : fuelChange.ToString());
            root.Q<UnityEngine.UIElements.Label>("medsVal").text = medsChange == 0 ? "0" : (medsChange > 0 ? $"+{medsChange}" : medsChange.ToString());
            root.Q<UnityEngine.UIElements.Label>("foodAddedVal").text = foodAdded == 0 ? "0" : (foodAdded > 0 ? $"+{foodAdded}" : foodAdded.ToString());
            root.Q<UnityEngine.UIElements.Label>("foodEatenVal").text = $"-{foodEaten}";
            root.Q<UnityEngine.UIElements.Label>("foodRemVal").text = fdmTotalFood.ToString();
            
            root.Q<UnityEngine.UIElements.Label>("xpGainVal").text = totalXpGained == 0 ? "0" : (totalXpGained > 0 ? $"+{totalXpGained}" : totalXpGained.ToString());
            if (xpPenalty > 0)
                root.Q<UnityEngine.UIElements.Label>("xpPenaltyLabel").text = $"-{xpPenalty} XP (CASUALTIES)";
            
            root.Q<UnityEngine.UIElements.Label>("footerLeft").text = $"23:59 · DAY {currentDay:D2}";

            var mainPanel = root.Q<UnityEngine.UIElements.VisualElement>("mainPanel");
            Debug.Log("[EndOfDay] All labels mapped.");

            yield return null;
            if (mainPanel != null) mainPanel.AddToClassList("visible");

            var repBarFill = root.Q<UnityEngine.UIElements.VisualElement>("repBarFill");
            if (repBarFill != null) repBarFill.style.width = new UnityEngine.UIElements.Length(Mathf.Clamp01(startXp / 350f) * 100, UnityEngine.UIElements.LengthUnit.Percent);

            float GetPct(int val, int max) => Mathf.Clamp01(Mathf.Abs(val) / (float)max) * 100;
            root.Q<UnityEngine.UIElements.VisualElement>("planesBar").style.width = new UnityEngine.UIElements.Length(GetPct(planesAccepted, 5), UnityEngine.UIElements.LengthUnit.Percent);
            root.Q<UnityEngine.UIElements.VisualElement>("peopleBar").style.width = new UnityEngine.UIElements.Length(GetPct(peopleChange, 100), UnityEngine.UIElements.LengthUnit.Percent);
            root.Q<UnityEngine.UIElements.VisualElement>("fuelBar").style.width = new UnityEngine.UIElements.Length(GetPct(fuelChange, 500), UnityEngine.UIElements.LengthUnit.Percent);
            root.Q<UnityEngine.UIElements.VisualElement>("medsBar").style.width = new UnityEngine.UIElements.Length(GetPct(medsChange, 20), UnityEngine.UIElements.LengthUnit.Percent);
            root.Q<UnityEngine.UIElements.VisualElement>("foodAddedBar").style.width = new UnityEngine.UIElements.Length(GetPct(foodAdded, 100), UnityEngine.UIElements.LengthUnit.Percent);
            root.Q<UnityEngine.UIElements.VisualElement>("foodEatenBar").style.width = new UnityEngine.UIElements.Length(GetPct(foodEaten, 100), UnityEngine.UIElements.LengthUnit.Percent);

            var repValLabel = root.Q<UnityEngine.UIElements.Label>("repVal");
            float animTime = 1.5f;
            float elapsed = 0f;
            Debug.Log("[EndOfDay] Starting UI animation.");
            while (elapsed < animTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / animTime;
                int curXp = Mathf.RoundToInt(Mathf.Lerp(startXp, finalXp, t));
                if (repValLabel != null) repValLabel.text = curXp.ToString();
                if (repBarFill != null) repBarFill.style.width = new UnityEngine.UIElements.Length(Mathf.Clamp01(curXp / 350f) * 100, UnityEngine.UIElements.LengthUnit.Percent);
                yield return null;
            }
            if (repValLabel != null) repValLabel.text = finalXp.ToString();
            if (repBarFill != null) repBarFill.style.width = new UnityEngine.UIElements.Length(Mathf.Clamp01(finalXp / 350f) * 100, UnityEngine.UIElements.LengthUnit.Percent);

            sleepBtn = root.Q<UnityEngine.UIElements.Button>("sleepBtn");
            if (sleepBtn != null)
            {
                System.Action sleepAction = () => {
                    if (!isSleeping) {
                        isSleeping = true;
                        sleepBtn.text = "▶  ZZZ... SLEEPING...  ◀";
                        sleepBtn.AddToClassList("sleeping");
                    }
                };

                sleepBtn.clicked += sleepAction;
                sleepBtn.RegisterCallback<UnityEngine.UIElements.PointerDownEvent>(evt => sleepAction());
            }
            Debug.Log("[EndOfDay] UI Setup complete, waiting for sleep.");
        }
        else
        {
            Debug.Log("[EndOfDay] uiAsset is NULL. Running fallback text.");
            yield return StartCoroutine(TypeText("END OF DAY\n\nCLICK TO CONTINUE"));
        }

        if (uiAsset != null)
        {
            yield return new WaitUntil(() => isSleeping);
        }
        else
        {
            yield return new WaitUntil(() => 
                (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame) ||
                (UnityEngine.InputSystem.Touchscreen.current != null && UnityEngine.InputSystem.Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            );
        }

        if (uiObj != null) Destroy(uiObj);
        if (dayText != null) dayText.text = "";
        
        if (transitionCanvasGroup != null) transitionCanvasGroup.alpha = 1f;

        if (currentDay2Outcome == Day2Outcome.Lost_NoSF)
        {
            TriggerGameOverCaptured();
            yield break;
        }
        else if (currentDay2Outcome == Day2Outcome.Won)
        {
            TriggerGameWonTransition();
            yield break;
        }

        if (currentDay == 2)
        {
            TriggerDemoEndTransition();
            yield break;
        }

        currentDay++;
        PlayerPrefs.SetInt(SaveKeys.StartDayNumber, currentDay);
        PlayerPrefs.Save();

        if (useSingleSceneMode)
        {
            if (ButtonSoundManager.instance != null) ButtonSoundManager.instance.StopAllSounds();
            
            if (gameScreenRoot != null) gameScreenRoot.SetActive(true);
            if (gameCamera != null)
            {
                if (Camera.main != null && Camera.main != gameCamera) Camera.main.gameObject.SetActive(false);
                gameCamera.gameObject.SetActive(true);
            }
            if (currentStoryRoot != null) currentStoryRoot.SetActive(false);
            
            StartCoroutine(WaitAndStartDay(currentDay, true));
        }
        else if (preloadOp != null)
        {
            if (ButtonSoundManager.instance != null) ButtonSoundManager.instance.StopAllSounds();
            preloadOp.allowSceneActivation = true;
            while (!preloadOp.isDone) yield return null;
        }
        else if (!string.IsNullOrEmpty(mainSceneName) && mainSceneName != SceneManager.GetActiveScene().name)
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
                if (flight.callsign == Callsigns.TR_404 && flight.approved) letRefugeesIn = true;
            }

            int finalFuel = FlightDataManager.Instance.totalFuel;
            bool fuelTargetMet = (finalFuel >= 400);

            // Save variables for Day 2 in memory
            PlayerPrefs.SetInt(SaveKeys.BaseEmergencyEconomy, fuelTargetMet ? 0 : 1);
            PlayerPrefs.SetInt(SaveKeys.TriggerEngineer, letRefugeesIn ? 1 : 0);
            PlayerPrefs.Save();

            if (letRefugeesIn)
            {
                AegisMailApp.ReceiveNewEmail(new EmailData
                {
                    sender = "Chief Engineer Mitchell",
                    subject = "Thank you from the survivors",
                    date = "20.08.2038",
                    body = "Dispatcher, I was on board TR-404. You saved my life and the lives of 64 others when our engines were failing. The Director is furious about the fuel shortage, but I've already set up a workspace in the hangar. I will do everything I can to help you optimize the base systems. We owe you our lives.\n\nI have requested a drop of special equipment to help us. To ensure it's not intercepted by marauders, the pilot will give an encrypted code. Put it in the Decryption Machine (shift -8). The real transport will decrypt to the word SAFE."
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
                    if (flight.callsign == Callsigns.GE_99) acceptedEQ = true;
                    if (flight.callsign == Callsigns.QY_01) acceptedMeds = true;
                    if (flight.callsign == Callsigns.GE_55) acceptedFuel = true;
                }
            }

            int engineerTrigger = PlayerPrefs.GetInt(SaveKeys.TriggerEngineer, 0);
            int emergencyEcon = PlayerPrefs.GetInt(SaveKeys.BaseEmergencyEconomy, 0);
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

                int medsNeeded = Mathf.CeilToInt(FlightDataManager.Instance.totalPeople / 15f);
                int medsUsed = Mathf.Min(medsNeeded, FlightDataManager.Instance.totalMedicines);
                int peopleSaved = medsUsed * 15;
                
                int diseaseDeaths = FlightDataManager.Instance.totalPeople - peopleSaved;
                if (diseaseDeaths < 0) diseaseDeaths = 0;

                // Потребляем медикаменты на лечение
                FlightDataManager.Instance.totalMedicines -= medsUsed;

                string emailSubject = "";
                string emailBody = "";

                if (emergencyEcon == 1) // Branch B-1 (No Fuel on Day 1)
                {
                    if (acceptedFuel)
                    {
                        if (diseaseDeaths == 0)
                        {
                            PlayerPrefs.SetInt(SaveKeys.BaseEmergencyEconomy, 0);
                            emailSubject = "Good job";
                            emailBody = "Good job, Dispatcher. You managed to secure both fuel and medical supplies. The pathogen is suppressed. We are entering open mode without interference since the power grid is stable. Keep up the good work.";
                        }
                        else
                        {
                            PlayerPrefs.SetInt("BaseEmergencyEconomy", 0);
                            emailSubject = "Tragic losses";
                            emailBody = $"We lost people today because we didn't have enough medical supplies to save everyone. We lost {diseaseDeaths} people to the pathogen. At least you secured the fuel, so the power grid is stable and the interference is gone.";
                        }
                    }
                    else
                    {
                        if (diseaseDeaths == 0)
                        {
                            PlayerPrefs.SetInt(SaveKeys.BaseEmergencyEconomy, 1);
                            emailSubject = "CRITICAL FUEL SHORTAGE";
                            emailBody = "You idiot! We had enough meds to save lives from the pathogen, but we have a critical fuel shortage! The generators are dying, the radar is going black, and the interference will only get worse. How are we supposed to survive in the dark?";
                        }
                        else
                        {
                            PlayerPrefs.SetInt(SaveKeys.BaseEmergencyEconomy, 1);
                            emailSubject = "DISASTER";
                            emailBody = $"You are an absolute failure. You failed to bring enough fuel, and we didn't have enough medicines. We lost {diseaseDeaths} people to the pathogen, and the generators are completely dead. You are officially relieved of duty... though there is no one left to take your place.";
                        }
                    }
                }
                else // Branch B-2 (Fuel secured on Day 1)
                {
                    if (diseaseDeaths == 0)
                    {
                        emailSubject = "Crisis Averted";
                        emailBody = "Excellent work. We had enough medical supplies to treat all the infected. Everyone survived the quarantine. Keep the skies clear, open mode begins.";
                    }
                    else if (diseaseDeaths < FlightDataManager.Instance.totalPeople * 0.5f)
                    {
                        emailSubject = "Partial Success";
                        emailBody = $"We didn't have enough medicine to save everyone. We lost {diseaseDeaths} people to the pathogen. It could have been worse, but it's still a tragedy.";
                    }
                    else
                    {
                        emailSubject = "YOU ARE FIRED";
                        emailBody = $"You idiot. A massive part of the base died because we lacked medical supplies. We lost {diseaseDeaths} people today. You are officially relieved of your duties as Dispatcher. Do not return to the control tower.";
                    }
                }

                if (diseaseDeaths > 0)
                {
                    diseaseDeathsThisShift = diseaseDeaths;
                    FlightDataManager.Instance.totalPeople -= diseaseDeaths;
                    if (FlightDataManager.Instance.totalPeople < 0) FlightDataManager.Instance.totalPeople = 0;
                }

                AegisMailApp.ReceiveNewEmail(new EmailData {
                    sender = "Director Reed",
                    subject = emailSubject,
                    date = "21.08.2038",
                    body = emailBody
                });
            }
            else // Branch A
            {
                bool acceptedFriendSF = false;
                bool acceptedEnemySF = false;
                foreach (var flight in FlightDataManager.Instance.savedFlights)
                {
                    if (flight.approved)
                    {
                        if (flight.callsign == Callsigns.TR_11) acceptedFriendSF = true;
                        if (flight.callsign == Callsigns.TR_88) acceptedEnemySF = true;
                    }
                }

                if (!acceptedFriendSF && !acceptedEnemySF)
                {
                    currentDay2Outcome = Day2Outcome.Lost_NoSF;
                }
                else if (acceptedFriendSF)
                {
                    currentDay2Outcome = Day2Outcome.Won;
                    
                    AegisMailApp.ReceiveNewEmail(new EmailData
                    {
                        sender = "Director Reed",
                        subject = "Well done, you protected us",
                        date = "21.08.2038",
                        body = "Dispatcher. The reinforcements you let in secured the perimeter just in time. The marauders have been repelled. You saved the base. Great job."
                    });
                }
            }

            PlayerPrefs.SetInt(SaveKeys.Day3Slots, day3Slots);
            PlayerPrefs.Save();
        }
    }

    private IEnumerator DayTransitionSequence(int dayNumber, bool isScreenAlreadyBlack)
    {


        LockPlayerInput(true);
        ForceBlackScreen(); 

        if (!isScreenAlreadyBlack) yield return StartCoroutine(Fade(0f, 1f, 1.0f));

        if (FlightDataManager.Instance != null)
        {
            if (dayNumber == 1)
            {
                int randomPeople = UnityEngine.Random.Range(105, 126); // 105 to 125 inclusive
                int randomFuel = UnityEngine.Random.Range(250, 381);   // 250 to 380 inclusive
                int randomMeds = UnityEngine.Random.Range(0, 3);       // 0 to 2 inclusive
                
                // Чтобы на 2-й день без дополнительных поставок еды умерло 3-4 человека (нехватка 6-8 еды).
                // Считаем, что игрок примет беженцев (+65 человек). 
                // Суммарное потребление за 2 дня примерно равно количеству людей.
                int calculatedFood = (randomPeople + 65) - 7;

                FlightDataManager.Instance.ResetForNewShift(randomFuel, calculatedFood, randomPeople, randomMeds);
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
                FlightDataManager.Instance.maxPlanes = PlayerPrefs.GetInt(SaveKeys.Day3Slots, 3);

                if (marauderAmbienceRoot != null) marauderAmbienceRoot.SetActive(false);
                if (crashedPlaneRadarIcon != null) crashedPlaneRadarIcon.SetActive(false);
            }
        }

        string displayDate = (18 + dayNumber) + ".08.2038";
        if (dayText != null) dayText.text = ""; // Ensure old text is cleared

        ShiftIntroBuilder builder = Object.FindFirstObjectByType<ShiftIntroBuilder>();
        if (builder == null) 
        {
            builder = new GameObject("ShiftIntroBuilder").AddComponent<ShiftIntroBuilder>();
        }

        yield return StartCoroutine(builder.PlaySequence(dayText.transform.parent, dayText.font, dayNumber, displayDate));

        if (dayNumber == 1) SendDay1Directives();
        else if (dayNumber == 2) SendDay2Directives();

        yield return StartCoroutine(Fade(1f, 0f, 1.5f));

        if (transitionScreen != null) transitionScreen.SetActive(false);
        if (transitionCanvasGroup != null) transitionCanvasGroup.blocksRaycasts = false;

        LockPlayerInput(false);

        if (FlightDataManager.Instance != null)
        {
            // Убедимся, что очистили очередь, если вдруг был перезапуск или накладка
            FlightDataManager.Instance.scriptedFlightsQueue.Clear();
            FlightDataManager.Instance.scriptedDelaysQueue.Clear();

            // Убираем все самолёты прошлого дня с радара (маркеры и маршруты исчезнут вместе с ними)
            ClearAllRadarPlanes();

            FlightDataManager.Instance.StartDaySpawning(dayNumber);
        }

        isTransitioning = false;
        if (HintManager.Instance != null) HintManager.Instance.TriggerEmailHint();
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

    private void ClearAllRadarPlanes()
    {
        // Return all live UIAirplane objects back to the pool (this also destroys their waypoint markers and route segments)
        UIAirplane[] activePlanes = Object.FindObjectsByType<UIAirplane>(FindObjectsSortMode.None);
        foreach (var plane in activePlanes)
        {
            if (plane == null) continue;
            if (AirplaneSpawner.Instance != null)
                AirplaneSpawner.Instance.ReturnPlaneToPool(plane);
            else
                plane.gameObject.SetActive(false);
        }

        // NOTE: savedFlights is NOT cleared here.
        // ResetForNewShift() already rebuilt savedFlights to contain only the preserved
        // (landed + not yet departed) planes before this method is called.
        // Clearing here would destroy all data about planes waiting to depart on the next day.
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
        bool letRefugeesIn = PlayerPrefs.GetInt(SaveKeys.TriggerEngineer, 0) == 1;
        bool fuelTargetMet = PlayerPrefs.GetInt(SaveKeys.BaseEmergencyEconomy, 0) == 0;

        EmailData day2Email = new EmailData();
        day2Email.date = "20.08.2038";

        if (!letRefugeesIn && fuelTargetMet)
        {
            // Branch A: Marauders
            if (crashedPlaneRadarIcon != null) crashedPlaneRadarIcon.SetActive(true);
            if (marauderAmbienceRoot != null) marauderAmbienceRoot.SetActive(true);
            
            day2Email.sender = "Director Reed";
            day2Email.subject = "SECURITY ALERT — PERIMETER BREACH";
            day2Email.body = "ATS, listen carefully. That passenger plane you turned away yesterday crashed five miles outside the perimeter. The burning wreckage served as a beacon for local looters. Now these looters have spotted our gates and are actively trying to breach the outer fence. Our fighters will fight with all their might, but they’re unlikely to hold out for long—there are too many of them.\n\nUsing my old connections, I convinced one of our friendly bases to send us reinforcements; a heavy transport carrying a special forces unit is on its way. You MUST immediately clear a landing zone for them. If they don’t secure the perimeter before nightfall, we’ll all be killed.\n\nATTENTION: The enemy might try to send a fake transport. When asked about their cargo, the REAL transport will say the password 'ECHO'. The enemy will say something similar. Do NOT let the enemy land!";
        }
        else if (!letRefugeesIn && !fuelTargetMet)
        {
            // Branch A-2: Marauders + Blackout
            if (crashedPlaneRadarIcon != null) crashedPlaneRadarIcon.SetActive(true);
            if (marauderAmbienceRoot != null) marauderAmbienceRoot.SetActive(true);
            
            day2Email.sender = "Director Reed";
            day2Email.subject = "PERIMETER BREACH & POWER FAILURE";
            day2Email.body = "You failed the simplest task yesterday. The grid is dying, and we are sitting in the dark.\n\nTo make matters worse, that passenger plane you turned away crashed five miles outside the perimeter. The burning wreckage acted like a beacon for local scavengers. Now, marauders are using our blackout to their advantage and are actively breaching the external gates.\n\nYOUR DIRECTIVE:\n> You have two critical jobs today. First, using my old connections, I convinced one of our friendly bases to send us reinforcements; a heavy transport carrying a special forces unit is on its way. You MUST immediately clear a landing zone for them. Second, get a Fuel transport down here before your radar shuts off completely.\n\nDo not waste time on anything else. If you fail to bring in the ops team or the fuel, we are all dead.\n\nATTENTION: The enemy might try to send a fake transport. When asked about their cargo, the REAL transport will say the password 'ECHO'. The enemy will say something similar. Do NOT let the enemy land!";
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