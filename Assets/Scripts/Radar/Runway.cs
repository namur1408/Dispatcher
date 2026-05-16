using UnityEngine;

public class Runway : MonoBehaviour
{
    public string id1; // e.g., "08"
    public string id2; // e.g., "26"

    [Tooltip("The angle of the runway for id1 (in degrees). id2 will be opposite.")]
    public float angleDegrees;

    [Tooltip("How far the plane needs to be to align before landing.")]
    public float alignmentDistance = 150f;

    public bool isOccupied { get; private set; }
    public float occupiedTimer { get; private set; }

    private void Start()
    {
        if (RunwayManager.Instance != null)
        {
            RunwayManager.Instance.RegisterRunway(this);
        }
    }

    private void Update()
    {
        if (isOccupied)
        {
            occupiedTimer -= Time.deltaTime;
            if (occupiedTimer <= 0)
            {
                isOccupied = false;
            }
        }
    }

    public void SetOccupied(float time)
    {
        isOccupied = true;
        occupiedTimer = time;
    }

    public Vector2 GetDirection(string runwayId)
    {
        float ang = (runwayId == id1) ? angleDegrees : angleDegrees + 180f;
        return new Vector2(Mathf.Cos(ang * Mathf.Deg2Rad), Mathf.Sin(ang * Mathf.Deg2Rad)).normalized;
    }

    public Vector2 GetAlignmentPoint(string runwayId, Vector2 centerPos)
    {
        Vector2 dir = GetDirection(runwayId);
        return centerPos - dir * alignmentDistance;
    }

    private void OnDrawGizmosSelected()
    {
        // Visualize in Editor
        Gizmos.color = Color.blue;
        Vector2 pos = transform.position;
        Vector2 dir1 = GetDirection(id1);
        Vector2 align1 = pos - dir1 * alignmentDistance;
        
        Gizmos.DrawLine(pos, align1);
        Gizmos.DrawWireSphere(align1, 10f);
        
        Gizmos.color = Color.cyan;
        Vector2 dir2 = GetDirection(id2);
        Vector2 align2 = pos - dir2 * alignmentDistance;
        Gizmos.DrawLine(pos, align2);
        Gizmos.DrawWireSphere(align2, 10f);
    }
}
