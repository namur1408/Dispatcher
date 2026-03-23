using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class BookHoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite highlightSprite;
    [SerializeField] private string sceneToLoad = "ManualScene";

    private Image _bookImage;

    private void Awake()
    {
        _bookImage = GetComponent<Image>();
    }

    public void OnPointerEnter(PointerEventData eventData) => _bookImage.sprite = highlightSprite;
    public void OnPointerExit(PointerEventData eventData) => _bookImage.sprite = normalSprite;

    public void OnPointerClick(PointerEventData eventData)
    {
        SceneManager.LoadScene(sceneToLoad);
    }
}