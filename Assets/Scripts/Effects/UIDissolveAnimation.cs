using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Events;

public class UIDissolveAnimation : MonoBehaviour
{
    [Header("Settings")]
    public float duration = 1.0f;
    public AnimationCurve dissolveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Tooltip("Назначьте сюда материал с шейдером UIDissolve. Скрипт подменит текущий материал (Sprite-Lit) на этот при начале анимации.")]
    public Material dissolveMaterialPrefab;

    [Header("Events")]
    public UnityEvent OnDissolveComplete;

    private Material materialInstance;
    private Image imageComponent;
    private Material originalMaterial;

    void Awake()
    {
        imageComponent = GetComponent<Image>();
        if (imageComponent != null)
        {
            originalMaterial = imageComponent.material;
        }
    }

    private void OnDisable()
    {
        if (imageComponent != null && originalMaterial != null)
        {
            imageComponent.material = originalMaterial;
        }
        if (materialInstance != null)
        {
            Destroy(materialInstance);
            materialInstance = null;
        }
    }

    public void PlayDissolve()
    {
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(DissolveRoutine());
        }
    }

    private IEnumerator DissolveRoutine()
    {
        if (imageComponent == null) yield break;

        if (dissolveMaterialPrefab != null)
        {
            materialInstance = Instantiate(dissolveMaterialPrefab);
        }
        else
        {
            materialInstance = Instantiate(originalMaterial);
        }
        
        imageComponent.material = materialInstance;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float val = dissolveCurve.Evaluate(t);
            
            if (materialInstance.HasProperty("_DissolveAmount"))
            {
                materialInstance.SetFloat("_DissolveAmount", val);
            }
            yield return null;
        }
        
        if (materialInstance.HasProperty("_DissolveAmount"))
        {
            materialInstance.SetFloat("_DissolveAmount", 1f);
        }
        
        OnDissolveComplete?.Invoke();
        gameObject.SetActive(false);
    }
}
