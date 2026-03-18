using UnityEngine;
using UnityEngine.SceneManagement;

public class ReturnToDesk : MonoBehaviour
{
    [Header("Navigation Settings")]
    [SerializeField] private string mainSceneName = "SampleScene";

    // This method will be linked to the UI Button
    public void GoBackToMainScene()
    {
        SceneManager.LoadScene(mainSceneName);
    }
}