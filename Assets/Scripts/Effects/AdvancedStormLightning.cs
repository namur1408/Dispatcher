using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Video;
using System.Collections;

[HelpURL("https://github.com/namur1408/Dispatcher")]
public class AdvancedStormLightning : MonoBehaviour
{
    [Header("Storm Detection (Настройки шторма)")]
    [Tooltip("Если включено, молния будет бить по таймеру без проверки шторма на радаре (удобно для тестирования).")]
    public bool testMode = false;

    [Tooltip("Центральная точка радара. Если не указана, скрипт автоматически возьмет позицию объекта DynamicStorm.")]
    public Transform radarCenter;

    [Header("Timing Settings (Интервалы и вероятности)")]
    [Tooltip("Минимальное время (в секундах) между ударами молнии.")]
    public float minStrikeInterval = 10f;
    
    [Tooltip("Максимальное время (в секундах) между ударами молнии.")]
    public float maxStrikeInterval = 25f;

    [Range(0f, 1f)]
    [Tooltip("Вероятность удара молнии при истечении таймера (0 - никогда, 1 - всегда).")]
    public float strikeProbability = 0.8f;

    [Header("Visuals - Video Player (Видео-плеер)")]
    [Tooltip("Компонент VideoPlayer, проигрывающий видео с молнией.")]
    public VideoPlayer videoPlayer;

    [Tooltip("Компонент RawImage, на который выводится текстура видео. Будет автоматически включаться во время вспышки.")]
    public UnityEngine.UI.RawImage videoDisplayImage;

    [Tooltip("Компонент SpriteRenderer (если молния создана через 2D Object -> Sprite), на который выводится видео. Будет автоматически включаться во время вспышки.")]
    public SpriteRenderer videoDisplaySprite;

    [Header("Visuals - Sprites (Альтернатива: спрайты)")]
    [Tooltip("Спрайты молний (GameObjects), которые будут активироваться при ударе, если не используется видео.")]
    public GameObject[] lightningSprites;

    [Header("Light Settings (Вспышка света)")]
    [Tooltip("Источники света (Light2D), которые будут имитировать вспышку за окном. Подходит для URP 2D.")]
    public Light2D[] lightningLights;

    [Tooltip("Базовая интенсивность света за окном в спокойном состоянии.")]
    public float baseLightIntensity = 0f;

    [Tooltip("Максимальная интенсивность света во время пика вспышки.")]
    public float maxLightIntensity = 5f;

    [Header("Audio Settings (Звук грома)")]
    [Tooltip("Компонент AudioSource для воспроизведения грома.")]
    public AudioSource thunderAudioSource;

    [Tooltip("Массив аудиоклипов грома. Скрипт будет выбирать случайный звук при каждом ударе.")]
    public AudioClip[] thunderClips;

    [Tooltip("Диапазон громкости звука грома (Min / Max).")]
    public Vector2 volumeRange = new Vector2(0.7f, 1f);

    [Tooltip("Диапазон питча (скорости воспроизведения) для разнообразия звука грома (Min / Max).")]
    public Vector2 pitchRange = new Vector2(0.85f, 1.15f);

    [Header("Camera Shake (Тряска камеры)")]
    [Tooltip("Объект камеры (или контейнер сцены) для тряски. Если пустой, скрипт найдет Camera.main.")]
    public Transform cameraTransform;

    [Tooltip("Базовая сила тряски камеры. Установите 0 для отключения эффекта.")]
    public float shakeIntensity = 0.15f;

    [Tooltip("Продолжительность тряски камеры.")]
    public float shakeDuration = 0.6f;

    // Внутренние переменные
    private float strikeTimer;
    private bool isLightningActive = false;

    private void Start()
    {
        // Инициализируем свет базовыми значениями
        if (lightningLights != null)
        {
            foreach (var light in lightningLights)
            {
                if (light != null) light.intensity = baseLightIntensity;
            }
        }

        // Выключаем спрайты
        if (lightningSprites != null)
        {
            foreach (var sprite in lightningSprites)
            {
                if (sprite != null) sprite.SetActive(false);
            }
        }

        // Выключаем картинку видео-плеера
        if (videoDisplayImage != null)
        {
            videoDisplayImage.enabled = false;
        }

        // Выключаем спрайт видео-плеера
        if (videoDisplaySprite != null)
        {
            videoDisplaySprite.enabled = false;
        }

        // Настраиваем видео-плеер
        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.loopPointReached += OnVideoFinished;
        }

