using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ReturnToDesk : MonoBehaviour
{
    [SerializeField] private string mainSceneName = "SampleScene";

    void Start()
    {
        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveListener(GoBackToMainScene);
            btn.onClick.AddListener(GoBackToMainScene);
        }
    }

    public void GoBackToMainScene()
    {
        if (RadarManager.Instance != null) RadarManager.Instance.SaveToGlobalManager();
        if (ButtonSoundManager.instance != null) ButtonSoundManager.instance.StopAllSounds();
        SceneManager.LoadScene(mainSceneName);
    }
}