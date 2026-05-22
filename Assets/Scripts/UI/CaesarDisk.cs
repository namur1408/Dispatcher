using UnityEngine;
using UnityEngine.EventSystems;

public class CaesarDisk : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("References")]
    public DecryptionMachine machine;
    public RectTransform diskRect;
    
    [Header("Settings")]
    public int alphabetLength = 26;
    public float snapSpeed = 15f; 

    private float initialMouseAngle;
    private float initialDiskAngle;
    
    private float targetSnapAngle;
    private float visualAngle;

    void Start()
    {
        if (diskRect == null) diskRect = GetComponent<RectTransform>();
        
        visualAngle = diskRect.localEulerAngles.z;
        targetSnapAngle = visualAngle;
    }

    void Update()
    {
        // Всегда плавно доводим визуальный угол до целевого (эффект тяжелого механизма)
        if (Mathf.Abs(Mathf.DeltaAngle(visualAngle, targetSnapAngle)) > 0.01f)
        {
            visualAngle = Mathf.LerpAngle(visualAngle, targetSnapAngle, Time.deltaTime * snapSpeed);
            diskRect.localRotation = Quaternion.Euler(0, 0, visualAngle);
        }
        else
        {
            visualAngle = targetSnapAngle;
            diskRect.localRotation = Quaternion.Euler(0, 0, visualAngle);
        }
    }

    private float GetMouseAngle(PointerEventData eventData)
    {
        Vector2 screenCenter = RectTransformUtility.WorldToScreenPoint(eventData.pressEventCamera, diskRect.position);
        Vector2 offset = eventData.position - screenCenter;
        return Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        initialMouseAngle = GetMouseAngle(eventData);
        // Берем за основу не текущий оборванный кадр анимации, а последнюю четкую позицию (цель)
        initialDiskAngle = targetSnapAngle; 
    }

    public void OnDrag(PointerEventData eventData)
    {
        float currentMouseAngle = GetMouseAngle(eventData);
        float deltaAngle = Mathf.DeltaAngle(initialMouseAngle, currentMouseAngle);
        
        float desiredAngle = initialDiskAngle + deltaAngle;
        
        float step = 360f / alphabetLength;
        int shift = Mathf.RoundToInt(desiredAngle / step);
        
        float newTarget = shift * step;

        // Если мы перескочили на новое деление шестеренки
        if (!Mathf.Approximately(targetSnapAngle, newTarget))
        {
            targetSnapAngle = newTarget;
            // Можно добавить сюда звук щелчка: AudioSource.PlayClipAtPoint(clickSound, ...);
            CalculateShift(); // Обновляем сдвиг сразу при переходе на новое деление
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // При отпускании еще раз на всякий случай подтверждаем сдвиг
        CalculateShift();
    }

    private void CalculateShift()
    {
        float step = 360f / alphabetLength;
        int shift = Mathf.RoundToInt(targetSnapAngle / step);
        
        shift = shift % alphabetLength;
        if (shift > alphabetLength / 2) shift -= alphabetLength;
        if (shift < -alphabetLength / 2) shift += alphabetLength;

        if (machine != null)
        {
            machine.SetShift(shift);
        }
    }
}
