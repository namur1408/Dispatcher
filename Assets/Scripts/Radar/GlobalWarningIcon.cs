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

        // Пытаемся найти CanvasGroup или Image, если мы управляем самим собой
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
                // Если управляем собой, нельзя использовать SetActive(false), иначе Update остановится!
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
                    // Fallback (может выключить дочерние объекты, но лучше так не делать без подготовки)
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
