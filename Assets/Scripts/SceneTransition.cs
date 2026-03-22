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

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            if (RadarManager.Instance != null)
            {
                RadarManager.Instance.SaveToGlobalManager();
            }

            Debug.Log($"Switching to scene: {sceneName}");
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogError("Scene Name is not assigned in the Inspector!");
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (spriteRenderer != null) spriteRenderer.color = hoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (spriteRenderer != null) spriteRenderer.color = originalColor;
    }
}