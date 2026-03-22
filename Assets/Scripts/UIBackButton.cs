using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIBackButton : MonoBehaviour
{
    [SerializeField] private string mainSceneName = "SampleScene";

    void Start()
    {
        Button btn = GetComponent<Button>();
        if (btn != null) btn.onClick.AddListener(Return);
    }

    void Return()
    {
        SceneManager.LoadScene(mainSceneName);
    }
}