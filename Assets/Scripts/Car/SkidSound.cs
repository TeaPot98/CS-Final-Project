using System;
using UnityEngine;

public class SkidSound : MonoBehaviour
{
    public AudioClip skidSound;

    private Car _car;
    private AudioSource _skidSoundSource;

    private void Awake()
    {
        _car = GetComponent<Car>();

        _skidSoundSource = CreateSource(skidSound);
        _skidSoundSource.volume = 0f;
        _skidSoundSource.Play();
        _skidSoundSource.Pause();
        _skidSoundSource.volume = 1f;
    }

    private void Update()
    {
        if (_car.ShouldPlaySkidSound && !_skidSoundSource.isPlaying) _skidSoundSource.UnPause();
        if (!_car.ShouldPlaySkidSound && _skidSoundSource.isPlaying) _skidSoundSource.Pause();
    }

    private AudioSource CreateSource(AudioClip clip)
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();

        source.clip = clip;
        source.loop = true;
        source.playOnAwake = false;

        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.minDistance = 2f;
        source.maxDistance = 60f;
        source.dopplerLevel = 0.25f;

        return source;
    }
}