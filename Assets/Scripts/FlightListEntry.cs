using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class FlightListEntry : MonoBehaviour
{
    public TextMeshProUGUI callsignText;
    private UIAirplane linkedPlane;

    public void Setup(UIAirplane plane)
    {
        linkedPlane = plane;
        callsignText.text = plane.callsignText.text;

        GetComponent<Button>().onClick.AddListener(OnButtonClick);
    }

    void OnButtonClick()
    {
        RadarManager.Instance.SelectAirplane(linkedPlane);
    }

    void Update()
    {
        if (linkedPlane == null)
        {
            Destroy(gameObject);
        }
    }
}