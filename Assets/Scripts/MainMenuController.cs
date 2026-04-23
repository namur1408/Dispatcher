using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    // Название сцены, куда переходить при нажатии START
    // Убедись что оно совпадает с именем в Build Settings!
    public string gameSceneName = "SampleScene";

    public void OnStartClicked()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    public void OnExitClicked()
    {
        Application.Quit();

        // Это нужно для тестирования в редакторе —
        // в билде Application.Quit() сработает сам
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}