using UnityEngine;
using UnityEngine.SceneManagement;

public class ReturnToDesk : MonoBehaviour
{
    [SerializeField] private string mainSceneName = "SampleScene";
    public void GoBackToMainScene()
    {
        SceneManager.LoadScene(mainSceneName);
    }
}