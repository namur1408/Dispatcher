using UnityEngine;

public class AirplaneAudio
{
    private UIAirplane _plane;
    private AudioSource _audioSource;
    private float _lastPingTime = 0f;

    public AirplaneAudio(UIAirplane plane, AudioSource source)
    {
        _plane = plane;
        _audioSource = source;

        if (_audioSource != null)
        {
            _audioSource.playOnAwake = false;
            _audioSource.volume = _plane.pingVolume;
        }
    }

    public void PlayPing()
    {
        if (_plane.pingSound != null && Time.time - _lastPingTime > 1.0f)
        {
            if (_audioSource != null) _audioSource.PlayOneShot(_plane.pingSound, _plane.pingVolume);
            _lastPingTime = Time.time;
        }
    }

    public void PlayClick()
    {
        if (ButtonSoundManager.instance != null && _plane.airplaneClickSound != null)
        {
            ButtonSoundManager.instance.PlaySpecialSound(_plane.airplaneClickSound, ButtonSoundManager.instance.volume * _plane.airplaneClickVolume);
        }
        else if (_plane.airplaneClickSound != null && _audioSource != null)
        {
            _audioSource.PlayOneShot(_plane.airplaneClickSound, _plane.airplaneClickVolume);
        }
        else if (ButtonSoundManager.instance != null)
        {
            ButtonSoundManager.instance.PlayDefaultClick();
        }
    }
}
