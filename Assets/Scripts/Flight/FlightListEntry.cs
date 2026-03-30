using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class FlightListEntry : MonoBehaviour
{
    public TextMeshProUGUI callsignText;
    private UIAirplane linkedPlane;
    public bool isStaticEntry = false;

    public void Setup(UIAirplane plane)
    {
        linkedPlane = plane;
        if (plane != null && plane.callsignText != null)
        {
            callsignText.text = plane.callsignText.text;
        }

        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(OnButtonClick);
        }
    }

    void OnButtonClick()
    {
        if (RadarManager.Instance != null && linkedPlane != null)
        {
            RadarManager.Instance.SelectAirplane(linkedPlane);
        }
    }

    void Update()
    {
        if (isStaticEntry) return;
        if (linkedPlane == null)
        {
            Destroy(gameObject);
        }
    }
}