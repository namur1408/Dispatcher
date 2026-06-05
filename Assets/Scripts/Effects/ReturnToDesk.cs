using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Events;

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

    [Header("Single Scene Return Mode (Optional)")]
    public Camera returnCamera;
    public GameObject returnScreenRoot;
    [Tooltip("Target screen root to disable when returning (e.g., RadarScreen)")]
    public GameObject currentScreenRoot;
    
    public UnityEvent onReturn;

    public void GoBackToMainScene()
    {
        if (RadarManager.Instance != null) RadarManager.Instance.SaveToGlobalManager();
        if (ButtonSoundManager.instance != null) ButtonSoundManager.instance.StopAllSounds();

        if (returnCamera != null || returnScreenRoot != null)
        {
            if (returnScreenRoot != null)
            {
                returnScreenRoot.SetActive(true);
                // Restore canvas visibility and interactivity (in case keepCurrentAlive was used)
                CanvasGroup cg = returnScreenRoot.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    cg.alpha = 1f;
                    cg.blocksRaycasts = true;
                    cg.interactable = true;
                }
                // Re-enable ALL GraphicRaycasters so clicks work again
                UnityEngine.UI.GraphicRaycaster[] allRaycasters = returnScreenRoot.GetComponentsInChildren<UnityEngine.UI.GraphicRaycaster>(true);
                foreach (var gr in allRaycasters) gr.enabled = true;
            }
            if (returnCamera != null)
            {
                returnCamera.gameObject.SetActive(true);
            }
            if (currentScreenRoot != null) currentScreenRoot.SetActive(false);

            ZoomReturnManager zrm = FindAnyObjectByType<ZoomReturnManager>();
            if (zrm != null) zrm.TriggerReturnAnimation();
        }
        else
        {
            SceneManager.LoadScene(mainSceneName);
        }
        
        onReturn?.Invoke();
    }
}