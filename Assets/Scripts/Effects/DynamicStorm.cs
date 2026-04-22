using UnityEngine;
using UnityEngine.UI;

public class DynamicStorm : MonoBehaviour
{
    public static DynamicStorm Instance;

    private RawImage stormImage;

    [Header("Настройки визуала")]
    public Gradient stormGradient;
    public float scale = 12f;

    [Header("Скорость ветра")]
    public float scrollSpeedX = 0.05f;
    public float scrollSpeedY = 0.02f;

    private float emptySkyThreshold = 0.45f;

    [Header("Настройки опасности")]
    [Tooltip("Насколько плотным должно быть облако (от 0 до 1), чтобы самолет потерял связь.")]
    [Range(0f, 1f)]
    public float dangerThreshold = 0.51f;

    // СТАТИЧНЫЕ ПЕРЕМЕННЫЕ (Память бури)
    private static bool isSeedGenerated = false;
    private static float globalXOrg;
    private static float globalYOrg;

    // Запоминаем, куда улетел ветер, чтобы ставить его на паузу
    private static float currentWindOffsetX = 0f;
    private static float currentWindOffsetY = 0f;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        stormImage = GetComponent<RawImage>();

        // Генерируем случайное место для бури только один раз
        if (!isSeedGenerated)
        {
            globalXOrg = Random.value * 100f;
            globalYOrg = Random.value * 100f;
            isSeedGenerated = true;
        }

        Texture2D noiseTex = CreateNoiseTexture(128, 128, scale);
        if (stormImage != null)
        {
            stormImage.texture = noiseTex;
            stormImage.color = Color.white;
        }
    }

    void Update()
    {
        if (stormImage != null)
        {
            // Двигаем ветер ТОЛЬКО когда радар открыт (Update работает)
            currentWindOffsetX = (currentWindOffsetX + scrollSpeedX * Time.deltaTime) % 1.0f;
            currentWindOffsetY = (currentWindOffsetY + scrollSpeedY * Time.deltaTime) % 1.0f;

            Rect uv = stormImage.uvRect;
            uv.x = currentWindOffsetX;
            uv.y = currentWindOffsetY;
            stormImage.uvRect = uv;
        }
    }

    public bool IsInStorm(Vector2 planePosition)
    {
        if (stormImage == null) return false;

        RectTransform rt = GetComponent<RectTransform>();
        if (rt.rect.width == 0 || rt.rect.height == 0) return false;

        float normalizedX = (planePosition.x / rt.rect.width) + 0.5f;
        float normalizedY = (planePosition.y / rt.rect.height) + 0.5f;

        float finalUvX = normalizedX + currentWindOffsetX;
        float finalUvY = normalizedY + currentWindOffsetY;

        float u = finalUvX % 1.0f;
        float v = finalUvY % 1.0f;

        if (u < 0) u += 1.0f;
        if (v < 0) v += 1.0f;

        float xCoord = globalXOrg + u * scale;
        float yCoord = globalYOrg + v * scale;

        float sample = Mathf.PerlinNoise(xCoord, yCoord);

        return sample >= dangerThreshold;
    }

    Texture2D CreateNoiseTexture(int width, int height, float noiseScale)
    {
        Texture2D tex = new Texture2D(width, height);
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Point;

        Color[] pix = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float xCoord = globalXOrg + (float)x / width * noiseScale;
                float yCoord = globalYOrg + (float)y / height * noiseScale;
                float sample = Mathf.PerlinNoise(xCoord, yCoord);

                if (sample < emptySkyThreshold)
                {
                    pix[y * width + x] = new Color(0, 0, 0, 0);
                }
                else
                {
                    float stormIntensity = Mathf.InverseLerp(emptySkyThreshold, 1.0f, sample);
                    pix[y * width + x] = stormGradient.Evaluate(stormIntensity);
                }
            }
        }
        tex.SetPixels(pix);
        tex.Apply();
        return tex;
    }
}