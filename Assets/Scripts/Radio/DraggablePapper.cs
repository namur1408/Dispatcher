using UnityEngine;
using UnityEngine.EventSystems;

public class DraggablePaper : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    [Header("Audio")] 
    public AudioClip pickupSound;
    [Range(0f, 1f)] public float soundVolume = 0.8f;

    private RectTransform rectTransform;
    private Canvas canvas;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        rectTransform.SetAsLastSibling();

        if (pickupSound != null && Camera.main != null)
        {
            AudioSource.PlayClipAtPoint(pickupSound, Camera.main.transform.position, soundVolume);
        }

        if (RadioTutorialManager.Instance != null && !RadioTutorialManager.isRadioTutorialCompleted)
        {
            RadioTutorialManager.Instance.NotifyDocumentClicked();
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (canvas == null) return;
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }
}
