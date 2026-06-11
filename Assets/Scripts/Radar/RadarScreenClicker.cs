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

    [Header("Calibration (Percentage/UV)")]
    [Tooltip("Корректировка клика в процентах (например, 0.05 сместит на 5%).")]
    public Vector2 uvCalibration = Vector2.zero;

    void Awake() 
    {
        zoneRect = GetComponent<RectTransform>();
        if (GetComponent<RadarClickVisualizer>() == null)
        {
            gameObject.AddComponent<RadarClickVisualizer>();
        }
    }

    public void OnPointerDown(PointerEventData eventData) => pointerDownPos = eventData.position;

    public void OnPointerUp(PointerEventData eventData)
    {
        Debug.Log($"[RadarClicker] OnPointerUp at screen pos: {eventData.position}");
        if (Vector2.Distance(pointerDownPos, eventData.position) > DRAG_THRESHOLD) 
        {
            Debug.Log("[RadarClicker] Ignored because it was a drag.");
            return;
        }

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

        Canvas canvas = zoneRect.GetComponentInParent<Canvas>();
        Camera clickCamera = (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : eventData.pressEventCamera;

        // Раз теперь всё в одном UI канвасе, мы можем получить мировые координаты UI напрямую!
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(zoneRect, eventData.position, clickCamera, out Vector3 worldClickPos))
        {
            // Если нужна старая калибровка (обычно в одном канвасе она не нужна, но на всякий случай оставим)
            if (uvCalibration != Vector2.zero)
            {
                // Сдвигаем мировые координаты на основе размеров зоны
                worldClickPos.x += zoneRect.rect.width * uvCalibration.x * zoneRect.lossyScale.x;
                worldClickPos.y += zoneRect.rect.height * uvCalibration.y * zoneRect.lossyScale.y;
            }

            worldClickPos.z = 0f; // Убеждаемся, что мы в 2D плоскости
            Debug.Log($"[RadarClicker] ScreenPos: {eventData.position}, WorldClickPos: {worldClickPos}");

            Vector2 finalPosInsideContent = Vector2.zero;
            if (selectedPlane != null)
            {
                finalPosInsideContent = selectedPlane.transform.parent.InverseTransformPoint(worldClickPos);
            }

            bool clickedWaypoint = false;
            if (selectedPlane != null)
            {
                int clickedIndex = selectedPlane.GetWaypointIndexAt(finalPosInsideContent, 40f);
                if (clickedIndex != -1)
                {
                    selectedPlane.RemoveWaypoint(clickedIndex);
                    clickedWaypoint = true;
                }
            }

            // Поиск самолёта через UI Raycast
            UIAirplane clickedPlane = null;
            foreach (var result in results)
            {
                UIAirplane plane = result.gameObject.GetComponentInParent<UIAirplane>();
                if (plane != null)
                {
                    clickedPlane = plane;
                    break;
                }
            }

            // Эффект клика
            if (RadarClickVisualizer.Instance != null)
            {
                Transform clickParent = zoneRect;
                if (AirplaneSpawner.Instance != null)
                {
                    Transform activeContent = AirplaneSpawner.Instance.radarContent;
                    if (activeContent != null) clickParent = activeContent;
                }
                bool playBgSound = (clickedPlane == null && !clickedWaypoint);
                RadarClickVisualizer.Instance.ShowClick(worldClickPos, clickParent, playBgSound);
            }

            if (clickedWaypoint) return;

            if (clickedPlane != null)
            {
                if (selectedPlane == clickedPlane) DeselectAll();
                else
                {
                    selectedPlane = clickedPlane;
                    clickedPlane.TriggerSelection();
                }
                return; 
            }

            // Если не попали по самолету, добавляем путевую точку
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