using UnityEngine;
using UnityEngine.InputSystem; 

public class RadarZoomManager : MonoBehaviour
{
    [Header("Zoom settings")]
    public RectTransform radarContent;
    public float zoomSpeed = 0.001f;   
    public float minZoom = 0.5f;
    public float maxZoom = 2.0f;

    void Update()
    {
        float scroll = Mouse.current.scroll.ReadValue().y;

        if (scroll != 0)
        {
            float currentScale = radarContent.localScale.x;

            float newScale = Mathf.Clamp(currentScale + scroll * zoomSpeed, minZoom, maxZoom);

            radarContent.localScale = new Vector3(newScale, newScale, 1f);
        }
    }
}