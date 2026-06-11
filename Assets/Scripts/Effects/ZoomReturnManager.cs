using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI; // Обязательно для CanvasGroup

public class ZoomReturnManager : MonoBehaviour
{
    // Статическая переменная, которая помнит, откуда мы возвращаемся
    public static string pendingReturnTargetName = "";

    [Header("Настройки отдаления")]
    public RectTransform rootContainer; // Твой ScreenContent
    public float zoomDuration = 0.2f;
    public float startingZoomMultiplier = 2.5f;

    void Start()
    {
        TriggerReturnAnimation();
    }

    public void TriggerReturnAnimation()
    {
        // Если переменная не пустая, значит мы загрузили сцену, возвращаясь с радара/терминала
        if (!string.IsNullOrEmpty(pendingReturnTargetName))
        {
            StartCoroutine(PrepareAndZoomOut());
        }
    }

    private IEnumerator PrepareAndZoomOut()
    {
        var evSys = Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
        if (evSys != null) evSys.enabled = false;
        // 0. Создаём полностью черный оверлей ДО ожидания кадра, чтобы скрыть "прыжок"
        GameObject fadeObj = new GameObject("ReturnFadeOverlay");
        Canvas fadeCanvas = fadeObj.AddComponent<Canvas>();
        fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        fadeCanvas.sortingOrder = 9999;
        UnityEngine.UI.Image fadeImage = fadeObj.AddComponent<UnityEngine.UI.Image>();
        fadeImage.color = new Color(0f, 0f, 0f, 1f); // Полностью черный для скрытия загрузки

        // 1. Делаем контейнер временно прозрачным на 1 кадр, чтобы скрыть "прыжок"
        CanvasGroup canvasGroup = rootContainer.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = rootContainer.gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;

        // 2. ИСПРАВЛЕНИЕ: Ждем ровно один кадр и принудительно обновляем UI. 
        // Без этого Unity думает, что все объекты находятся в точке (0,0).
        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();

        // 3. Ищем наш объект по сохраненному имени
        GameObject targetObj = GameObject.Find(pendingReturnTargetName);

        if (targetObj != null)
        {
            Debug.Log($"<color=green>[ZoomReturn]</color> Найден объект для возврата: {targetObj.name}");
            // Передаем fadeImage и fadeObj в функцию анимации
            yield return StartCoroutine(ZoomOutAnimation(targetObj.transform, canvasGroup, fadeImage, fadeObj, evSys));
        }
        else
        {
            Debug.LogError($"<color=red>[ZoomReturn]</color> Ошибка! Не найден объект с именем: {pendingReturnTargetName}. Возврат из центра.");
            canvasGroup.alpha = 1f; // Возвращаем видимость, если сломалось
            Destroy(fadeObj);
        }

        // Очищаем переменную
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
        
        // Открываем зумированный кадр (снижаем черноту до 70%)
        fadeImage.color = new Color(0f, 0f, 0f, 0.7f);

        // Зависаем немного перед объектом перед началом отдаления
        yield return new WaitForSecondsRealtime(0.15f);

        float elapsedTime = 0f;
        while (elapsedTime < zoomDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float smooth = elapsedTime / zoomDuration;
            smooth = smooth * smooth * (3f - 2f * smooth);

            rootContainer.localScale = Vector3.Lerp(zoomedScale, normalScale, smooth);
            rootContainer.anchoredPosition = Vector2.Lerp(zoomedPos, normalPos, smooth);
            
            // Плавное исчезновение черного затемнения
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
