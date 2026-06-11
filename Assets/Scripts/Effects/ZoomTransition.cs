using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using UnityEngine.Rendering.Universal;

public class ZoomTransition : MonoBehaviour, IPointerClickHandler
{
    [Header("Scene Loading Mode")]
    public string sceneToLoad;

    [Header("Single Scene Mode (Optional)")]
    public Camera targetCamera;
    public GameObject targetScreenRoot;
    public GameObject currentScreenRoot;
    [Tooltip("Keep currentScreenRoot alive (invisible) so scripts inside keep running. Use this when AirplaneSpawner lives inside currentScreenRoot.")]
    public bool keepCurrentAlive = false;

    [Header("Zoom Settings")]
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
    [HideInInspector] public bool canClick = true;

    private void Awake()
    {
        canClick = true; // Force unlock since tutorials are removed
        AudioSource src = GetComponent<AudioSource>();
        if (src != null) src.playOnAwake = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (StoryManager.isInputLocked) return;
        if (!canClick) return;
        TriggerTransition();
    }

    public void TriggerTransition()
    {
        RadioManager radio = GetComponent<RadioManager>();
        if (radio != null && string.IsNullOrEmpty(RadioManager.activeCallsign)) return;

        bool hasDestination = !string.IsNullOrEmpty(sceneToLoad) || targetCamera != null || targetScreenRoot != null;
        if (isTransitioning || !hasDestination) return;
        StartCoroutine(ZoomAndLoadAsync());
    }

    private IEnumerator ZoomAndLoadAsync()
    {
        isTransitioning = true;
        var evSys = Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
        if (evSys != null) evSys.enabled = false;

        // 1. Запускаем звук
        AudioSource localSource = GetComponent<AudioSource>();
        if (localSource == null) localSource = gameObject.AddComponent<AudioSource>();
        localSource.ignoreListenerVolume = true;
        localSource.playOnAwake = false;

        float totalWaitTime = zoomDuration;

        if (transitionSound != null)
        {
            localSource.clip = transitionSound;
            localSource.volume = soundVolume;
            localSource.Play();

            // Если задан customSoundDuration, ждем его (иначе ждем только зум)
            totalWaitTime = customSoundDuration > 0f ? customSoundDuration : zoomDuration;
        }
        else if (ButtonSoundManager.instance != null)
        {
            ButtonSoundManager.instance.PlayDefaultClick();
        }

        if (RadarManager.Instance != null) RadarManager.Instance.SaveToGlobalManager();

        onZoomStart?.Invoke();

        Transform targetTransform = zoomTarget != null ? zoomTarget : transform;
        ZoomReturnManager.pendingReturnTargetName = targetTransform.name;

        // Начинаем загрузку новой сцены заранее, если указана
        AsyncOperation asyncLoad = null;
        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            asyncLoad = SceneManager.LoadSceneAsync(sceneToLoad);
            asyncLoad.allowSceneActivation = false;
        }

        // Задержка перед началом зума (дает время сцене подгрузиться)
        yield return new WaitForSecondsRealtime(0.1f);

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

        // Останавливаем звук ровно в момент окончания ожидания ТОЛЬКО при загрузке новой сцены (чтобы избежать треска)
        if (asyncLoad != null)
        {
            if (transitionSound != null && localSource.isPlaying)
            {
                localSource.Stop();
            }
        }

        if (asyncLoad != null)
        {
            while (asyncLoad.progress < 0.9f)
            {
                yield return null;
            }

            if (ButtonSoundManager.instance != null)
            {
                ButtonSoundManager.instance.StopAllSounds();
            }

            // Stop all active audio sources to prevent crackling during the scene transition freeze
            AudioSource[] allAudioSources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
            foreach (var source in allAudioSources)
            {
                if (source != null && source.isPlaying && source != localSource)
                {
                    source.Stop();
                }
            }

            // Активируем новую сцену
            if (evSys != null) evSys.enabled = true;
            asyncLoad.allowSceneActivation = true;
        }
        else
        {
            // Переход внутри одной сцены (Single Scene)
            if (targetScreenRoot != null) targetScreenRoot.SetActive(true);
            
            if (targetCamera != null)
            {
                if (Camera.main != null && Camera.main != targetCamera && !keepCurrentAlive)
                {
                    Camera.main.gameObject.SetActive(false);
                }
                targetCamera.gameObject.SetActive(true);
            }

            if (currentScreenRoot != null)
            {
                if (keepCurrentAlive)
                {
                    // Keep it active so spawners/scripts keep running,
                    // but disable ALL GraphicRaycasters so no clicks bleed through.
                    UnityEngine.UI.GraphicRaycaster[] allRaycasters = currentScreenRoot.GetComponentsInChildren<UnityEngine.UI.GraphicRaycaster>(true);
                    foreach (var gr in allRaycasters) gr.enabled = false;
                    // Also hide visually
                    CanvasGroup cg = currentScreenRoot.GetComponent<CanvasGroup>();
                    if (cg == null) cg = currentScreenRoot.AddComponent<CanvasGroup>();
                    cg.alpha = 0f;
                    cg.interactable = false;
                    cg.blocksRaycasts = false;
                }
                else
                {
                    currentScreenRoot.SetActive(false);
                }
            }

            // Сбрасываем зум, чтобы при возвращении на этот экран он был в нормальном виде
            rootContainer.localScale = startScale;
            rootContainer.anchoredPosition = startPos;
            for (int i = 0; i < lights.Length; i++)
            {
                lights[i].pointLightOuterRadius = initialOuter[i];
                lights[i].pointLightInnerRadius = initialInner[i];
            }
            if (evSys != null) evSys.enabled = true;
            isTransitioning = false;
        }
    }
}