using UnityEngine;
using TMPro;

public class TimeScaleUI : MonoBehaviour
{
    public TextMeshProUGUI speedText;

    private float lastSpeed = -1f;

    void Update()
    {
        if (TimeScaleController.Instance != null && speedText != null)
        {
            float speed = TimeScaleController.Instance.GetCurrentSpeed();
            if (speed != lastSpeed)
            {
                lastSpeed = speed;
                speedText.text = $"<color=#00FF41>SPEED: {speed}x</color>";
            }
        }
    }
}
