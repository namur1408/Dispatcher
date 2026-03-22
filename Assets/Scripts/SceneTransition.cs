using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class SceneTransition : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Transition Settings")]
    [Tooltip("The exact name of the scene to load")]
    [SerializeField] private string sceneName;

    [Header("Hover Effect (Optional)")]
    [SerializeField] private Color hoverColor = Color.yellow;
    private Color originalColor;
    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) originalColor = spriteRenderer.color;
    }

    // Method called when the object is clicked
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            Debug.Log($"Switching to scene: {sceneName}");
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogError("Scene Name is not assigned in the Inspector!");
        }
    }

    // Visual feedback when mouse enters
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (spriteRenderer != null) spriteRenderer.color = hoverColor;
    }

    // Visual feedback when mouse exits
    public void OnPointerExit(PointerEventData eventData)
    {
        if (spriteRenderer != null) spriteRenderer.color = originalColor;
    }
}