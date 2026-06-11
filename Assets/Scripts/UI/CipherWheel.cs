using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class CipherWheel : MonoBehaviour
{
    [Header("UI References")]
    public RectTransform container;   // Пустой RectTransform, куда будут спавниться буквы
    public GameObject textPrefab;     // Префаб TextMeshProUGUI

    [Header("Cylinder Settings")]
    public float radius = 250f;             // Радиус виртуального цилиндра (высота барабана)
    public float spacingMultiplier = 1f;    // Расстояние между буквами

    [Header("Letter Colors")]
    public Color centerColor  = new Color(0.95f, 0.85f, 0.55f, 1f);  // Центральная буква — золотая
    public Color normalColor  = new Color(0.40f, 0.35f, 0.25f, 1f);  // Боковые — тёмные
    [Tooltip("How far from center (in angle steps) the color fully transitions to normalColor. 1 = immediate, 3 = gradual")]
    public float colorFalloffSteps = 2.0f;

    [Header("Letter Size")]
    [Tooltip("Font size of the center (active) letter")]
    public float centerFontSize = 52f;
    [Tooltip("Font size of letters at the edge of visibility")]
    public float edgeFontSize = 28f;
    [Tooltip("Power curve for size falloff — higher = center letter stands out more")]
    public float sizeFalloffPower = 1.5f;

    [Header("Visibility")]
    [Tooltip("Angle cutoff beyond which letters are hidden (degrees). 85 shows ~3 letters each side.")]
    public float visibilityCutoffAngle = 85f;
    [Tooltip("How quickly letters fade with distance from center. Higher = sharper fade.")]
    public float alphaCurvePower = 1.6f;

    [Header("Horizontal Squeeze (Cylinder Perspective)")]
    [Tooltip("Amount of horizontal squeeze at the edges (0 = none, 0.25 = subtle 3D perspective)")]
    [Range(0f, 0.5f)]
    public float horizontalSqueezeAmount = 0.12f;

    [Header("Inertia / Mechanical Feel")]
    [Tooltip("Base spring speed of the drum rotation")]
    public float rotationSpeed = 10f;
    [Tooltip("How much the drum overshoots and bounces back (0 = no bounce, 0.25 = subtle mechanical snap)")]
    [Range(0f, 0.4f)]
    public float overshootAmount = 0.15f;

    [Header("Logic")]
    public string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    public int currentIndex = 0;

    [Header("Audio")]
    public AudioClip rotationSound;
    public AudioClip fastRotationSound;
    [Range(0f, 1f)] public float soundVolume = 1f;
    public float minPitch = 0.9f;
    public float maxPitch = 1.1f;
    public float fastThreshold = 1.5f;

    // ── Private state ──────────────────────────────────────────────────────────
    private AudioSource dedicatedAudioSource;
    private bool isMoving = false;
    private bool isCurrentlyFast = false;

    private List<RectTransform>    slots        = new List<RectTransform>();
    private List<TextMeshProUGUI>  letters      = new List<TextMeshProUGUI>();
    private List<CanvasGroup>      canvasGroups = new List<CanvasGroup>();

    private float currentFloatIndex = 0f;
    private float velocity = 0f;      // for spring simulation

    // ── Init ──────────────────────────────────────────────────────────────────
    void Start()
    {
        for (int i = 0; i < alphabet.Length; i++)
        {
            GameObject go = Instantiate(textPrefab, container);

            RectTransform rect = go.GetComponent<RectTransform>();
            slots.Add(rect);

            TextMeshProUGUI txt = go.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null)
            {
                txt.text      = alphabet[i].ToString();
                txt.alignment = TextAlignmentOptions.Center;
                txt.fontSize  = edgeFontSize;
                letters.Add(txt);
            }

            CanvasGroup cg = go.GetComponent<CanvasGroup>();
            if (cg == null) cg = go.AddComponent<CanvasGroup>();
            canvasGroups.Add(cg);
        }
        currentFloatIndex = currentIndex;
    }

    // ── Update ────────────────────────────────────────────────────────────────
    void Update()
    {
        // ── Spring-damper towards target ──────────────────────────────────────
        float target = currentIndex;

        // Wrap: always take the shortest path around the cylinder
        float diff = target - currentFloatIndex;
        if (diff >  alphabet.Length / 2f) currentFloatIndex += alphabet.Length;
        if (diff < -alphabet.Length / 2f) currentFloatIndex -= alphabet.Length;

        // Critically-damped spring with configurable overshoot
        float stiffness = rotationSpeed * rotationSpeed;
        float damping   = 2f * rotationSpeed * (1f - overshootAmount);
        float delta     = (target - currentFloatIndex);
        velocity       += (stiffness * delta - damping * velocity) * Time.deltaTime;
        currentFloatIndex += velocity * Time.deltaTime;

        // Wrap float index back into [0, length)
        if (currentFloatIndex >= alphabet.Length) currentFloatIndex -= alphabet.Length;
        if (currentFloatIndex <  0)               currentFloatIndex += alphabet.Length;

        float distToTarget = Mathf.Abs(Mathf.DeltaAngle(
            currentFloatIndex * (360f / alphabet.Length),
            target            * (360f / alphabet.Length)));
        bool moving = distToTarget > 0.05f;
        bool isFast = Mathf.Abs(velocity) > fastThreshold;
        HandleSound(moving, isFast);

        // ── Position each letter on the virtual cylinder ──────────────────────
        float angleStep = (360f / alphabet.Length) * spacingMultiplier;

        for (int i = 0; i < slots.Count; i++)
        {
            float distance = i - currentFloatIndex;

            // Wrap distance to shortest path
            if (distance >  alphabet.Length / 2f) distance -= alphabet.Length;
            if (distance < -alphabet.Length / 2f) distance += alphabet.Length;

            float angle = distance * angleStep;

            if (Mathf.Abs(angle) < visibilityCutoffAngle)
            {
                if (!slots[i].gameObject.activeSelf) slots[i].gameObject.SetActive(true);

                float rad = angle * Mathf.Deg2Rad;
                float cos = Mathf.Cos(rad);   // 1 at center, 0 at 90°
                float t   = cos;              // 0..1, 1 = center

                // ── Y position on cylinder arc ────────────────────────────────
                slots[i].anchoredPosition = new Vector2(0, radius * Mathf.Sin(rad));

                // ── Scale: vertical cos compression + horizontal squeeze ───────
                float hScale = 1f - horizontalSqueezeAmount * (1f - cos);
                slots[i].localScale = new Vector3(hScale, cos, 1f);

                // ── Alpha: smooth power curve, full brightness at center ───────
                float normalizedAngle = Mathf.Abs(angle) / visibilityCutoffAngle; // 0..1
                float alpha = Mathf.Pow(1f - normalizedAngle, alphaCurvePower);
                if (canvasGroups.Count > i && canvasGroups[i] != null)
                    canvasGroups[i].alpha = Mathf.Clamp01(alpha);

                // ── Color: lerp based on proximity to center ──────────────────
                float colorT = Mathf.Clamp01(1f - Mathf.Abs(distance) / colorFalloffSteps);
                colorT = colorT * colorT; // ease-in — snappier gold at exact center
                if (letters.Count > i && letters[i] != null)
                {
                    letters[i].color = Color.Lerp(normalColor, centerColor, colorT);

                    // ── Font size: larger at center ───────────────────────────
                    float sizeT = Mathf.Pow(Mathf.Clamp01(cos), sizeFalloffPower);
                    letters[i].fontSize = Mathf.Lerp(edgeFontSize, centerFontSize, sizeT);
                }
            }
            else
            {
                if (slots[i].gameObject.activeSelf) slots[i].gameObject.SetActive(false);
            }
        }
    }

    // ── Sound ─────────────────────────────────────────────────────────────────
    private void HandleSound(bool moving, bool isFast)
    {
        if (dedicatedAudioSource == null)
        {
            dedicatedAudioSource = gameObject.AddComponent<AudioSource>();
            dedicatedAudioSource.playOnAwake = false;
        }

        if (moving)
        {
            AudioClip clipToPlay = (isFast && fastRotationSound != null) ? fastRotationSound : rotationSound;
            if (clipToPlay == null) return;

            if (!isMoving || isCurrentlyFast != isFast)
            {
                dedicatedAudioSource.clip   = clipToPlay;
                dedicatedAudioSource.volume = soundVolume;
                dedicatedAudioSource.pitch  = Random.Range(minPitch, maxPitch);
                dedicatedAudioSource.loop   = true;
                dedicatedAudioSource.Play();

                isMoving        = true;
                isCurrentlyFast = isFast;
            }
        }
        else if (!moving && isMoving)
        {
            dedicatedAudioSource.Stop();
            isMoving = false;
        }
    }
}
