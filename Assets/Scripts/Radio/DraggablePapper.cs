using UnityEngine;
using UnityEngine.EventSystems;

public class DraggablePaper : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Audio")] 
    public AudioClip[] pickupSounds;
    public AudioClip dropSound;
    [Range(0f, 1f)] public float soundVolume = 0.8f;
    [Range(0f, 1f)] public float dropSoundVolume = 0.8f;
    public float minPitch = 0.9f;
    public float maxPitch = 1.1f;

    [Header("Bounds")]
    public bool constrainToScreen = true;
    public float screenPadding = 50f;

    private RectTransform rectTransform;
    private Canvas canvas;
    private AudioSource paperAudioSource;

    private Vector2 lastDragDirection;
    private float lastDragTime;
    private float lastSoundPlayTime;
    private bool wasDragged;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
    }

    private void PlayPaperSound(AudioClip[] clips, float volume)
    {
        if (clips == null || clips.Length == 0) return;
        AudioClip clip = clips[Random.Range(0, clips.Length)];
        if (clip == null) return;

        if (paperAudioSource == null)
        {
            paperAudioSource = gameObject.AddComponent<AudioSource>();
            paperAudioSource.playOnAwake = false;
        }

        paperAudioSource.pitch = Random.Range(minPitch, maxPitch);
        paperAudioSource.volume = volume;
        paperAudioSource.clip = clip;
        paperAudioSource.Play();
    }

    private void PlayPaperSound(AudioClip clip, float volume)
    {
        if (clip == null) return;
        
        if (paperAudioSource == null)
        {
            paperAudioSource = gameObject.AddComponent<AudioSource>();
            paperAudioSource.playOnAwake = false;
        }

        paperAudioSource.pitch = Random.Range(minPitch, maxPitch);
        paperAudioSource.volume = volume;
        paperAudioSource.clip = clip;
        paperAudioSource.Play();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        rectTransform.SetAsLastSibling();
        
        lastDragDirection = Vector2.zero;
        lastDragTime = Time.time;
        lastSoundPlayTime = Time.time;
        wasDragged = false;

        if (RadioTutorialManager.Instance != null && !RadioTutorialManager.isRadioTutorialCompleted)
        {
            RadioTutorialManager.Instance.NotifyDocumentClicked();
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (canvas == null) return;
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;

        // Логика звуков: играем звук только при смене направления или после паузы
        if (eventData.delta.sqrMagnitude > 1f)
        {
            wasDragged = true;
            Vector2 currentDir = eventData.delta.normalized;
            
            bool isFirstMove = lastDragDirection == Vector2.zero;
            bool directionChanged = !isFirstMove && Vector2.Angle(lastDragDirection, currentDir) > 45f;
            bool pausedAndResumed = (Time.time - lastDragTime) > 0.2f;

            if (isFirstMove || directionChanged || pausedAndResumed)
            {
                // Защита от "пулемета" (слишком частого старта)
                if (Time.time - lastSoundPlayTime > 0.1f)
                {
                    PlayPaperSound(pickupSounds, soundVolume);
                    lastSoundPlayTime = Time.time;
                }
                lastDragDirection = currentDir;
            }

            lastDragTime = Time.time;
        }

        if (constrainToScreen)
        {
            Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(cam, rectTransform.position);
            
            float clampedX = Mathf.Clamp(screenPos.x, screenPadding, Screen.width - screenPadding);
            float clampedY = Mathf.Clamp(screenPos.y, screenPadding, Screen.height - screenPadding);
            
            if (Mathf.Abs(screenPos.x - clampedX) > 0.1f || Mathf.Abs(screenPos.y - clampedY) > 0.1f)
            {
                Vector2 screenOffset = new Vector2(clampedX - screenPos.x, clampedY - screenPos.y);
                rectTransform.anchoredPosition += screenOffset / canvas.scaleFactor;
            }
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (wasDragged)
        {
            PlayPaperSound(dropSound, dropSoundVolume);
        }
    }
}
