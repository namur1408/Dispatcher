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
    public float maxPanRadius = 4000f;

    void Update()
    {
        {
            if (Mouse.current == null) return;

            HandleZoom();
            HandlePan();
            HandleResetView();
            ClampPosition();
        }
    }

    void HandleZoom()
    {
        float scroll = Mouse.current.scroll.ReadValue().y;
        if (scroll != 0)
        {
            float currentScale = radarContent.localScale.x;
            float newScale = Mathf.Clamp(currentScale + scroll * zoomSpeed, minZoom, maxZoom);
            if (currentScale != newScale)
            {
                float scaleRatio = newScale / currentScale;
                radarContent.localScale = new Vector3(newScale, newScale, 1f);
                radarContent.anchoredPosition *= scaleRatio;
            }
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

    void ClampPosition()
    {
        float zoomRatio = Mathf.InverseLerp(minZoom, maxZoom, radarContent.localScale.x);
        float currentLimit = Mathf.Lerp(0f, maxPanRadius, zoomRatio);
        Vector2 currentPos = radarContent.anchoredPosition;
        if (currentPos.magnitude > currentLimit)
        {
            radarContent.anchoredPosition = currentPos.normalized * currentLimit;
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