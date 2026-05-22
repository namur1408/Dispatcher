using UnityEngine;

public class GlobalWarningIcon : MonoBehaviour
{
    [Header("Settings")]
    public GameObject warningIconObject;

    void Start()
    {
        if (warningIconObject == null)
        {
            warningIconObject = gameObject;
        }
    }

    void Update()
    {
        bool isWarning = BigRadarLoader.isGlobalWarningActive;
        
        if (warningIconObject != null && warningIconObject.activeSelf != isWarning)
        {
            warningIconObject.SetActive(isWarning);
        }
    }
}
