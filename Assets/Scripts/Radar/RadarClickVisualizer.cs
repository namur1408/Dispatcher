using UnityEngine;
using UnityEngine.UI;

public class RadarClickVisualizer : MonoBehaviour
{
    public static RadarClickVisualizer Instance;

    [Header("Settings")]
    public GameObject crossPrefab;
    public float duration = 0.5f;
    public Color clickColor = Color.white;

    [Header("Audio")]
    public AudioClip radarClickSound;
    [Range(0f, 1f)]
    public float soundVolume = 1f;

    void Awake()
    {
        Instance = this;
    }

    public void ShowClick(Vector3 worldPos, Transform parent, bool playSound = true)
    {
        if (playSound)
        {
            PlayClickSound();
        }

        if (crossPrefab == null)
        {
            CreateDefaultCrossPrefab();
        }

        GameObject cross = Instantiate(crossPrefab, parent);
        
        RectTransform rt = cross.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.localPosition = parent.InverseTransformPoint(worldPos);
            // Ensure Z is zeroed for UI
            Vector3 pos = rt.localPosition;
            pos.z = 0;
            rt.localPosition = pos;
        }
        else
        {
            cross.transform.position = worldPos;
        }

        cross.transform.localScale = Vector3.one;
        cross.SetActive(true);
        
        Destroy(cross, duration);
        
        var images = cross.GetComponentsInChildren<Image>();
        foreach (var img in images)
        {
            img.color = clickColor;
            StartCoroutine(FadeOut(img));
        }
    }

    private void PlayClickSound()
    {
        if (ButtonSoundManager.instance != null)
        {
            if (radarClickSound != null)
            {
                ButtonSoundManager.instance.PlaySpecialSound(radarClickSound, ButtonSoundManager.instance.volume * soundVolume);
            }
            else
            {
                ButtonSoundManager.instance.PlayDefaultClick();
            }
        }
    }

    private System.Collections.IEnumerator FadeOut(Image img)
    {
        float elapsed = 0;
        Color startColor = img.color;
        while (elapsed < duration)
        {
            if (img == null) break;
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            img.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }
    }

    private void CreateDefaultCrossPrefab()
    {
        GameObject go = new GameObject("DefaultCrossTemplate");
        go.transform.SetParent(this.transform);
        
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(40, 40);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);

        CreateLine(go.transform, 45);
        CreateLine(go.transform, -45);

        crossPrefab = go;
        go.SetActive(false);
    }

    private void CreateLine(Transform parent, float angle)
    {
        GameObject line = new GameObject("Line");
        line.transform.SetParent(parent);
        
        Image img = line.AddComponent<Image>();
        img.color = clickColor;
        img.raycastTarget = false; // Important to not block clicks
        
        RectTransform rt = line.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(30, 2);
        rt.anchoredPosition = Vector2.zero;
        rt.localRotation = Quaternion.Euler(0, 0, angle);
        rt.localScale = Vector3.one;
    }
}
