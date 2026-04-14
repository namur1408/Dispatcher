using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RadarScreenClicker : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public static UIAirplane selectedPlane;

    public Camera radarCamera;
    public Button backButton;
    public LayerMask airplaneLayer;

    private RectTransform zoneRect;
    private Vector2 pointerDownPos;
    private const float DRAG_THRESHOLD = 30f;

    void Awake() => zoneRect = GetComponent<RectTransform>();

    public void OnPointerDown(PointerEventData eventData) => pointerDownPos = eventData.position;

    public void OnPointerUp(PointerEventData eventData)
    {
        if (Vector2.Distance(pointerDownPos, eventData.position) > DRAG_THRESHOLD) return;

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        foreach (var result in results)
        {
            if (backButton != null && result.gameObject == backButton.gameObject)
            {
                backButton.onClick.Invoke();
                return;
            }
        }

        if (radarCamera == null) return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(zoneRect, eventData.position, eventData.pressEventCamera, out Vector2 localPos))
        {
            float u = (localPos.x - zoneRect.rect.xMin) / zoneRect.rect.width;
            float v = (localPos.y - zoneRect.rect.yMin) / zoneRect.rect.height;
            float distanceToPlane = Mathf.Abs(radarCamera.transform.position.z);
            Vector3 worldClickPos = radarCamera.ViewportToWorldPoint(new Vector3(u, v, distanceToPlane));
            worldClickPos.z = 0f;

            Vector2 finalPosInsideContent = Vector2.zero;
            if (selectedPlane != null)
            {
                finalPosInsideContent = selectedPlane.transform.parent.InverseTransformPoint(worldClickPos);
            }

            if (selectedPlane != null)
            {
                int clickedIndex = selectedPlane.GetWaypointIndexAt(finalPosInsideContent, 150f);
                if (clickedIndex != -1)
                {
                    selectedPlane.RemoveWaypoint(clickedIndex);
                    return; 
                }
            }

            Collider2D hit = Physics2D.OverlapPoint(worldClickPos, airplaneLayer);
            if (hit != null)
            {
                UIAirplane plane = hit.GetComponentInParent<UIAirplane>();
                if (plane != null)
                {
                    if (selectedPlane == plane) DeselectAll();
                    else
                    {
                        selectedPlane = plane;
                        plane.TriggerSelection();
                    }
                    return; 
                }
            }

            if (selectedPlane != null)
            {
                selectedPlane.AddWaypoint(finalPosInsideContent);
            }
            else
            {
                DeselectAll();
            }
        }
    }

    private void DeselectAll()
    {
        selectedPlane = null;
        UIAirplane[] allPlanes = Object.FindObjectsByType<UIAirplane>(FindObjectsSortMode.None);
        foreach (var p in allPlanes) p.SetHighlight(false);
        if (BigRadarTerminal.Instance != null) BigRadarTerminal.Instance.ClearSelection();
    }
}