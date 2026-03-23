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

            // Если масштаб действительно изменился (не уперся в лимиты)
            if (currentScale != newScale)
            {
                // Вычисляем разницу (коэффициент) между новым и старым масштабом
                float scaleRatio = newScale / currentScale;

                // Применяем новый масштаб
                radarContent.localScale = new Vector3(newScale, newScale, 1f);

                // МАГИЯ: Умножаем текущую позицию на этот коэффициент!
                // Это компенсирует сдвиг и оставит центр экрана ровно на том же месте карты.
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