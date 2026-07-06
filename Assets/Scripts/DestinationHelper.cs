using UnityEngine;

/// <summary>
/// Global registry for coordinates of destinations on radar.
/// Used by UIAirplane and RadarPanelsManager to prevent code duplication.
/// </summary>
public static class DestinationHelper
{
    /// <summary>
    /// Returns the coordinate of a destination in RadarContent space.
    /// For unknown items, calculates the position using the name hash.
    /// </summary>
    public static Vector2 GetCoordinate(string destination)
    {
        if (string.IsNullOrEmpty(destination)) return Vector2.zero;

        switch (destination)
        {
            case "Bastion-1": return new Vector2(-416f,  476f);
            case "Bastion-2": return new Vector2( 400f,  400f);
            case "Bastion-3": return new Vector2(-535f,  119f);
            case "Bastion-4": return new Vector2(   0f,  535f);
            case "Bastion-5": return new Vector2( 437f, -357f);
            case "Bastion-6": return new Vector2(-450f, -400f);
            case "Bastion-7": return new Vector2( 500f,  100f);
            case "Bastion-8": return new Vector2( 150f, -500f);
            case "Bastion-9": return new Vector2(-200f,  500f);
            case "Sector-Z":  return new Vector2(   0f,  535f);
            default:
                int hash = destination.GetHashCode();
                float angle = Mathf.Abs(hash % 360) * Mathf.Deg2Rad;
                float radius = 480f + Mathf.Abs(hash % 50);
                return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }
    }
}
