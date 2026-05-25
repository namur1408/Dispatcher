using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class StoryDebugWindow : EditorWindow
{
    private int startDay = 1;
    private bool skipTutorial = false;
    private bool day1_LetRefugeesIn = false;
    private bool day1_MetFuelTarget = true;
    private int reputationXP = 0;
    private int day3Slots = 3;

    [MenuItem("Tools/Story Debugger")]
    public static void ShowWindow()
    {
        GetWindow<StoryDebugWindow>("Story Debugger");
    }

    private void OnEnable()
    {
        LoadPrefs();
    }

    private void LoadPrefs()
    {
        startDay = PlayerPrefs.GetInt("StartDayNumber", 1);
        skipTutorial = PlayerPrefs.GetInt("SkipTutorial", 0) == 1;
        day1_LetRefugeesIn = PlayerPrefs.GetInt("Trigger_Engineer", 0) == 1;
        day1_MetFuelTarget = PlayerPrefs.GetInt("BaseEmergencyEconomy", 0) == 0;
        reputationXP = PlayerPrefs.GetInt("ReputationXP", 0);
        day3Slots = PlayerPrefs.GetInt("Day3Slots", 3);
    }

    private void SavePrefs()
    {
        PlayerPrefs.SetInt("StartDayNumber", startDay);
        PlayerPrefs.SetInt("SkipTutorial", skipTutorial ? 1 : 0);
        
        PlayerPrefs.SetInt("Trigger_Engineer", day1_LetRefugeesIn ? 1 : 0);
        PlayerPrefs.SetInt("BaseEmergencyEconomy", day1_MetFuelTarget ? 0 : 1); 
        
        PlayerPrefs.SetInt("ReputationXP", reputationXP);
        PlayerPrefs.SetInt("Day3Slots", day3Slots);
        PlayerPrefs.Save();
    }

    private void OnGUI()
    {
        GUILayout.Label("Game Progress Settings", EditorStyles.boldLabel);
        
        EditorGUILayout.Space();
        
        startDay = EditorGUILayout.IntSlider("Start Day Number", startDay, 1, 10);
        skipTutorial = EditorGUILayout.Toggle("Skip Tutorial", skipTutorial);

        EditorGUILayout.Space();
        GUILayout.Label("Day 1 Story Choices (Affects plot from Day 2)", EditorStyles.boldLabel);
        day1_LetRefugeesIn = EditorGUILayout.Toggle("Accepted Refugees (TR-404)", day1_LetRefugeesIn);
        day1_MetFuelTarget = EditorGUILayout.Toggle("Met Fuel Target (>400)", day1_MetFuelTarget);

        EditorGUILayout.Space();
        GUILayout.Label("General Stats", EditorStyles.boldLabel);
        reputationXP = EditorGUILayout.IntField("Reputation XP", reputationXP);
        day3Slots = EditorGUILayout.IntSlider("Day 3 Radar Slots", day3Slots, 1, 5);

        EditorGUILayout.Space();
        EditorGUILayout.Space();
        
        GUI.backgroundColor = new Color(0.2f, 0.9f, 0.2f);
        if (GUILayout.Button("1. APPLY & PLAY (BYPASS MAIN MENU)", GUILayout.Height(40)))
        {
            SavePrefs();
            DeleteSaveFile();
            Debug.Log("Story settings applied. Force-starting game scene...");
            
            if (!Application.isPlaying)
            {
                EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
                EditorApplication.isPlaying = true;
            }
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space();

        if (GUILayout.Button("Apply Settings (Don't Start)"))
        {
            SavePrefs();
            DeleteSaveFile();
            Debug.Log("Story settings applied.");
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Reset Progress (Clear PlayerPrefs)"))
        {
            PlayerPrefs.DeleteKey("StartDayNumber");
            PlayerPrefs.DeleteKey("SkipTutorial");
            PlayerPrefs.DeleteKey("Trigger_Engineer");
            PlayerPrefs.DeleteKey("BaseEmergencyEconomy");
            PlayerPrefs.DeleteKey("ReputationXP");
            PlayerPrefs.DeleteKey("Day3Slots");
            PlayerPrefs.DeleteKey("GameSaveData");
            PlayerPrefs.Save();
            
            DeleteSaveFile();
            
            LoadPrefs();
            Debug.Log("Progress cleared. Game will start from Day 1 with default settings.");
        }
    }
    
    private void DeleteSaveFile()
    {
        string savePath = Application.persistentDataPath + "/savedata.json";
        if (System.IO.File.Exists(savePath))
        {
            System.IO.File.Delete(savePath);
        }
    }
}
