using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;

public class LampToggle : MonoBehaviour, IPointerClickHandler
{
    private Light2D[] allLamps;
    
    [Tooltip("Звук клацання при увімкненні/вимкненні (можна перетягнути .wav файл)")]
    public AudioClip clickSound;

    [Range(0f, 1f)]
    [Tooltip("Гучність звуку клацання")]
    public float soundVolume = 0.2f;

    private AudioSource audioSource;

    [Tooltip("Об'єкт пилу лампи (LampDust), який буде вмикатися/вимикатися разом зі світлом")]
    public GameObject lampDust;

    void Awake()
    {
        // Ищем ВСЕ компоненты Light 2D на этом объекте и на всех вложенных (дочерних)
        allLamps = GetComponentsInChildren<Light2D>();
        
        if (allLamps == null || allLamps.Length == 0)
        {
            Debug.LogWarning("LampToggle: Не найдено ни одного компонента Light 2D!");
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        // Автоматично шукаємо дочірній об'єкт з назвою "LampDust", якщо не перетягнули вручну
        if (lampDust == null)
        {
            Transform dustTransform = transform.Find("LampDust");
            if (dustTransform != null)
            {
                lampDust = dustTransform.gameObject;
            }
            else
            {
                // Якщо не знайшли прямого нащадка, шукаємо по всій ієрархії вглиб
                foreach (Transform child in GetComponentsInChildren<Transform>(true))
                {
                    if (child.name == "LampDust")
                    {
                        lampDust = child.gameObject;
                        break;
                    }
                }
            }
        }
    }

    // Вызывается, если лампа является UI-элементом
    public void OnPointerClick(PointerEventData eventData)
    {
        ToggleLight();
    }

    // Вызывается, если лампа - объект в мире (с Collider2D)
    void OnMouseDown()
    {
        // Если перед лампой есть UI элемент, игнорируем клик
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        ToggleLight();
    }

    private void ToggleLight()
    {
        if (allLamps != null && allLamps.Length > 0)
        {
            // Переключаем состояние у КАЖДОГО найденного света
            bool newState = !allLamps[0].enabled; // Смотрим по первой лампе
            
            foreach (var lamp in allLamps)
            {
                if (lamp != null)
                {
                    lamp.enabled = newState;
                }
            }

            // Вмикаємо або вимикаємо пил лампи
            if (lampDust != null)
            {
                lampDust.SetActive(newState);
            }

            // Відтворюємо звук з налаштованою гучністю
            if (audioSource != null && clickSound != null)
            {
                audioSource.PlayOneShot(clickSound, soundVolume);
            }
        }
    }
}
