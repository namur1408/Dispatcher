using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Encapsulates the logical position, waypoints, and movement math for an airplane.
/// Part of Phase 3 refactoring to break down UIAirplane.
/// </summary>
public class AirplaneMovement
{
    public List<Vector2> waypoints = new List<Vector2>();
    public Vector2 logicalPosition = Vector2.zero;
    public bool isHolding = false;
    public float currentHoldingAngle = 0f;
    public Vector2 holdingCenter = Vector2.zero;

    // Returns true if the FIRST waypoint was reached
    public bool UpdatePosition(float deltaTime, float currentSpeed, float holdingRadius)
    {
        if (isHolding)
        {
            float angularSpeed = (currentSpeed / holdingRadius) * Mathf.Rad2Deg;
            currentHoldingAngle += angularSpeed * deltaTime;
            Vector2 circleTarget = holdingCenter + new Vector2(
                Mathf.Cos(currentHoldingAngle * Mathf.Deg2Rad),
                Mathf.Sin(currentHoldingAngle * Mathf.Deg2Rad)) * holdingRadius;
            logicalPosition = Vector2.MoveTowards(logicalPosition, circleTarget, currentSpeed * deltaTime);
            return false;
        }

        if (waypoints.Count == 0) return false;

        Vector2 currentTarget = waypoints[0];
        logicalPosition = Vector2.MoveTowards(logicalPosition, currentTarget, currentSpeed * deltaTime);

        return Vector2.Distance(logicalPosition, currentTarget) < 5f;
    }

    public void StartHolding(Vector2 center)
    {
        isHolding = true;
        holdingCenter = center;

        Vector2 dirFromCenter = (logicalPosition - center).normalized;
        currentHoldingAngle = Mathf.Atan2(dirFromCenter.y, dirFromCenter.x) * Mathf.Rad2Deg;
        waypoints.Clear();
    }

    public void StopHolding()
    {
        isHolding = false;
    }

    public Quaternion GetVisualRotation(float holdingRadius)
    {
        Vector2 direction = Vector2.zero;

        if (isHolding)
        {
            float nextAngle = currentHoldingAngle + 10f;
            Vector2 nextCircleTarget = holdingCenter + new Vector2(Mathf.Cos(nextAngle * Mathf.Deg2Rad), Mathf.Sin(nextAngle * Mathf.Deg2Rad)) * holdingRadius;
            direction = (nextCircleTarget - logicalPosition).normalized;
        }
        else if (waypoints.Count > 0)
        {
            direction = (waypoints[0] - logicalPosition).normalized;
        }

        if (direction != Vector2.zero)
        {
            float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            return Quaternion.Euler(0, 0, targetAngle);
        }
        return Quaternion.identity;
    }

    public int GetWaypointIndexAt(Vector2 clickPos, float thresholdRadius = 30f)
    {
        for (int i = 0; i < waypoints.Count; i++)
        {
            if (Vector2.Distance(clickPos, waypoints[i]) <= thresholdRadius)
            {
                return i;
            }
        }
        return -1;
    }

    public float DistanceToSegment(Vector2 point, Vector2 start, Vector2 end)
    {
        float l2 = (end - start).sqrMagnitude;
        if (l2 == 0.0f) return Vector2.Distance(point, start);
        float t = Mathf.Max(0, Mathf.Min(1, Vector2.Dot(point - start, end - start) / l2));
        Vector2 projection = start + t * (end - start);
        return Vector2.Distance(point, projection);
    }
}
