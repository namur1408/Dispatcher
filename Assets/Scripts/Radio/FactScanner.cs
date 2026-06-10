using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

[RequireComponent(typeof(TextMeshProUGUI))]
public class FactScanner : MonoBehaviour, IPointerClickHandler
{
    private TextMeshProUGUI textMesh;
    private Dictionary<int, GameObject> activeCircles = new Dictionary<int, GameObject>();

    void Awake()
    {
        textMesh = GetComponent<TextMeshProUGUI>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        int linkIndex = TMP_TextUtilities.FindIntersectingLink(textMesh, eventData.position, eventData.pressEventCamera);

        if (linkIndex != -1)
        {
            TMP_LinkInfo linkInfo = textMesh.textInfo.linkInfo[linkIndex];
            string factID = linkInfo.GetLinkID();
            string factText = linkInfo.GetLinkText();

            CommsManager.Instance.SelectFact(factID, factText, this, linkIndex);
        }
    }

    public void HighlightLink(int linkIndex, Color32 color)
    {
        // If color is close to the original text color, we clear the circle
        if (Mathf.Abs(color.r - (textMesh.color.r * 255)) < 5 && Mathf.Abs(color.g - (textMesh.color.g * 255)) < 5 && Mathf.Abs(color.b - (textMesh.color.b * 255)) < 5)
        {
            if (activeCircles.ContainsKey(linkIndex))
            {
                Destroy(activeCircles[linkIndex]);
                activeCircles.Remove(linkIndex);
            }
            return;
        }

        TMP_TextInfo textInfo = textMesh.textInfo;
        TMP_LinkInfo linkInfo = textInfo.linkInfo[linkIndex];

        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);
        bool foundVisible = false;

        for (int i = 0; i < linkInfo.linkTextLength; i++)
        {
            int charIndex = linkInfo.linkTextfirstCharacterIndex + i;
            TMP_CharacterInfo charInfo = textInfo.characterInfo[charIndex];

            if (charInfo.isVisible)
            {
                foundVisible = true;
                Vector3 bottomLeft = charInfo.bottomLeft;
                Vector3 topRight = charInfo.topRight;

                min.x = Mathf.Min(min.x, bottomLeft.x);
                min.y = Mathf.Min(min.y, bottomLeft.y);
                max.x = Mathf.Max(max.x, topRight.x);
                max.y = Mathf.Max(max.y, topRight.y);
            }
        }

        if (!foundVisible) return;

        GameObject circleObj;
        PenCircle penCircle;

        if (activeCircles.ContainsKey(linkIndex))
        {
            circleObj = activeCircles[linkIndex];
            penCircle = circleObj.GetComponent<PenCircle>();
        }
        else
        {
            circleObj = new GameObject("PenCircle_" + linkIndex);
            circleObj.transform.SetParent(transform, false);
            penCircle = circleObj.AddComponent<PenCircle>();
            penCircle.raycastTarget = false;
            activeCircles[linkIndex] = circleObj;
        }

        RectTransform rt = circleObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(max.x - min.x, max.y - min.y);
        rt.localPosition = new Vector2((min.x + max.x) / 2f, (min.y + max.y) / 2f);

        penCircle.color = color;
        penCircle.SetVerticesDirty();
    }
}
