using UnityEngine;
using TMPro;

public class TimeScaleUI : MonoBehaviour
{
    public TextMeshProUGUI speedText;

    void Update()
    {
        if (TimeScaleController.Instance != null && speedText != null)
        {
            int level = TimeScaleController.Instance.GetSpeedLevel();
            float speed = TimeScaleController.Instance.GetCurrentSpeed();

            speedText.text = $"<color=#00FF41>SPEED: {speed}x</color>";
        }
    }
}
