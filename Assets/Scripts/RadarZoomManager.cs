using UnityEngine;
using UnityEngine.InputSystem; 

public class RadarZoomManager : MonoBehaviour
{
    [Header("Zoom settings")]
    public RectTransform radarContent;
    public float zoomSpeed = 0.001f;   
    public float minZoom = 0.5f;
    public float maxZoom = 2.0f;

    public float panSpeed = 1f;

    void Update()
    {
        {
            if (Mouse.current == null) return;

            HandleZoom();
            HandlePan();
            HandleResetView();
        }
    }

    void HandleZoom()
    {
        float scroll = Mouse.current.scroll.ReadValue().y;
        if (scroll != 0)
        {
            float currentScale = radarContent.localScale.x;
            float newScale = Mathf.Clamp(currentScale + scroll * zoomSpeed, minZoom, maxZoom);
            radarContent.localScale = new Vector3(newScale, newScale, 1f);
        }
    }

    void HandlePan()
    {
        if (Mouse.current.rightButton.isPressed)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();

            radarContent.anchoredPosition += mouseDelta * panSpeed;
        }
    }

    void HandleResetView()
    {
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            radarContent.anchoredPosition = Vector2.zero;
        }
    }
}