using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class TestData
{
    public List<Vector2> waypoints = new List<Vector2>();
}

public class TestJson : MonoBehaviour
{
    void Start()
    {
        TestData data = new TestData();
        data.waypoints.Add(new Vector2(1, 2));
        data.waypoints.Add(new Vector2(3, 4));
        string json = JsonUtility.ToJson(data);
        Debug.Log("JSON output: " + json);
    }
}
