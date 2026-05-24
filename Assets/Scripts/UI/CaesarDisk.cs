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

    [Header("Audio")]
    public AudioClip rotationSound;
    [Range(0f, 5f)] public float soundVolume = 1f; // Можно больше 1 (1 = 100%, 3 = 300%)
    public float minPitch = 0.9f;
    public float maxPitch = 1.1f;

    private AudioSource dedicatedAudioSource;
    private bool isMoving = false;

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
        bool moving = false;
        // Всегда плавно доводим визуальный угол до целевого (эффект тяжелого механизма)
        if (Mathf.Abs(Mathf.DeltaAngle(visualAngle, targetSnapAngle)) > 0.01f)
        {
            visualAngle = Mathf.LerpAngle(visualAngle, targetSnapAngle, Time.deltaTime * snapSpeed);
            diskRect.localRotation = Quaternion.Euler(0, 0, visualAngle);
            moving = true;
        }
        else
        {
            visualAngle = targetSnapAngle;
            diskRect.localRotation = Quaternion.Euler(0, 0, visualAngle);
            moving = false;
        }
        HandleSound(moving);
    }

    private void HandleSound(bool moving)
    {
        if (rotationSound == null) return;
        
        if (dedicatedAudioSource == null)
        {
            dedicatedAudioSource = gameObject.AddComponent<AudioSource>();
            dedicatedAudioSource.playOnAwake = false;
        }

        if (moving && !isMoving)
        {
            dedicatedAudioSource.clip = rotationSound;
            dedicatedAudioSource.volume = Mathf.Min(soundVolume, 1f); // Устанавливаем макс базовую громкость
            dedicatedAudioSource.pitch = Random.Range(minPitch, maxPitch);
            dedicatedAudioSource.loop = true;
            dedicatedAudioSource.Play();
            isMoving = true;
        }
        else if (!moving && isMoving)
        {
            dedicatedAudioSource.Stop();
            isMoving = false;
        }
    }

    // Программное усиление звука (если громкость больше 100%)
    void OnAudioFilterRead(float[] data, int channels)
    {
        if (soundVolume > 1f && isMoving)
        {
            for (int i = 0; i < data.Length; i++)
            {
                data[i] *= soundVolume;
            }
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
