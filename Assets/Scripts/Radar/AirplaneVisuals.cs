using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class AirplaneVisuals
{
    private UIAirplane _plane;
    private CanvasGroup _canvasGroup;
    private TextMeshProUGUI _callsignText;
    private Image _hitboxVisual;
    private GameObject _routeSegmentPrefab;
    private GameObject _waypointMarkerPrefab;
    private Transform _parentTransform;

    private List<GameObject> _lineSegments = new List<GameObject>();
    private List<GameObject> _activeMarkers = new List<GameObject>();

    public AirplaneVisuals(UIAirplane plane, CanvasGroup canvasGroup, TextMeshProUGUI callsignText, Image hitboxVisual, GameObject routeSegPrefab, GameObject waypointMarkerPrefab, Transform parentTransform)
    {
        _plane = plane;
        _canvasGroup = canvasGroup;
        _callsignText = callsignText;
        _hitboxVisual = hitboxVisual;
        _routeSegmentPrefab = routeSegPrefab;
        _waypointMarkerPrefab = waypointMarkerPrefab;
        _parentTransform = parentTransform;
    }

    public void SetVisualState(bool visible, float alpha = 1f)
    {
        if (_canvasGroup != null) _canvasGroup.alpha = alpha;
        if (_callsignText != null) _callsignText.gameObject.SetActive(visible);
        foreach (var marker in _activeMarkers) if (marker != null) marker.SetActive(visible);
        foreach (var segment in _lineSegments) if (segment != null) segment.SetActive(visible);
        if (_hitboxVisual != null) _hitboxVisual.gameObject.SetActive(visible);
    }

    public void CheckZoomVisibility(float zoom, float threshold)
    {
        bool show = zoom >= threshold;
        if (_callsignText != null && _callsignText.gameObject.activeSelf != show) 
            _callsignText.gameObject.SetActive(show);
    }

    public void SyncRouteAlpha()
    {
        if (_canvasGroup == null) return;
        float currentAlpha = _canvasGroup.alpha;

        foreach (GameObject seg in _lineSegments)
        {
            if (seg == null) continue;
            Image parentImg = seg.GetComponent<Image>();
            if (parentImg != null)
            {
                Color pc = parentImg.color;
                pc.a = currentAlpha * 0.4f;
                parentImg.color = pc;
            }

            Transform fuelVisualTrans = seg.transform.Find("FuelVisual");
            if (fuelVisualTrans != null)
            {
                Image childImg = fuelVisualTrans.GetComponent<Image>();
                Color cc = childImg.color;
                cc.a = currentAlpha;
                childImg.color = cc;
            }
        }

        foreach (GameObject marker in _activeMarkers)
        {
            if (marker == null) continue;
            Image img = marker.GetComponent<Image>();
            Color c = img.color;
            c.a = currentAlpha;
            img.color = c;
        }
    }

    public void FadeOut(bool isLandingPhase, bool isTakingOff, bool hasBeenPinged, bool isSelected, float fadeSpeed, float minAlpha)
    {
        if (isLandingPhase || isTakingOff)
        {
            if (_canvasGroup != null) _canvasGroup.alpha = 0.3f;
            SyncRouteAlpha();
            return;
        }

        if (!hasBeenPinged)
        {
            if (_canvasGroup != null) _canvasGroup.alpha = 0f;
            SyncRouteAlpha();
            return;
        }

        if (isSelected)
        {
            if (_canvasGroup.alpha != 1f)
            {
                _canvasGroup.alpha = 1f;
                SyncRouteAlpha();
            }
            return;
        }

        if (_canvasGroup != null && _canvasGroup.alpha > minAlpha)
        {
            _canvasGroup.alpha = Mathf.Max(minAlpha, _canvasGroup.alpha - fadeSpeed * Time.deltaTime);
            SyncRouteAlpha();
        }
    }

    public void CleanupRouteVisuals()
    {
        if (_lineSegments != null)
        {
            foreach (GameObject seg in _lineSegments) if (seg != null) Object.Destroy(seg);
            _lineSegments.Clear();
        }

        if (_activeMarkers != null)
        {
            foreach (GameObject marker in _activeMarkers) if (marker != null) Object.Destroy(marker);
            _activeMarkers.Clear();
        }
    }

    public void RebuildRouteLayer(bool isLandingPhase)
    {
        if (_plane.fuelSystem != null) _plane.fuelSystem.RecalcFuelRange();

        var waypoints = _plane.waypoints;

        if (isLandingPhase || waypoints.Count == 0)
        {
            foreach (var seg in _lineSegments) if (seg != null) seg.SetActive(false);
            foreach (var marker in _activeMarkers) if (marker != null) marker.SetActive(false);
            return;
        }

        foreach (var seg in _lineSegments) seg.SetActive(false);
        foreach (var marker in _activeMarkers) marker.SetActive(false);

        int currentMarkerIndex = 0;
        int currentSegmentIndex = 0;

        for (int i = 0; i < waypoints.Count; i++)
        {
            SetMarker(currentMarkerIndex, waypoints[i]);
            currentMarkerIndex++;

            if (i < waypoints.Count - 1)
            {
                SetSegment(currentSegmentIndex, waypoints[i], waypoints[i + 1]);
                currentSegmentIndex++;
            }
        }

        SetSegment(currentSegmentIndex, _plane.logicalPosition, waypoints[0]);
        SyncRouteAlpha();
        UpdateHitboxColor();
    }

    private void SetMarker(int index, Vector2 pos)
    {
        GameObject marker;
        if (index < _activeMarkers.Count)
        {
            marker = _activeMarkers[index];
            marker.SetActive(true);
        }
        else
        {
            marker = Object.Instantiate(_waypointMarkerPrefab, _parentTransform, false);
            _activeMarkers.Add(marker);
        }

        marker.transform.SetSiblingIndex(_plane.transform.GetSiblingIndex());
        marker.GetComponent<RectTransform>().anchoredPosition = pos;
    }

    private void SetSegment(int index, Vector2 start, Vector2 end)
    {
        GameObject seg;
        if (index < _lineSegments.Count)
        {
            seg = _lineSegments[index];
            seg.SetActive(true);
        }
        else
        {
            seg = Object.Instantiate(_routeSegmentPrefab, _parentTransform, false);
            _lineSegments.Add(seg);
        }

        seg.transform.SetSiblingIndex(_plane.transform.GetSiblingIndex());
        UpdateSegmentLook(seg.GetComponent<RectTransform>(), start, end);
    }

    public void UpdateFirstSegment()
    {
        var waypoints = _plane.waypoints;
        if (waypoints.Count == 0) return;

        int activeSegmentIndex = waypoints.Count - 1;
        if (activeSegmentIndex >= 0 && activeSegmentIndex < _lineSegments.Count)
        {
            UpdateSegmentLook(_lineSegments[activeSegmentIndex].GetComponent<RectTransform>(),
                              _plane.logicalPosition,
                              waypoints[0]);
        }
    }

    private void UpdateSegmentLook(RectTransform segRect, Vector2 start, Vector2 end)
    {
        float dist = Vector2.Distance(start, end);
        segRect.pivot = new Vector2(0.5f, 0f);
        float scaleY = segRect.localScale.y != 0 ? segRect.localScale.y : 1f;
        segRect.sizeDelta = new Vector2(_plane.routeLineWidth, dist / scaleY);
        segRect.anchoredPosition = start;
        Vector2 dir = (end - start).normalized;
        segRect.rotation = Quaternion.Euler(0, 0, (Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg) - 90f);
    }

    public void UpdateHitboxColor()
    {
        if (_hitboxVisual == null) return;

        Color iconColor = Color.white;
        bool isSelected = RadarScreenClicker.selectedPlane == _plane;
        bool isColliding = _plane.isColliding; // Needs public accessor
        bool isInDanger = _plane.isInDanger;

        if (isColliding || _plane.isOutOfFuel) iconColor = Color.red;
        else if (_plane.inStorm) iconColor = new Color(0.4f, 0.4f, 0.4f, 0.8f);
        else if (isSelected) iconColor = new Color(1f, 0.9f, 0f, 1f);
        else if (isInDanger) iconColor = new Color(1f, 0.5f, 0f);
        else
        {
            if (_plane.dispatchStatus == UIAirplane.DispatchStatus.Approved) iconColor = Color.green;
            else if (_plane.dispatchStatus == UIAirplane.DispatchStatus.Denied) iconColor = Color.red;
            else iconColor = Color.white;
        }

        if (_canvasGroup != null) iconColor.a = _canvasGroup.alpha;
        _hitboxVisual.color = iconColor;

        if (!_plane.isOutOfFuel || _callsignText.text != "MAYDAY")
        {
            _callsignText.color = iconColor;
        }
        else
        {
            _callsignText.color = Color.red;
        }

        Color fuelColor = isSelected ? new Color(1f, 0.9f, 0f, iconColor.a) : new Color(0f, 1f, 0f, iconColor.a);
        Color emptyColor = new Color(1f, 0f, 0f, iconColor.a * 0.4f);

        float maxFlightDistance = 0f;
        if (_plane.fuelSystem != null)
        {
            float distFromOriginToPlane = Vector2.Distance(_plane.fuelSystem.routeOriginPosition, _plane.GetComponent<RectTransform>().anchoredPosition);
            maxFlightDistance = Mathf.Max(0f, _plane.fuelSystem.fuelRangeFromRouteOrigin - distFromOriginToPlane);
        }

        float accumulatedDistance = 0f;
        var waypoints = _plane.waypoints;

        if (_lineSegments != null && waypoints.Count > 0)
        {
            List<int> orderedIndices = new List<int>();
            orderedIndices.Add(waypoints.Count - 1);
            for (int i = 0; i < waypoints.Count - 1; i++) orderedIndices.Add(i);

            Vector2 lastPos = _plane.GetComponent<RectTransform>().anchoredPosition;

            foreach (int idx in orderedIndices)
            {
                if (idx < _lineSegments.Count && _lineSegments[idx] != null)
                {
                    Vector2 nextPos = (idx == orderedIndices[0]) ? waypoints[0] : waypoints[idx + 1];
                    float segLen = Vector2.Distance(lastPos, nextPos);

                    Image redLineImg = _lineSegments[idx].GetComponent<Image>();
                    if (redLineImg != null) redLineImg.color = emptyColor;

                    Transform fuelVisualTrans = _lineSegments[idx].transform.Find("FuelVisual");
                    if (fuelVisualTrans != null)
                    {
                        Image fuelImg = fuelVisualTrans.GetComponent<Image>();
                        float distLeft = maxFlightDistance - accumulatedDistance;

                        if (distLeft <= 0) fuelImg.fillAmount = 0;
                        else if (distLeft >= segLen) { fuelImg.fillAmount = 1; fuelImg.color = fuelColor; }
                        else { fuelImg.fillAmount = distLeft / segLen; fuelImg.color = fuelColor; }
                    }

                    accumulatedDistance += segLen;
                    lastPos = nextPos;
                }
            }
        }

        if (_activeMarkers != null)
        {
            float distToMarker = 0f;
            Vector2 markerPathPos = _plane.GetComponent<RectTransform>().anchoredPosition;
            for (int i = 0; i < waypoints.Count; i++)
            {
                distToMarker += Vector2.Distance(markerPathPos, waypoints[i]);
                markerPathPos = waypoints[i];
                if (i < _activeMarkers.Count && _activeMarkers[i] != null)
                {
                    Image mImg = _activeMarkers[i].GetComponent<Image>();
                    mImg.color = (distToMarker > maxFlightDistance) ? new Color(1f, 0f, 0f, iconColor.a) : fuelColor;
                }
            }
        }
    }
}
