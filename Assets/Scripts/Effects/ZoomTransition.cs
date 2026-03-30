using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

public class ZoomTransition : MonoBehaviour, IPointerClickHandler
{
    public string sceneToLoad;
    public float zoomDuration = 0.5f;
    public float zoomMultiplier = 2.5f;
    public RectTransform rootContainer;
    public RectTransform zoomTarget;

    public UnityEvent onZoomStart;

    private bool isTransitioning = false;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isTransitioning || string.IsNullOrEmpty(sceneToLoad)) return;
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

        float elapsedTime = 0f;

        while (elapsedTime < zoomDuration)
        {
            elapsedTime += Time.deltaTime;
            float smooth = elapsedTime / zoomDuration;
            smooth = smooth * smooth * (3f - 2f * smooth);

            rootContainer.localScale = Vector3.Lerp(startScale, targetScale, smooth);
            rootContainer.anchoredPosition = Vector2.Lerp(startPos, targetPos, smooth);

            yield return null;
        }

        rootContainer.localScale = targetScale;
        rootContainer.anchoredPosition = targetPos;

        while (asyncLoad.progress < 0.9f) yield return null;

        asyncLoad.allowSceneActivation = true;
    }
}