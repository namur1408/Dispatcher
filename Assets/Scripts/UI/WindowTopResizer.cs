using UnityEngine;
using UnityEngine.EventSystems;

public class WindowTopResizer : MonoBehaviour, IDragHandler, IPointerDownHandler
{
    [Tooltip("Окно (RectTransform), которое мы будем растягивать. Если пустое, возьмет родительский объект.")]
    public RectTransform windowRect;

    [Tooltip("Минимальная высота окна.")]
    public float minHeight = 30f;

    [Tooltip("Максимальная высота окна.")]
    public float maxHeight = 1000f;

    private Vector2 originalPointerPosition;
    private float originalHeight;
    private Vector2 originalPosition;

    void Start()
    {
        if (windowRect == null)
        {
            windowRect = transform.parent.GetComponent<RectTransform>();
        }

        if (windowRect != null)
        {
            SetHeightFromTop(minHeight, windowRect.rect.height, windowRect.anchoredPosition);
        }
    }

    public void OnPointerDown(PointerEventData data)
    {
        if (windowRect == null) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            windowRect.parent as RectTransform,
            data.position,
            data.pressEventCamera,
            out originalPointerPosition);
        originalHeight = windowRect.rect.height;
        originalPosition = windowRect.anchoredPosition;
    }

    public void OnDrag(PointerEventData data)
    {
        if (windowRect == null) return;

        Vector2 localPointerPosition;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            windowRect.parent as RectTransform,
            data.position,
            data.pressEventCamera,
            out localPointerPosition))
        {
            float deltaY = localPointerPosition.y - originalPointerPosition.y;
            float newHeight = originalHeight + deltaY;
            newHeight = Mathf.Clamp(newHeight, minHeight, maxHeight);

            SetHeightFromTop(newHeight, originalHeight, originalPosition);
        }
    }

    private void SetHeightFromTop(float newHeight, float baseHeight, Vector2 basePosition)
    {
        float heightDifference = newHeight - baseHeight;
        windowRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, newHeight);        
        windowRect.anchoredPosition = basePosition + new Vector2(0, heightDifference * windowRect.pivot.y);
    }
}
