using UnityEngine;
using UnityEngine.UI;

public class GlobalWarningIcon : MonoBehaviour
{
    [Header("Settings")]
    public GameObject warningIconObject;

    private CanvasGroup canvasGroup;
    private Image img;

    void Start()
    {
        if (warningIconObject == null)
        {
            warningIconObject = gameObject;
        }

        // Trying to find CanvasGroup or Image if we are managing ourselves
        if (warningIconObject == gameObject)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            img = GetComponent<Image>();
        }
    }

    void Update()
    {
        bool isWarning = BigRadarLoader.isGlobalWarningActive;
        
        if (warningIconObject != null)
        {
            if (warningIconObject == gameObject)
            {
                // If we control ourselves, we cannot use SetActive(false), otherwise Update will stop!
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = isWarning ? 1f : 0f;
                }
                else if (img != null)
                {
                    img.enabled = isWarning;
                }
                else
                {
                    // Fallback (can turn off child objects, but it's better not to do this without preparation)
                    foreach (Transform child in transform)
                    {
                        child.gameObject.SetActive(isWarning);
                    }
                }
            }
            else
            {
                if (warningIconObject.activeSelf != isWarning)
                {
                    warningIconObject.SetActive(isWarning);
                }
            }
        }
    }
}
