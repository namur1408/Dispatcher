using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Events;

public class FolderTearInteractable : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    [Header("UI Elements")]
    public Image sealBaseImage; // Image with FillAmount (Horizontal, Origin Left)
    public RectTransform rolledSeal; // The piece that rolls up
    
    [Header("Settings")]
    public float tearThreshold = 0.95f; // How far to tear before it fully rips (0.95 means torn by 95%)
    public float maxRolledScaleX = 1.3f; // How much wider it gets
    public float maxRolledScaleY = 1.3f; // How much taller it gets
    
    public UnityEvent OnTearComplete;

    private float originalWidth;
    private float startRolledX;
    private bool isTorn = false;

    void Start()
    {
        if (sealBaseImage != null)
        {
            originalWidth = sealBaseImage.rectTransform.rect.width;
            sealBaseImage.fillAmount = 1f;
        }
        if (rolledSeal != null)
        {
            startRolledX = rolledSeal.anchoredPosition.x;
        }
        UpdateRolledSeal();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isTorn) return;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isTorn || sealBaseImage == null) return;

        // Convert drag position to local point in seal rect
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            sealBaseImage.rectTransform, 
            eventData.position, 
            eventData.pressEventCamera, 
            out Vector2 localPoint);

        // Calculate fill amount based on local X
        float normalizedX = (localPoint.x + (sealBaseImage.rectTransform.pivot.x * originalWidth)) / originalWidth;
        
        // Ensure the player is dragging from right to left (fillAmount decreases)
        // We only allow tearing in one direction. We check if the finger is to the left of the current tear point.
        if (normalizedX < sealBaseImage.fillAmount)
        {
            // Clamp so we don't go below 0
            sealBaseImage.fillAmount = Mathf.Max(0f, normalizedX);
            UpdateRolledSeal();

            // If torn enough (e.g. fillAmount <= 0.05 when tearThreshold is 0.95)
            if (sealBaseImage.fillAmount <= (1f - tearThreshold))
            {
                CompleteTear();
            }
        }
    }

    void UpdateRolledSeal()
    {
        if (rolledSeal == null || sealBaseImage == null) return;

        float tornAmount = 1f - sealBaseImage.fillAmount; // 0 to 1
        
        // Move the rolled seal to the current edge relative to its start position
        float currentX = startRolledX - (tornAmount * originalWidth);
        rolledSeal.anchoredPosition = new Vector2(currentX, rolledSeal.anchoredPosition.y);

        // Scale the rolled seal slightly
        float scaleX = Mathf.Lerp(1f, maxRolledScaleX, tornAmount);
        float scaleY = Mathf.Lerp(1f, maxRolledScaleY, tornAmount);
        rolledSeal.localScale = new Vector3(scaleX, scaleY, 1f);
        
        // Show rolled seal only if tearing has started
        if (!rolledSeal.gameObject.activeSelf && tornAmount > 0.01f)
        {
            rolledSeal.gameObject.SetActive(true);
        }
    }

    void CompleteTear()
    {
        isTorn = true;
        sealBaseImage.fillAmount = 0f;
        if (rolledSeal != null) rolledSeal.gameObject.SetActive(false);
        
        OnTearComplete?.Invoke();
    }
}
