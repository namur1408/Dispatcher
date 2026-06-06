using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class WindowLightning : MonoBehaviour
{
    [Header("Посилання (Ссылки)")]
    [Tooltip("Джерела світла за вікнами (Freeform Light 2D або інші). Можна додати кілька!")]
    public Light2D[] lightningLights;
    
    [Tooltip("Звук грому (AudioSource)")]
    public AudioSource thunderAudio;
    
    [Tooltip("Масив картинок (спрайтів) самих блискавок, які будуть з'являтися у вікні. Перетягни сюди об'єкти з блискавками.")]
    public GameObject[] lightningSprites;

    [Tooltip("Центр радара (зазвичай можна залишити пустим, скрипт сам знайде центр екрану радара)")]
    public Transform radarCenter;

    [Header("Налаштування блискавки")]
    [Tooltip("Шанс удару блискавки в секунду (від 0 до 1), коли шторм над радаром")]
    public float lightningChancePerSecond = 0.2f;
    
    [Tooltip("Максимальна яскравість спалаху")]
    public float maxIntensity = 10f;
    
    [Tooltip("Базова яскравість вікна (коли немає блискавки)")]
    public float baseIntensity = 0f;

    private bool isFlashing = false;

    void Awake()
    {
        if (lightningLights != null)
        {
            foreach (var light in lightningLights)
            {
                if (light != null) light.intensity = baseIntensity;
            }
        }

        if (lightningSprites != null)
        {
            foreach (var sprite in lightningSprites)
            {
                if (sprite != null) sprite.SetActive(false);
            }
        }
    }

    void Update()
    {
        if (DynamicStorm.Instance != null && !isFlashing)
        {
            // МАГІЯ ТУТ: Якщо ти нічого не вказав, скрипт бере центр самого об'єкту DynamicStorm (центр великого радара).
            // Оскільки авіабаза завжди по центру радара, це ідеально співпадає!
            Vector3 centerPos = radarCenter != null ? radarCenter.position : DynamicStorm.Instance.transform.position;

            if (DynamicStorm.Instance.IsInStorm(centerPos))
            {
                if (Random.value < lightningChancePerSecond * Time.deltaTime)
                {
                    StartCoroutine(LightningSequence());
                }
            }
        }
    }

    private IEnumerator LightningSequence()
    {
        isFlashing = true;

        GameObject activeLightning1 = null;
        GameObject activeLightning2 = null;

        if (lightningSprites != null && lightningSprites.Length > 0)
        {
            activeLightning1 = lightningSprites[Random.Range(0, lightningSprites.Length)];
            
            if (lightningSprites.Length > 1 && Random.value > 0.5f)
            {
                activeLightning2 = lightningSprites[Random.Range(0, lightningSprites.Length)];
            }
        }

        int flashes = Random.Range(1, 4);

        for (int i = 0; i < flashes; i++)
        {
            float currentFlashIntensity = Random.Range(maxIntensity * 0.7f, maxIntensity);

            if (lightningLights != null)
            {
                foreach (var light in lightningLights)
                {
                    if (light != null) light.intensity = currentFlashIntensity;
                }
            }
            if (activeLightning1 != null) activeLightning1.SetActive(true);
            if (activeLightning2 != null) activeLightning2.SetActive(true);
            
            yield return new WaitForSeconds(Random.Range(0.05f, 0.1f));
            
            if (lightningLights != null)
            {
                foreach (var light in lightningLights)
                {
                    if (light != null) light.intensity = baseIntensity;
                }
            }
            if (activeLightning1 != null) activeLightning1.SetActive(false);
            if (activeLightning2 != null) activeLightning2.SetActive(false);
            
            yield return new WaitForSeconds(Random.Range(0.05f, 0.15f));
        }

        float soundDelay = Random.Range(0.2f, 1.5f);
        yield return new WaitForSeconds(soundDelay);

        if (thunderAudio != null)
        {
            thunderAudio.pitch = Random.Range(0.85f, 1.15f);
            thunderAudio.Play();
        }

        yield return new WaitForSeconds(Random.Range(3f, 7f));

        isFlashing = false;
    }
}
