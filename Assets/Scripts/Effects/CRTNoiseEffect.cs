using UnityEngine;
using UnityEngine.UI;

public class CRTNoiseEffect : MonoBehaviour
{
    private GameObject container;
    private RectTransform[] scanlines = new RectTransform[50];
    private Image[] lineImages = new Image[50];

    void Start()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        // Setup subtle static scanlines
        container = new GameObject("VCR_Static_Lines");
        container.transform.SetParent(canvas.transform, false);
        container.transform.SetAsLastSibling();

        RectTransform containerRt = container.AddComponent<RectTransform>();
        containerRt.anchorMin = Vector2.zero;
        containerRt.anchorMax = Vector2.one;
        containerRt.offsetMin = Vector2.zero;
        containerRt.offsetMax = Vector2.zero;

        for (int i = 0; i < 50; i++)
        {
            GameObject lineObj = new GameObject("StaticLine");
            lineObj.transform.SetParent(container.transform, false);
            
            Image img = lineObj.AddComponent<Image>();
            img.raycastTarget = false;
            
            RectTransform lineRt = lineObj.GetComponent<RectTransform>();
            lineRt.anchorMin = new Vector2(0, 0.5f);
            lineRt.anchorMax = new Vector2(1, 0.5f);
            
            scanlines[i] = lineRt;
            lineImages[i] = img;
        }
    }

    void Update()
    {
        // Update all scanlines constantly to create a subtle static effect
        for (int i = 0; i < 50; i++)
        {
            if (Random.value > 0.2f) 
            {
                RectTransform rt = scanlines[i];
                Image img = lineImages[i];
                
                rt.gameObject.SetActive(true);
                
                float yPos = Random.Range(-600f, 600f);
                float height = Random.Range(1f, 3f);
                
                rt.anchoredPosition = new Vector2(0, yPos);
                rt.sizeDelta = new Vector2(0, height);
                
                // Very faint transparent colors (barely visible)
                float shade = Random.Range(0.4f, 0.9f);
                Color[] colors = { 
                    new Color(1f, 1f, 1f, Random.Range(0.01f, 0.04f)), // Faint White 
                    new Color(0f, 0f, 0f, Random.Range(0.02f, 0.05f)),  // Faint Black
                    new Color(shade, shade, shade, Random.Range(0.01f, 0.03f)) // Faint Grey
                };
                img.color = colors[Random.Range(0, colors.Length)];
            }
            else
            {
                scanlines[i].gameObject.SetActive(false);
            }
        }
    }
}