        // Задаем первый интервал ожидания молнии
        ResetStrikeTimer();
    }

    private void Update()
    {
        if (isLightningActive) return;

        bool isStormOverCenter = false;

        if (testMode)
        {
            isStormOverCenter = true;
        }
        else if (DynamicStorm.Instance != null)
        {
            // Берем центр радара или позицию самого шторма
            Vector3 checkPos = radarCenter != null ? radarCenter.position : DynamicStorm.Instance.transform.position;
            isStormOverCenter = DynamicStorm.Instance.IsInStorm(checkPos);
        }

        // Таймер идет только если шторм находится над центром
        if (isStormOverCenter)
        {
            strikeTimer -= Time.deltaTime;
            if (strikeTimer <= 0f)
            {
                // Таймер истек, проверяем вероятность
                if (Random.value < strikeProbability)
                {
                    StartCoroutine(TriggerLightningSequence());
                }
                else
                {
                    // Если не повезло, пересчитываем таймер и пробуем снова позже
                    ResetStrikeTimer();
                }
            }
        }
    }

    private void ResetStrikeTimer()
    {
        strikeTimer = Random.Range(minStrikeInterval, maxStrikeInterval);
    }

    private IEnumerator TriggerLightningSequence()
    {
        isLightningActive = true;

        // Выбираем случайный спрайт, если они заданы
        GameObject activeSprite = null;
        if (lightningSprites != null && lightningSprites.Length > 0)
        {
            activeSprite = lightningSprites[Random.Range(0, lightningSprites.Length)];
        }

        // Рассчитываем физическую задержку звука грома (имитируем расстояние от центра удара)
        // Задержка от 0.1 секунды (очень близко) до 2.5 секунд (далеко)
        float soundDelay = Random.Range(0.1f, 2.5f);
        
        // Чем ближе удар (меньше задержка звука), тем сильнее трясется экран
        float currentShakeIntensity = Mathf.Lerp(shakeIntensity, shakeIntensity * 0.15f, soundDelay / 2.5f);

        // Запускаем воспроизведение видео
        if (videoPlayer != null)
        {
            if (videoDisplayImage != null) videoDisplayImage.enabled = true;
            if (videoDisplaySprite != null) videoDisplaySprite.enabled = true;
            videoPlayer.Play();
        }

        // Моделируем серию коротких вспышек (как настоящая молния)
        int flashCount = Random.Range(2, 5); // 2-4 вспышки в одном разряде
        for (int i = 0; i < flashCount; i++)
        {
            // Вспышка (пиковая яркость)
            float flashIntensity = Random.Range(maxLightIntensity * 0.7f, maxLightIntensity);
            SetVisualsState(flashIntensity, activeSprite, true);
            
            // Длительность пика вспышки (очень быстро)
            yield return new WaitForSeconds(Random.Range(0.02f, 0.07f));

            // Затухание между микро-вспышками
            SetVisualsState(baseLightIntensity, activeSprite, false);
            
            // Время темноты между вспышками
            yield return new WaitForSeconds(Random.Range(0.04f, 0.12f));
        }

        // Финальное плавное затухание света
        float fadeDuration = Random.Range(0.2f, 0.5f);
        float elapsed = 0f;

        // Если играет видео, даем ему время завершиться
        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            StartCoroutine(EnsureVideoTurnsOff(1.5f));
        }

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;
            float currentIntensity = Mathf.Lerp(maxLightIntensity * 0.5f, baseLightIntensity, t);
            
            if (lightningLights != null)
            {
                foreach (var light in lightningLights)
                {
                    if (light != null) light.intensity = currentIntensity;
                }
            }
            yield return null;
        }

        // Гарантируем, что свет выключен
        if (lightningLights != null)
        {
            foreach (var light in lightningLights)
            {
                if (light != null) light.intensity = baseLightIntensity;
            }
        }

        // Воспроизводим звук грома и тряску с рассчитанной задержкой (скорость звука)
        StartCoroutine(PlayThunderAndShake(soundDelay, currentShakeIntensity));

        // Сбрасываем таймер для следующего удара
        ResetStrikeTimer();
        isLightningActive = false;
    }

    private void SetVisualsState(float lightIntensity, GameObject spriteObj, bool isVisible)
    {
        // Управляем светом
        if (lightningLights != null)
        {
            foreach (var light in lightningLights)
            {
                if (light != null) light.intensity = lightIntensity;
            }
        }

        // Управляем спрайтом
        if (spriteObj != null)
        {
            spriteObj.SetActive(isVisible);
        }

        // Если видео-плеера нет, но картинка/спрайт заданы, переключаем их видимость
        if (videoPlayer == null)
        {
            if (videoDisplayImage != null) videoDisplayImage.enabled = isVisible;
            if (videoDisplaySprite != null) videoDisplaySprite.enabled = isVisible;
        }
    }

    private IEnumerator PlayThunderAndShake(float delay, float currentShakeMagnitude)
    {
        yield return new WaitForSeconds(delay);

        // Воспроизводим звук грома
        if (thunderAudioSource != null && thunderClips != null && thunderClips.Length > 0)
        {
            AudioClip clip = thunderClips[Random.Range(0, thunderClips.Length)];
            thunderAudioSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
            thunderAudioSource.volume = Random.Range(volumeRange.x, volumeRange.y);
            thunderAudioSource.PlayOneShot(clip);
        }

        // Трясем камеру
        if (currentShakeMagnitude > 0f)
        {
            Transform cam = cameraTransform != null ? cameraTransform : Camera.main?.transform;
            if (cam != null)
            {
                StartCoroutine(ShakeTransform(cam, currentShakeMagnitude, shakeDuration));
            }
        }
    }

    private IEnumerator ShakeTransform(Transform target, float magnitude, float duration)
    {
        Vector3 originalPos = target.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            
            // Плавное затухание тряски со временем
            float percentComplete = elapsed / duration;
            float damper = 1.0f - percentComplete;

            float x = Random.Range(-1f, 1f) * magnitude * damper;
            float y = Random.Range(-1f, 1f) * magnitude * damper;

            target.localPosition = originalPos + new Vector3(x, y, 0f);
            
            yield return null;
        }

        // Возвращаем камеру в исходное положение
        target.localPosition = originalPos;
    }

    private void OnVideoFinished(VideoPlayer source)
    {
        if (videoDisplayImage != null)
        {
            videoDisplayImage.enabled = false;
        }
        if (videoDisplaySprite != null)
        {
            videoDisplaySprite.enabled = false;
        }
    }

    private IEnumerator EnsureVideoTurnsOff(float timeout)
    {
        yield return new WaitForSeconds(timeout);
        if (videoPlayer != null && !videoPlayer.isPlaying)
        {
            if (videoDisplayImage != null)
            {
                videoDisplayImage.enabled = false;
            }
            if (videoDisplaySprite != null)
            {
                videoDisplaySprite.enabled = false;
            }
        }
    }

    private void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
        }
    }
}
