using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using UnityEngine.Rendering.Universal;

public class ZoomTransition : MonoBehaviour, IPointerClickHandler
{
    public string sceneToLoad;
    public float zoomDuration = 0.5f;
    public float zoomMultiplier = 2.5f;
    public RectTransform rootContainer;
    public RectTransform zoomTarget;

    [Header("Звук перехода")]
    public AudioClip transitionSound;
    [Range(0f, 1f)]
    public float soundVolume = 0.8f;
    [Tooltip("Время (в сек) до загрузки сцены. Если 0, то берется длина самого аудиофайла.")]
    public float customSoundDuration = 0f;

    public UnityEvent onZoomStart;

    private bool isTransitioning = false;
    public bool canClick = true;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isTransitioning || string.IsNullOrEmpty(sceneToLoad) || !canClick) return;
        StartCoroutine(ZoomAndLoadAsync());
    }

    private IEnumerator ZoomAndLoadAsync()
    {
        isTransitioning = true;

        // 1. Запускаем звук
        AudioSource localSource = GetComponent<AudioSource>();
        if (localSource == null) localSource = gameObject.AddComponent<AudioSource>();

        float totalWaitTime = zoomDuration;

        if (transitionSound != null)
        {
            localSource.clip = transitionSound;
            localSource.volume = soundVolume;
            localSource.Play();

            // Если указано свое время - используем его, иначе берем длину аудиофайла
            float soundLength = customSoundDuration > 0f ? customSoundDuration : transitionSound.length;
            totalWaitTime = Mathf.Max(zoomDuration, soundLength);
        }
        else if (ButtonSoundManager.instance != null)
        {
            ButtonSoundManager.instance.PlayDefaultClick();
        }

        if (RadarManager.Instance != null) RadarManager.Instance.SaveToGlobalManager();

        onZoomStart?.Invoke();

        Transform targetTransform = zoomTarget != null ? zoomTarget : transform;
        ZoomReturnManager.pendingReturnTargetName = targetTransform.name;

        // Начинаем загрузку новой сцены заранее
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneToLoad);
        asyncLoad.allowSceneActivation = false;

        // 2. Анимация Зума (она задает темп)
        Vector3 startScale = rootContainer.localScale;
        Vector3 targetScale = startScale * zoomMultiplier;
        Vector2 startPos = rootContainer.anchoredPosition;

        Vector3 localTargetPos3D = rootContainer.InverseTransformPoint(targetTransform.position);
        Vector2 localTargetPos = new Vector2(localTargetPos3D.x, localTargetPos3D.y);
        Vector2 targetPos = startPos - (localTargetPos * (targetScale.x - startScale.x));

        Light2D[] lights = rootContainer.GetComponentsInChildren<Light2D>();
        float[] initialOuter = new float[lights.Length];
        float[] initialInner = new float[lights.Length];
        for (int i = 0; i < lights.Length; i++)
        {
            initialOuter[i] = lights[i].pointLightOuterRadius;
            initialInner[i] = lights[i].pointLightInnerRadius;
        }

        float elapsedTime = 0f;
        while (elapsedTime < zoomDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float smooth = elapsedTime / zoomDuration;
            smooth = smooth * smooth * (3f - 2f * smooth);

            rootContainer.localScale = Vector3.Lerp(startScale, targetScale, smooth);
            rootContainer.anchoredPosition = Vector2.Lerp(startPos, targetPos, smooth);

            float currentScaleRatio = rootContainer.localScale.x / startScale.x;
            for (int i = 0; i < lights.Length; i++)
            {
                lights[i].pointLightOuterRadius = initialOuter[i] * currentScaleRatio;
                lights[i].pointLightInnerRadius = initialInner[i] * currentScaleRatio;
            }
            yield return null;
        }

        rootContainer.localScale = targetScale;
        rootContainer.anchoredPosition = targetPos;

        // Ждем оставшееся время, пока звук не доиграет
        if (totalWaitTime > zoomDuration)
        {
            yield return new WaitForSecondsRealtime(totalWaitTime - zoomDuration);
        }

        // Останавливаем звук ровно в момент окончания ожидания
        if (transitionSound != null && localSource.isPlaying)
        {
            localSource.Stop();
        }

        while (asyncLoad.progress < 0.9f)
        {
            yield return null;
        }

        // Активируем новую сцену
        asyncLoad.allowSceneActivation = true;
    }
}