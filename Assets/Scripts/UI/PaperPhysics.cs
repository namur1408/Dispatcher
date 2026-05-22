using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class PaperPhysics : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Настройки физики бумаги")]
    public float swayMultiplier = -0.5f; // Сила наклона от скорости мышки
    public float maxSwayAngle = 15f;     // Максимальный угол наклона
    public float springForce = 15f;      // Как быстро бумага выравнивается обратно
    public float dragDamping = 10f;      // Плавность наклона
    
    [Header("Настройки сжатия (Squash & Stretch)")]
    public bool enableSquash = true;
    public float squashAmount = 0.05f;   // Насколько бумага вытягивается при движении

    private Vector2 lastMousePos;
    private float currentSway = 0f;
    private float targetSway = 0f;
    private bool isDragging = false;
    
    private RectTransform rectTransform;
    private Vector3 originalScale;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        originalScale = rectTransform.localScale;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        lastMousePos = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 delta = eventData.position - lastMousePos;
        lastMousePos = eventData.position;
        
        // Горизонтальное движение вызывает наклон
        targetSway = Mathf.Clamp(delta.x * swayMultiplier, -maxSwayAngle, maxSwayAngle);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        targetSway = 0f;
    }

    void Update()
    {
        if (!isDragging)
        {
            targetSway = 0f; // Если отпустили, цель - ровное положение
        }

        // Плавно меняем текущий наклон к целевому
        float speed = isDragging ? dragDamping : springForce;
        currentSway = Mathf.Lerp(currentSway, targetSway, Time.deltaTime * speed);
        
        // Применяем вращение
        rectTransform.localRotation = Quaternion.Euler(0, 0, currentSway);

        // Применяем эффект "провисания/вытягивания" (опционально)
        if (enableSquash)
        {
            float stretch = 1f + (Mathf.Abs(currentSway) / maxSwayAngle) * squashAmount;
            float squash = 1f - (Mathf.Abs(currentSway) / maxSwayAngle) * squashAmount;
            
            // Если тянем, бумага чуть удлиняется по ходу движения и сужается
            rectTransform.localScale = new Vector3(originalScale.x * squash, originalScale.y * stretch, originalScale.z);
        }
    }
}
