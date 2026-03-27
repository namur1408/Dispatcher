using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RadarScreenClicker : MonoBehaviour, IPointerClickHandler
{
    public static UIAirplane selectedPlane;

    public Camera radarCamera;
    public Button backButton;
    public LayerMask airplaneLayer;

    private RectTransform zoneRect;

    void Awake() => zoneRect = GetComponent<RectTransform>();

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

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

            // 3. ПОИСК САМОЛЕТА
            Collider2D hit = Physics2D.OverlapPoint(worldClickPos, airplaneLayer);

            if (hit != null)
            {
                UIAirplane plane = hit.GetComponent<UIAirplane>();
                if (plane != null)
                {
                    // --- НОВАЯ ЛОГИКА ПЕРЕКЛЮЧЕНИЯ (TOGGLE) ---
                    if (selectedPlane == plane)
                    {
                        // Если кликнули по УЖЕ выделенному самолету — снимаем выделение
                        DeselectAll();
                    }
                    else
                    {
                        // Если это новый самолет — выделяем его
                        selectedPlane = plane;
                        plane.TriggerSelection();
                    }
                }
            }
            else
            {
                // 4. КЛИК В ПУСТОТУ (Создание точки маршрута)
                if (selectedPlane != null)
                {
                    Transform radarContent = selectedPlane.transform.parent;
                    Vector2 finalWaypointPos = radarContent.InverseTransformPoint(worldClickPos);

                    int clickedIndex = selectedPlane.GetWaypointIndexAt(finalWaypointPos, 30f);

                    if (clickedIndex != -1)
                    {
                        selectedPlane.RemoveWaypoint(clickedIndex);
                    }
                    else
                    {
                        selectedPlane.AddWaypoint(finalWaypointPos);
                    }
                }
                else
                {
                    DeselectAll();
                }
            }
        }
    }

    private void DeselectAll()
    {
        selectedPlane = null;
        UIAirplane[] allPlanes = Object.FindObjectsByType<UIAirplane>(FindObjectsSortMode.None);
        foreach (var p in allPlanes) p.SetHighlight(false);

        if (BigRadarTerminal.Instance != null)
        {
            BigRadarTerminal.Instance.ClearSelection();
        }
    }
}