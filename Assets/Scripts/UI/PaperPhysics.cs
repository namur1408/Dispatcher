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

    [Header("Настройки прогиба (Shader Bend)")]
    public bool enableBend = true;
    [Tooltip("Базовое провисание под своим весом, когда держишь бумагу")]
    public float baseSagAmount = 30f;
    [Tooltip("Множитель прогиба от вертикальной скорости мыши.")]
    public float bendMultiplier = 3f;
    [Tooltip("Макс. сила прогиба шейдера.")]
    public float maxBendAmount = 80f;
    [Tooltip("Скорость возврата прогиба к базе.")]
    public float bendSpringForce = 8f;
    [Tooltip("Инерция: при резком движении бумага 'запаздывает'.")]
    public float bendInertia = 0.15f;

    [Header("Настройки поднятия (Lift)")]
    public bool enableLift = true;
    [Tooltip("Во сколько раз увеличивается бумага при поднятии")]
    public float liftScaleMultiplier = 1.05f;
    [Tooltip("Смещение тени вниз при поднятии")]
    public float shadowDistance = 20f;
    [Tooltip("Прозрачность тени при поднятии")]
    [Range(0f, 1f)]
    public float shadowAlpha = 0.4f;
    [Tooltip("Плавность поднятия и опускания")]
    public float liftSpeed = 12f;

    private Vector2 lastMousePos;
    private float currentSway = 0f;
    private float targetSway = 0f;
    private bool isDragging = false;
    
    private RectTransform rectTransform;
    private Vector3 originalScale;

    // Bend
    private float currentBend = 0f;
    private float targetBend = 0f;

    // Lift
    private float currentLift = 0f; // 0 = лежит, 1 = поднята в воздух
    private GlobalPaperBend globalBend;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        originalScale = rectTransform.localScale;

        if (enableBend)
        {
            // Подключаем наш новый глобальный модификатор мешей
            globalBend = GetComponent<GlobalPaperBend>();
            if (globalBend == null)
            {
                globalBend = gameObject.AddComponent<GlobalPaperBend>();
            }
        }
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

        // Вертикальное движение вызывает прогиб бумаги
        if (enableBend)
        {
            // Увеличили множитель, чтобы было заметнее.
            // При движении мыши (delta) мы добавляем или вычитаем из базового провисания.
            float motionBend = -delta.y * bendMultiplier * 2.5f;
            motionBend += Mathf.Abs(delta.x) * bendMultiplier * 0.5f;
            
            targetBend = Mathf.Clamp(baseSagAmount + motionBend, -maxBendAmount, maxBendAmount);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        targetSway = 0f;
        targetBend = 0f;
    }

    void Update()
    {
        if (!isDragging)
        {
            targetSway = 0f; // Если отпустили, цель - ровное положение
            if (enableBend) targetBend = 0f; // Кладём на стол - бумага выравнивается
        }
        else
        {
            // Если мы держим бумагу (isDragging = true), но мышь не двигается,
            // delta в OnDrag равна 0, поэтому нам нужно плавно возвращать targetBend к baseSagAmount.
            // OnDrag вызывается только при движении, поэтому восстанавливаем тут:
            targetBend = Mathf.Lerp(targetBend, baseSagAmount, Time.deltaTime * bendSpringForce);
        }

        // Плавно меняем текущий наклон к целевому
        float speed = isDragging ? dragDamping : springForce;
        currentSway = Mathf.Lerp(currentSway, targetSway, Time.deltaTime * speed);
        
        // Применяем вращение
        rectTransform.localRotation = Quaternion.Euler(0, 0, currentSway);

        // ── Lift & Squash (Увеличение и Тень) ──
        
        // Плавно меняем значение поднятия (0 - лежит, 1 - в руке)
        currentLift = Mathf.Lerp(currentLift, isDragging ? 1f : 0f, Time.deltaTime * liftSpeed);
        
        // Базовый масштаб с учетом поднятия
        float liftScaleAmount = 1f + (liftScaleMultiplier - 1f) * currentLift;
        Vector3 currentBaseScale = originalScale * liftScaleAmount;

        // Применяем эффект "провисания/вытягивания" при движении мышки
        if (enableSquash)
        {
            float stretch = 1f + (Mathf.Abs(currentSway) / maxSwayAngle) * squashAmount;
            float squash = 1f - (Mathf.Abs(currentSway) / maxSwayAngle) * squashAmount;
            
            rectTransform.localScale = new Vector3(currentBaseScale.x * squash, currentBaseScale.y * stretch, currentBaseScale.z);
        }
        else
        {
            rectTransform.localScale = currentBaseScale;
        }

        // ── Передаем данные о тени и изгибе ──
        if (globalBend != null)
        {
            globalBend.dropShadowAlpha = currentLift * shadowAlpha;
            globalBend.dropShadowDistance = new Vector2(0, -shadowDistance * currentLift);
            globalBend.foldShadowAlpha = currentLift;

            if (enableBend)
            {
                float bendSpeed = isDragging ? dragDamping : bendSpringForce;
                currentBend = Mathf.Lerp(currentBend, targetBend, Time.deltaTime * bendSpeed);
                globalBend.bendAmount = currentBend;
            }
            
            // Если тень анимируется, нам нужно перестраивать сетку
            if (isDragging || currentLift > 0.01f || Mathf.Abs(targetBend - currentBend) > 0.1f)
            {
                // Форсируем обновление графики в GlobalPaperBend
                // (уже обрабатывается через bendAmount, но добавим грязный флаг для тени)
                var g = GetComponent<UnityEngine.UI.Graphic>();
                if (g != null) g.SetVerticesDirty();
            }
        }
    }
}
