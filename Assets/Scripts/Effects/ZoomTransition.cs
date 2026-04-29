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
        if (RadarManager.Instance != null) RadarManager.Instance.SaveToGlobalManager();

        onZoomStart?.Invoke();

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneToLoad);
        asyncLoad.allowSceneActivation = false;

        Vector3 startScale = rootContainer.localScale;
        Vector3 targetScale = startScale * zoomMultiplier;
        Vector2 startPos = rootContainer.anchoredPosition;

        Transform targetTransform = zoomTarget != null ? zoomTarget : transform;
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

        while (asyncLoad.progress < 0.9f) yield return null;
        asyncLoad.allowSceneActivation = true;
    }
}