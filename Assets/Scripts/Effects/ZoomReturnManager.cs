using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI; // Required for CanvasGroup

public class ZoomReturnManager : MonoBehaviour
{
    // Static variable that remembers where we are returning from
    public static string pendingReturnTargetName = "";

    [Header("Настройки отдаления")]
    public RectTransform rootContainer; // Your ScreenContent
    public float zoomDuration = 0.2f;
    public float startingZoomMultiplier = 2.5f;

    void Start()
    {
        TriggerReturnAnimation();
    }

    public void TriggerReturnAnimation()
    {
        // If the variable is not empty, then we loaded the scene when returning from the radar/terminal
        if (!string.IsNullOrEmpty(pendingReturnTargetName))
        {
            StartCoroutine(PrepareAndZoomOut());
        }
    }

    private IEnumerator PrepareAndZoomOut()
    {
        var evSys = Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
        if (evSys != null) evSys.enabled = false;
        // 0. Create a completely black overlay BEFORE waiting for a frame to hide the “jump”
        GameObject fadeObj = new GameObject("ReturnFadeOverlay");
        Canvas fadeCanvas = fadeObj.AddComponent<Canvas>();
        fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        fadeCanvas.sortingOrder = 9999;
        UnityEngine.UI.Image fadeImage = fadeObj.AddComponent<UnityEngine.UI.Image>();
        fadeImage.color = new Color(0f, 0f, 0f, 1f); // Completely black to hide loading

        // 1. Make the container temporarily transparent for 1 frame to hide the “jump”
        CanvasGroup canvasGroup = rootContainer.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = rootContainer.gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;

        // 2. FIX: Wait exactly one frame and force the UI to update. 
        // Without this, Unity thinks that all objects are at point (0,0).
        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();

        // 3. We look for our object by saved name
        GameObject targetObj = GameObject.Find(pendingReturnTargetName);

        if (targetObj != null)
        {
            Debug.Log($"<color=green>[ZoomReturn]</color> Найден объект для возврата: {targetObj.name}");
            // Passing fadeImage and fadeObj to the animation function
            yield return StartCoroutine(ZoomOutAnimation(targetObj.transform, canvasGroup, fadeImage, fadeObj, evSys));
        }
        else
        {
            Debug.LogError($"<color=red>[ZoomReturn]</color> Ошибка! Не найден объект с именем: {pendingReturnTargetName}. Возврат из центра.");
            canvasGroup.alpha = 1f; // We restore visibility if it’s broken
            Destroy(fadeObj);
        }

        // Clearing the variable
        pendingReturnTargetName = "";
    }

    private IEnumerator ZoomOutAnimation(Transform zoomTarget, CanvasGroup canvasGroup, UnityEngine.UI.Image fadeImage, GameObject fadeObj, UnityEngine.EventSystems.EventSystem evSys)
    {
        bool wasAdditive = (rootContainer.localScale.x > 1.5f);

        rootContainer.localScale = Vector3.one;
        rootContainer.anchoredPosition = Vector2.zero;

        Vector3 normalScale = rootContainer.localScale;
        Vector2 normalPos = rootContainer.anchoredPosition;

        Vector3 zoomedScale = normalScale * startingZoomMultiplier;

        Vector3 localTargetPos3D = rootContainer.InverseTransformPoint(zoomTarget.position);
        Vector2 localTargetPos = new Vector2(localTargetPos3D.x, localTargetPos3D.y);
        Vector2 zoomedPos = normalPos - (localTargetPos * (zoomedScale.x - normalScale.x));

        rootContainer.localScale = zoomedScale;
        rootContainer.anchoredPosition = zoomedPos;

        Light2D[] lights = rootContainer.GetComponentsInChildren<Light2D>();
        float[] normalOuter = new float[lights.Length];
        float[] normalInner = new float[lights.Length];
        for (int i = 0; i < lights.Length; i++)
        {
            if (wasAdditive)
            {
                normalOuter[i] = lights[i].pointLightOuterRadius / startingZoomMultiplier;
                normalInner[i] = lights[i].pointLightInnerRadius / startingZoomMultiplier;
            }
            else
            {
                normalOuter[i] = lights[i].pointLightOuterRadius;
                normalInner[i] = lights[i].pointLightInnerRadius;
            }

            lights[i].pointLightOuterRadius = normalOuter[i] * startingZoomMultiplier;
            lights[i].pointLightInnerRadius = normalInner[i] * startingZoomMultiplier;
        }

        canvasGroup.alpha = 1f;
        
        // Open the zoomed frame (reduce the blackness to 70%)
        fadeImage.color = new Color(0f, 0f, 0f, 0.7f);

        // Hover a little in front of the object before starting to move away
        yield return new WaitForSecondsRealtime(0.15f);

        float elapsedTime = 0f;
        while (elapsedTime < zoomDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float smooth = elapsedTime / zoomDuration;
            smooth = smooth * smooth * (3f - 2f * smooth);

            rootContainer.localScale = Vector3.Lerp(zoomedScale, normalScale, smooth);
            rootContainer.anchoredPosition = Vector2.Lerp(zoomedPos, normalPos, smooth);
            
            // Smooth blackout fade
            fadeImage.color = new Color(0f, 0f, 0f, Mathf.Lerp(0.7f, 0f, smooth));

            float currentScaleRatio = rootContainer.localScale.x / normalScale.x;
            for (int i = 0; i < lights.Length; i++)
            {
                lights[i].pointLightOuterRadius = normalOuter[i] * currentScaleRatio;
                lights[i].pointLightInnerRadius = normalInner[i] * currentScaleRatio;
            }

            yield return null;
        }

        rootContainer.localScale = normalScale;
        rootContainer.anchoredPosition = normalPos;
        canvasGroup.alpha = 1f;

        if (evSys != null) evSys.enabled = true;
        Destroy(fadeObj);
    }
}
