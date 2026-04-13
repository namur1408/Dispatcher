using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class RadarZoomManager : MonoBehaviour
{
    [Header("Zoom settings")]
    public RectTransform radarContent;
    public float zoomSpeed = 0.001f;
    public float mobileZoomSpeed = 0.005f; 
    public float minZoom = 0.5f;
    public float maxZoom = 2.0f;

    public float panSpeed = 1f;
    public float maxPanRadius = 4000f;

    void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

    void Update()
    {
        if (Touch.activeTouches.Count > 0)
        {
            HandleMobileInput();
        }
        else if (Mouse.current != null)
        {
            HandleZoom();
            HandlePan();
        }

        HandleResetView();
        ClampPosition();
    }

    void HandleMobileInput()
    {
        if (Touch.activeTouches.Count == 1)
        {
            var touch = Touch.activeTouches[0];
            if (touch.phase == UnityEngine.InputSystem.TouchPhase.Moved)
            {
                radarContent.anchoredPosition += touch.delta * panSpeed;
            }
        }
        else if (Touch.activeTouches.Count == 2)
        {
            var touch0 = Touch.activeTouches[0];
            var touch1 = Touch.activeTouches[1];

            Vector2 touch0Prev = touch0.screenPosition - touch0.delta;
            Vector2 touch1Prev = touch1.screenPosition - touch1.delta;
            float prevMagnitude = (touch0Prev - touch1Prev).magnitude;
            float currentMagnitude = (touch0.screenPosition - touch1.screenPosition).magnitude;
            float difference = currentMagnitude - prevMagnitude;

            ApplyZoom(difference * mobileZoomSpeed);
        }
    }

    void HandleZoom()
    {
        float scroll = Mouse.current.scroll.ReadValue().y;
        if (scroll != 0) ApplyZoom(scroll * zoomSpeed);
    }

    void ApplyZoom(float zoomDelta)
    {
        float currentScale = radarContent.localScale.x;
        float newScale = Mathf.Clamp(currentScale + zoomDelta, minZoom, maxZoom);

        if (currentScale != newScale)
        {
            float scaleRatio = newScale / currentScale;
            radarContent.localScale = new Vector3(newScale, newScale, 1f);
            radarContent.anchoredPosition *= scaleRatio;
        }
    }

    void HandlePan()
    {
        if (Mouse.current.rightButton.isPressed)
        {
            radarContent.anchoredPosition += Mouse.current.delta.ReadValue() * panSpeed;
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