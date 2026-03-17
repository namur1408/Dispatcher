using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class BookHoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Book Sprite Settings")]
    [SerializeField] private Sprite normalSprite;      // The default book sprite
    [SerializeField] private Sprite highlightSprite;   // The highlighted book sprite

    private Image _bookImage;

    private void Awake()
    {
        _bookImage = GetComponent<Image>();

        if (_bookImage == null)
        {
            Debug.LogError($"Missing Image component on {gameObject.name}!");
            enabled = false; 
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_bookImage != null && highlightSprite != null)
        {
            _bookImage.sprite = highlightSprite; 
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_bookImage != null && normalSprite != null)
        {
            _bookImage.sprite = normalSprite; 
        }
    }
}