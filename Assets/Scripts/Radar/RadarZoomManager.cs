using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using UnityEngine.EventSystems;

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

    private System.Collections.Generic.HashSet<int> blockedTouches = new System.Collections.Generic.HashSet<int>();
    private System.Collections.Generic.List<Touch> validTouches = new System.Collections.Generic.List<Touch>();
    // Cached to avoid GC allocation every frame
    private PointerEventData cachedPointerEventData;
    private System.Collections.Generic.List<RaycastResult> cachedRaycastResults = new System.Collections.Generic.List<RaycastResult>();

    void HandleMobileInput()
    {
        // Clean up blocked touches that are no longer active
        blockedTouches.RemoveWhere(id => {
            bool exists = false;
            foreach (var t in Touch.activeTouches) if (t.touchId == id) exists = true;
            return !exists;
        });

        // Mark touches that started on UI as blocked
        foreach (var touch in Touch.activeTouches)
        {
            if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                if (IsPointerOverUIWindow(touch.screenPosition))
                {
                    blockedTouches.Add(touch.touchId);
                }
            }
        }

        // Filter out blocked touches (reuse cached list)
        validTouches.Clear();
        foreach (var t in Touch.activeTouches)
        {
            if (!blockedTouches.Contains(t.touchId)) validTouches.Add(t);
        }

        if (validTouches.Count == 1)
        {
            var touch = validTouches[0];
            if (touch.phase == UnityEngine.InputSystem.TouchPhase.Moved)
            {
                radarContent.anchoredPosition += touch.delta * panSpeed;
            }
        }
        else if (validTouches.Count == 2)
        {
            var touch0 = validTouches[0];
            var touch1 = validTouches[1];

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
        if (scroll != 0)
        {
            if (!IsPointerOverUIWindow(Mouse.current.position.ReadValue()))
            {
                ApplyZoom(scroll * zoomSpeed);
            }
        }
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

    private bool isMousePanBlocked = false;

    void HandlePan()
    {
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            isMousePanBlocked = IsPointerOverUIWindow(Mouse.current.position.ReadValue());
        }

        if (Mouse.current.rightButton.wasReleasedThisFrame)
        {
            isMousePanBlocked = false;
        }

        if (Mouse.current.rightButton.isPressed && !isMousePanBlocked)
        {
            radarContent.anchoredPosition += Mouse.current.delta.ReadValue() * panSpeed;
        }
    }

    private bool IsPointerOverUIWindow(Vector2 screenPosition)
    {
        if (EventSystem.current == null) return false;

        // Reuse cached objects to avoid per-frame GC allocations
        if (cachedPointerEventData == null)
            cachedPointerEventData = new PointerEventData(EventSystem.current);
        cachedPointerEventData.position = screenPosition;
        cachedRaycastResults.Clear();
        EventSystem.current.RaycastAll(cachedPointerEventData, cachedRaycastResults);
        
        foreach (var result in cachedRaycastResults)
        {
            // If we hit the radar background, it's a valid pan
            if (result.gameObject.GetComponent<RadarScreenClicker>() != null) return false;

            // If we hit anything inside the radar content (like airplanes), it's a valid pan
            if (radarContent != null && result.gameObject.transform.IsChildOf(radarContent)) continue;

            // If we hit a runway, it's also valid to pan from it
            if (result.gameObject.GetComponentInParent<Runway>() != null) continue;

            // Explicitly check if this hit is a UI control we want to block on
            Transform t = result.gameObject.transform;
            bool isUI = false;
            while (t != null)
            {
                if (t.GetComponent<UnityEngine.UI.ScrollRect>() != null ||
                    t.GetComponent<WindowTopResizer>() != null ||
                    t.GetComponent<UnityEngine.UI.Button>() != null ||
                    t.name.IndexOf("Window", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    t.name.IndexOf("Panel", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    t.name.IndexOf("Bottom", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    isUI = true;
                    break;
                }
                t = t.parent;
            }

            if (isUI) return true;
        }
        
        return false;
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