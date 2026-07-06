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
        // We are looking for ALL Light 2D components on this object and on all nested (child)
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

        // We automatically look for a child object with the name "LampDust", if it was not dragged manually
        if (lampDust == null)
        {
            Transform dustTransform = transform.Find("LampDust");
            if (dustTransform != null)
            {
                lampDust = dustTransform.gameObject;
            }
            else
            {
                // If we do not find a direct descendant, we search deeply throughout the hierarchy
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

    // Called if the lamp is a UI element
    public void OnPointerClick(PointerEventData eventData)
    {
        ToggleLight();
    }

    // Called if the lamp is an object in the world (with Collider2D)
    void OnMouseDown()
    {
        // If there is a UI element in front of the lamp, ignore the click
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
            // Switch the state of EVERY light found
            bool newState = !allLamps[0].enabled; // Let's look at the first lamp
            
            foreach (var lamp in allLamps)
            {
                if (lamp != null)
                {
                    lamp.enabled = newState;
                }
            }

            // We turn on or off the dust of the lamp
            if (lampDust != null)
            {
                lampDust.SetActive(newState);
            }

            // Play the sound with the adjusted volume
            if (audioSource != null && clickSound != null)
            {
                audioSource.PlayOneShot(clickSound, soundVolume);
            }
        }
    }
}
