using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShiftIntroBuilder : MonoBehaviour
{
    [Header("Colors")]
    public Color hackerGreen = new Color(0f, 1f, 0.235f, 1f); // #00FF3C
    public Color hackerGreenDim = new Color(0f, 1f, 0.235f, 0.55f); 
    public Color hackerGreenHint = new Color(0f, 1f, 0.235f, 0.3f);
    public Color warningColor = new Color(1f, 0.7f, 0f, 1f); // #FFB400

    [Header("Font Settings")]
    public TMP_FontAsset customFont; // Основной шрифт (если пусто, возьмется из StoryManager)
    public TMP_FontAsset customTitleFont; // Отдельный шрифт только для заголовка "SHIFT"
    public int statusFontSize = 36;
    public int titleFontSize = 120;
    public int dateFontSize = 44;
    public int hintFontSize = 32;

    public IEnumerator PlaySequence(Transform parent, TMP_FontAsset defaultFont, int dayNumber, string dateStr)
    {
        TMP_FontAsset fontToUse = customFont != null ? customFont : defaultFont;

        // 1. Создаем контейнер
        GameObject container = new GameObject("ShiftIntroSequence");
        RectTransform containerRt = container.AddComponent<RectTransform>();
        containerRt.SetParent(parent, false);
        containerRt.anchorMin = Vector2.zero;
        containerRt.anchorMax = Vector2.one;
        containerRt.offsetMin = Vector2.zero;
        containerRt.offsetMax = Vector2.zero;

        // 2. Создаем элементы
        GameObject statusCont = new GameObject("StatusLines");
        RectTransform statusRt = statusCont.AddComponent<RectTransform>();
        statusRt.SetParent(containerRt, false);
        statusRt.anchorMin = new Vector2(0.5f, 0.5f);
        statusRt.anchorMax = new Vector2(0.5f, 0.5f);
        statusRt.anchoredPosition = new Vector2(0, 180);
        statusRt.sizeDelta = new Vector2(800, 150);
        
        VerticalLayoutGroup vlg = statusCont.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.spacing = 8;
        vlg.childControlHeight = true;
        vlg.childControlWidth = true;

        string statusString;
        if (dayNumber == 1) statusString = $"<color=#{ColorUtility.ToHtmlStringRGB(warningColor)}>FUEL CRITICAL</color>";
        else 
        {
            int econ = PlayerPrefs.GetInt("BaseEmergencyEconomy", 0);
            statusString = econ == 1 ? $"<color=#{ColorUtility.ToHtmlStringRGB(warningColor)}>FUEL CRITICAL</color>" : $"<color=#{ColorUtility.ToHtmlStringRGB(hackerGreen)}>NOMINAL</color>";
        }

        TextMeshProUGUI s1 = CreateText(statusCont.transform, "BASTION-7 // SECTOR GRID ONLINE", fontToUse, statusFontSize, hackerGreenDim);
        TextMeshProUGUI s2 = CreateText(statusCont.transform, "WEATHER: HEAVY STORM — VIS: LOW", fontToUse, statusFontSize, hackerGreenDim);
        TextMeshProUGUI s3 = CreateText(statusCont.transform, $"BASE STATUS: {statusString}", fontToUse, statusFontSize, hackerGreenDim);

        s1.characterSpacing = 2; s2.characterSpacing = 2; s3.characterSpacing = 2;
        s1.alpha = 0; s2.alpha = 0; s3.alpha = 0;

        // Divider
        GameObject divObj = new GameObject("Divider");
        RectTransform divRt = divObj.AddComponent<RectTransform>();
        divRt.SetParent(containerRt, false);
        divRt.anchorMin = new Vector2(0.5f, 0.5f);
        divRt.anchorMax = new Vector2(0.5f, 0.5f);
        divRt.anchoredPosition = new Vector2(0, 40);
        divRt.sizeDelta = new Vector2(0, 2);
        Image divImg = divObj.AddComponent<Image>();
        divImg.color = hackerGreenHint;

        // Title
        TMP_FontAsset titleFontToUse = customTitleFont != null ? customTitleFont : fontToUse;
        TextMeshProUGUI titleTxt = CreateText(containerRt, "", titleFontToUse, titleFontSize, hackerGreen);
        titleTxt.rectTransform.anchoredPosition = new Vector2(0, -60);
        titleTxt.characterSpacing = 15;

        // Date
        TextMeshProUGUI dateTxt = CreateText(containerRt, "", fontToUse, dateFontSize, new Color(hackerGreen.r, hackerGreen.g, hackerGreen.b, 0.55f));
        dateTxt.rectTransform.anchoredPosition = new Vector2(0, -180);
        dateTxt.characterSpacing = 10;

        // Hint
        TextMeshProUGUI hintTxt = CreateText(containerRt, "// TAP TO CONTINUE", fontToUse, hintFontSize, hackerGreenHint);
        hintTxt.rectTransform.anchoredPosition = new Vector2(0, -350);
        hintTxt.characterSpacing = 4;
        hintTxt.alpha = 0;

        // === АНИМАЦИЯ ===
        yield return new WaitForSecondsRealtime(0.4f);

        yield return FadeTMP(s1, 1f, 0.2f);
        yield return new WaitForSecondsRealtime(0.1f);
        yield return FadeTMP(s2, 1f, 0.2f);
        yield return new WaitForSecondsRealtime(0.1f);
        yield return FadeTMP(s3, 1f, 0.4f);

        // Расширение линии
        float t = 0;
        while (t < 0.6f)
        {
            t += Time.unscaledDeltaTime;
            // Easing out
            float smooth = 1f - Mathf.Pow(1f - (t / 0.6f), 3f);
            divRt.sizeDelta = new Vector2(Mathf.Lerp(0, 500, smooth), 2);
            yield return null;
        }

        yield return new WaitForSecondsRealtime(0.3f);

        // Печать SHIFT
        string shiftText = $"SHIFT {dayNumber}";
        for (int i = 0; i < shiftText.Length; i++)
        {
            titleTxt.text += shiftText[i];
            yield return new WaitForSecondsRealtime(0.08f);
        }

        yield return new WaitForSecondsRealtime(0.3f);

        // Печать даты с курсором
        string dateStrFinal = dateStr;
        string cursorColorHex = ColorUtility.ToHtmlStringRGB(hackerGreen);
        for (int i = 0; i < dateStrFinal.Length; i++)
        {
            dateTxt.text = dateStrFinal.Substring(0, i + 1) + $"<color=#{cursorColorHex}>_</color>";
            yield return new WaitForSecondsRealtime(0.07f);
        }
        yield return new WaitForSecondsRealtime(0.5f);
        dateTxt.text = dateStrFinal; // убираем курсор

        yield return new WaitForSecondsRealtime(0.4f);
        yield return FadeTMP(hintTxt, 1f, 0.5f);

        // Ждем клика
        bool clicked = false;
        while (!clicked)
        {
            // Пульсация подсказки
            hintTxt.alpha = 0.2f + Mathf.PingPong(Time.unscaledTime * 0.8f, 0.4f);

            // Мерцание заголовка (Flicker)
            if (Random.value > 0.95f) titleTxt.alpha = Random.Range(0.6f, 1f);
            else titleTxt.alpha = 1f;

            if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
            {
                clicked = true;
            }
            yield return null;
        }

        // Очистка
        Object.Destroy(container);
    }

    private TextMeshProUGUI CreateText(Transform parent, string text, TMP_FontAsset font, float size, Color color)
    {
        GameObject go = new GameObject("Txt");
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(1000, 150); // Достаточно большое, чтобы не было переносов
        
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.font = font;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.text = text;
        tmp.alignment = TextAlignmentOptions.Center;
        return tmp;
    }

    private IEnumerator FadeTMP(TextMeshProUGUI tmp, float targetAlpha, float duration)
    {
        float start = tmp.alpha;
        float time = 0;
        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            tmp.alpha = Mathf.Lerp(start, targetAlpha, time / duration);
            yield return null;
        }
        tmp.alpha = targetAlpha;
    }
}
