using UnityEngine;

public class RadarScanner : MonoBehaviour
{
    public float rotationSpeed = 60f; 

    void Update()
    {
        transform.Rotate(0, 0, -rotationSpeed * Time.deltaTime);
    }
}