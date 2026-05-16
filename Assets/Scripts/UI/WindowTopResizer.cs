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
            // Устанавливаем высоту при старте, корректно обрабатывая якоря
            SetHeightFromTop(minHeight, windowRect.rect.height, windowRect.anchoredPosition);
        }
    }

    public void OnPointerDown(PointerEventData data)
    {
        if (windowRect == null) return;

        // Запоминаем начальную позицию курсора относительно родителя окна (холста или панели)
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            windowRect.parent as RectTransform,
            data.position,
            data.pressEventCamera,
            out originalPointerPosition);

        // Запоминаем изначальные абсолютные размеры и позицию окна
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
            // Вычисляем, насколько сдвинули мышку по оси Y
            float deltaY = localPointerPosition.y - originalPointerPosition.y;

            // Вычисляем новую высоту
            float newHeight = originalHeight + deltaY;

            // Ограничиваем высоту от minHeight до maxHeight
            newHeight = Mathf.Clamp(newHeight, minHeight, maxHeight);

            SetHeightFromTop(newHeight, originalHeight, originalPosition);
        }
    }

    private void SetHeightFromTop(float newHeight, float baseHeight, Vector2 basePosition)
    {
        float heightDifference = newHeight - baseHeight;

        // SetSizeWithCurrentAnchors устанавливает абсолютную высоту, игнорируя настройки якорей (Stretch и т.д.)
        windowRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, newHeight);
        
        // Компенсируем сдвиг, чтобы нижний край оставался строго на месте в зависимости от Pivot
        windowRect.anchoredPosition = basePosition + new Vector2(0, heightDifference * windowRect.pivot.y);
    }
}
