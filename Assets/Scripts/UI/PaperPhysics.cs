using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class PaperPhysics : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Настройки физики бумаги")]
    public float swayMultiplier = -0.5f; // Tilt force on mouse speed
    public float maxSwayAngle = 15f;     // Maximum tilt angle
    public float springForce = 15f;      // How quickly does the paper straighten back out?
    public float dragDamping = 10f;      // Smoothness of tilt
    
    [Header("Настройки сжатия (Squash & Stretch)")]
    public bool enableSquash = true;
    public float squashAmount = 0.05f;   // How much does the paper stretch when moving?

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
    private float currentLift = 0f; // 0 = lying down, 1 = raised in the air
    private GlobalPaperBend globalBend;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        originalScale = rectTransform.localScale;

        if (enableBend)
        {
            // Connecting our new global mesh modifier
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
        
        // Horizontal movement causes tilt
        targetSway = Mathf.Clamp(delta.x * swayMultiplier, -maxSwayAngle, maxSwayAngle);

        // Vertical movement causes the paper to sag
        if (enableBend)
        {
            // Increased the multiplier to make it more noticeable.
            // As we move the mouse (delta), we add or subtract from the base slack.
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
            targetSway = 0f; // If released, the goal is a level position
            if (enableBend) targetBend = 0f; // Place it on the table - the paper is leveled
        }
        else
        {
            // If we are holding the paper (isDragging = true) but the mouse is not moving,
            // delta in OnDrag is 0, so we need to smoothly return targetBend to baseSagAmount.
            // OnDrag is called only when moving, so we restore it here:
            targetBend = Mathf.Lerp(targetBend, baseSagAmount, Time.deltaTime * bendSpringForce);
        }

        // Smoothly change the current slope to the target one
        float speed = isDragging ? dragDamping : springForce;
        currentSway = Mathf.Lerp(currentSway, targetSway, Time.deltaTime * speed);
        
        // Apply rotation
        rectTransform.localRotation = Quaternion.Euler(0, 0, currentSway);

        // ── Lift & Squash (Increase and Shadow) ──
        
        // Smoothly change the lift value (0 - lying, 1 - in hand)
        currentLift = Mathf.Lerp(currentLift, isDragging ? 1f : 0f, Time.deltaTime * liftSpeed);
        
        // Basic scale taking into account elevation
        float liftScaleAmount = 1f + (liftScaleMultiplier - 1f) * currentLift;
        Vector3 currentBaseScale = originalScale * liftScaleAmount;

        // Applying the “sagging/pulling” effect when moving the mouse
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

        // ── We transmit data about the shadow and bend ──
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
            
            // If the shadow is animated, we need to rebuild the mesh
            if (isDragging || currentLift > 0.01f || Mathf.Abs(targetBend - currentBend) > 0.1f)
            {
                // Force the graphics update in GlobalPaperBend
                // (already handled through bendAmount, but let's add a dirty flag for the shadow)
                var g = GetComponent<UnityEngine.UI.Graphic>();
                if (g != null) g.SetVerticesDirty();
            }
        }
    }
}
