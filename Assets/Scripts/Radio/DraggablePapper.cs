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
    
    private AudioSource dragAudioSource;
    private AudioSource dropAudioSource;

    private Vector2 lastDragDirection;
    private float lastDragTime;
    private float lastSoundPlayTime;
    private bool wasDragged;
    private bool isDragging;
    private float targetDragVolume = 0f;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();

        dragAudioSource = gameObject.AddComponent<AudioSource>();
        dragAudioSource.playOnAwake = false;

        dropAudioSource = gameObject.AddComponent<AudioSource>();
        dropAudioSource.playOnAwake = false;
    }

    private void PlayDragSound(AudioClip[] clips, float volume)
    {
        if (clips == null || clips.Length == 0) return;
        AudioClip clip = clips[Random.Range(0, clips.Length)];
        if (clip == null) return;

        dragAudioSource.pitch = Random.Range(minPitch, maxPitch);
        
        // Instantly return the volume if it has already faded out
        dragAudioSource.volume = volume;
        targetDragVolume = volume;
        
        dragAudioSource.PlayOneShot(clip, 1f); // 1f because the overall volume is already volume
    }

    private void PlayDropSound(AudioClip clip, float volume)
    {
        if (clip == null) return;
        
        dropAudioSource.pitch = Random.Range(minPitch, maxPitch);
        dropAudioSource.volume = 1f;
        dropAudioSource.PlayOneShot(clip, volume);
    }

    private void Update()
    {
        // If the movement stops for 0.1 sec or the paper is released
        if (!isDragging || (isDragging && Time.time - lastDragTime > 0.1f))
        {
            targetDragVolume = 0f; // We begin a smooth fade
        }

        // Smooth change in rustling volume
        if (dragAudioSource.volume != targetDragVolume)
        {
            // Fade Out rate - 5 units per second (fade out in 0.2s)
            dragAudioSource.volume = Mathf.MoveTowards(dragAudioSource.volume, targetDragVolume, Time.deltaTime * 5f);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        rectTransform.SetAsLastSibling();
        
        lastDragDirection = Vector2.zero;
        lastDragTime = Time.time;
        lastSoundPlayTime = 0f; // We reset it so that the first sound is played immediately upon microshift
        wasDragged = false;
        isDragging = true;
        targetDragVolume = soundVolume;


    }

    public void OnDrag(PointerEventData eventData)
    {
        if (canvas == null) return;
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;

        // The response threshold has been lowered so that the sound plays from the very beginning of the micro-movement
        if (eventData.delta.sqrMagnitude > 0.1f)
        {
            wasDragged = true;
            Vector2 currentDir = eventData.delta.normalized;
            
            bool isFirstMove = lastDragDirection == Vector2.zero;
            bool directionChanged = !isFirstMove && Vector2.Angle(lastDragDirection, currentDir) > 45f;
            bool pausedAndResumed = (Time.time - lastDragTime) > 0.1f;

            if (isFirstMove || directionChanged || pausedAndResumed)
            {
                if (Time.time - lastSoundPlayTime > 0.05f)
                {
                    PlayDragSound(pickupSounds, soundVolume);
                    lastSoundPlayTime = Time.time;
                }
                lastDragDirection = currentDir;
            }

            lastDragTime = Time.time;
            targetDragVolume = soundVolume; // Maintain volume while driving
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
        isDragging = false;
        
        // The rustling sound will gradually fade away in Update()
        
        if (wasDragged)
        {
            PlayDropSound(dropSound, dropSoundVolume);
        }
    }
}
