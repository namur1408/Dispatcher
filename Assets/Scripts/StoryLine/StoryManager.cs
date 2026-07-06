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
        // --- LOADING A SAVE (Continue) ---
        // StoryManager - DontDestroyOnLoad, Start() is called only once on the first scene.
        // All subsequent scene loadings end up here in OnSceneLoaded.
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

        // --- TRANSITION BETWEEN DAYS ---
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
        // Start() is called only the very first time the scene is loaded.
        // Subsequent loads are handled in OnSceneLoaded above.

        // If loadedData has already been processed in OnSceneLoaded, exit
        if (GameSaveManager.loadedData != null)
            return; // OnSceneLoaded will work next

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
        
        // We wait until RadarManager is ready
        yield return new WaitUntil(() => RadarManager.Instance != null);
        
        // Exactly like BigRadarLoader.RebuildAll() - read FlightDataManager and spawn planes
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
        
        // Be sure to save the game at the end of the day to record your progress.
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

        int shiftXpGained = DayLogicProvider.GetDayLogic(currentDay).GetBaseXP();

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

        IDayLogic currentDayLogic = DayLogicProvider.GetDayLogic(currentDay);
        EndOfDayResult result = currentDayLogic.GetEndOfDayResult();

        if (result == EndOfDayResult.GameOverCaptured)
        {
            TriggerGameOverCaptured();
            yield break;
        }
        else if (result == EndOfDayResult.GameWon)
        {
            TriggerGameWonTransition();
            yield break;
        }
        else if (result == EndOfDayResult.DemoEnd)
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
        IDayLogic dayLogic = DayLogicProvider.GetDayLogic(shiftDay);
        diseaseDeathsThisShift = dayLogic.EvaluateShift();
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
                
                // So that on the 2nd day, without additional food supplies, 3-4 people would die (shortage of 6-8 food).
                // We believe that the player will accept refugees (+65 people). 
                // The total consumption over 2 days is approximately equal to the number of people.
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

        IDayLogic dayLogic = DayLogicProvider.GetDayLogic(dayNumber);
        dayLogic.SendMorningDirectives();

        yield return StartCoroutine(Fade(1f, 0f, 1.5f));

        if (transitionScreen != null) transitionScreen.SetActive(false);
        if (transitionCanvasGroup != null) transitionCanvasGroup.blocksRaycasts = false;

        LockPlayerInput(false);

        if (FlightDataManager.Instance != null)
        {
            // Let's make sure we clear the queue in case there was a restart or a problem
            FlightDataManager.Instance.scriptedFlightsQueue.Clear();
            FlightDataManager.Instance.scriptedDelaysQueue.Clear();

            // We remove all planes of the previous day from the radar (markers and routes will disappear along with them)
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