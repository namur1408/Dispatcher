using UnityEngine;

public class SlideToggleUI : MonoBehaviour
{
    [Header("Настройки панели")]
    public RectTransform panelToMove; 
    public Vector2 hideOffset = new Vector2(0, -1000f); 
    public float speed = 10f; 
    public bool startHidden = true; // Added setting

    [Header("Кнопки (Опционально)")]
    public GameObject buttonToOpen; // A button that will appear when the car is completely hidden
    public GameObject buttonToClose; // A button that will hide while the car is driving away

    private Vector2 visiblePosition;
    private Vector2 hiddenPosition;
    private bool isVisible = true;
    private bool initialized = false;

    void Start()
    {
        if (panelToMove == null) panelToMove = GetComponent<RectTransform>();
        
        visiblePosition = panelToMove.anchoredPosition;
        hiddenPosition = visiblePosition + hideOffset;
        
        if (startHidden)
        {
            isVisible = false;
            panelToMove.anchoredPosition = hiddenPosition; // Instantly move down
        }
        
        initialized = true;
        
        // Initial state of the buttons
        if (buttonToOpen != null) buttonToOpen.SetActive(!isVisible);
        if (buttonToClose != null) buttonToClose.SetActive(isVisible);
    }

    void Update()
    {
        if (!initialized) return;

        Vector2 target = isVisible ? visiblePosition : hiddenPosition;
        
        float dist = Vector2.Distance(panelToMove.anchoredPosition, target);
        if (dist > 0.5f)
        {
            panelToMove.anchoredPosition = Vector2.Lerp(panelToMove.anchoredPosition, target, Time.deltaTime * speed);
        }
    }

    public void Toggle()
    {
        isVisible = !isVisible;
        
        if (isVisible)
        {
            // As soon as we clicked "Open", we immediately hide the open button and show the close button
            if (buttonToOpen != null) buttonToOpen.SetActive(false);
            if (buttonToClose != null) buttonToClose.SetActive(true);
        }
        else
        {
            // As soon as you click “Close”, we immediately hide the close button and show the open button
            if (buttonToClose != null) buttonToClose.SetActive(false);
            if (buttonToOpen != null) buttonToOpen.SetActive(true);
        }
    }

    public void Show()
    {
        if (!isVisible) Toggle();
    }

    public void Hide()
    {
        if (isVisible) Toggle();
    }
}
