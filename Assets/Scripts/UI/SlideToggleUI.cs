using UnityEngine;

public class SlideToggleUI : MonoBehaviour
{
    [Header("Настройки панели")]
    public RectTransform panelToMove; 
    public Vector2 hideOffset = new Vector2(0, -1000f); 
    public float speed = 10f; 
    public bool startHidden = true; // Добавлена настройка

    [Header("Кнопки (Опционально)")]
    public GameObject buttonToOpen; // Кнопка, которая появится, когда машина полностью спрячется
    public GameObject buttonToClose; // Кнопка, которая будет скрываться во время уезда машины

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
            panelToMove.anchoredPosition = hiddenPosition; // Мгновенно перемещаем вниз
        }
        
        initialized = true;
        
        // Начальное состояние кнопок
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
            // Как только мы нажали "Открыть", сразу прячем кнопку открытия и показываем кнопку закрытия
            if (buttonToOpen != null) buttonToOpen.SetActive(false);
            if (buttonToClose != null) buttonToClose.SetActive(true);
        }
        else
        {
            // Как только нажали "Закрыть", сразу прячем кнопку закрытия и показываем кнопку открытия
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
